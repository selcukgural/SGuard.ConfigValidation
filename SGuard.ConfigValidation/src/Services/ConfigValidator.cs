using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Resources;
using SGuard.ConfigValidation.Security;
using SGuard.ConfigValidation.Services.Abstract;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Validates SGuard configuration structure, uniqueness, and integrity.
/// </summary>
public sealed class ConfigValidator : IConfigValidator
{
    private readonly IValidatorFactory? _validatorFactory;
    private readonly SecurityOptions _securityOptions;
    private readonly ILogger<ConfigValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the ConfigValidator class.
    /// </summary>
    /// <param name="logger">Logger instance for logging validation operations.</param>
    /// <param name="securityOptions">Security options configured via IOptions pattern.</param>
    /// <param name="validatorFactory">Optional validator factory. If provided, supported validators will be retrieved from the factory.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="logger"/> or <paramref name="securityOptions"/> is null.</exception>
    public ConfigValidator(ILogger<ConfigValidator> logger, IOptions<SecurityOptions> securityOptions, IValidatorFactory? validatorFactory = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(securityOptions);

        _logger = logger;
        _securityOptions = securityOptions.Value;
        _validatorFactory = validatorFactory;
    }

    /// <summary>
    /// Validates the configuration structure, uniqueness, and integrity.
    /// Checks for duplicate environment IDs, duplicate rule IDs, missing required fields,
    /// unsupported validator types, and invalid rule-environment mappings.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <param name="supportedValidators">The list of supported validator types. Used only if a validator factory is not provided.</param>
    /// <returns>A list of validation errors. Empty list means validation passed.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="config"/> or <paramref name="supportedValidators"/> is null.</exception>
    public List<string> Validate(SGuardConfig config, IEnumerable<string> supportedValidators)
    {
        var errors = new List<string>();

        // Get supported validators from factory if available, otherwise use a provided list

        var supportedValidatorSet = _validatorFactory != null
                                        ? new HashSet<string>(_validatorFactory.GetSupportedValidators(), StringComparer.OrdinalIgnoreCase)
                                        : new HashSet<string>(supportedValidators, StringComparer.OrdinalIgnoreCase);

        // Validate version
        ValidateVersion(config, errors);

        // Validate environments
        ValidateEnvironments(config, errors);

        // Validate rules
        ValidateRules(config, errors, supportedValidatorSet);

        // Validate rule-environment mappings
        ValidateRuleEnvironmentMappings(config, errors);

        if (errors.Count > 0)
        {
            _logger.LogWarning("Configuration validation failed with {ErrorCount} error(s)", errors.Count);
        }

        return errors;
    }

    /// <summary>
    /// Validates the version field of the configuration.
    /// Checks that the version is not null or empty.
    /// </summary>
    /// <param name="config">The configuration object to validate.</param>
    /// <param name="errors">The list to which validation error messages will be added.</param>
    private static void ValidateVersion(SGuardConfig config, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(config.Version))
        {
            errors.Add(SR.ConfigValidator_VersionRequired);
        }
    }

    /// <summary>
    /// Validates the environments section of the configuration.
    /// Checks for required fields, duplicate environment IDs, and path format issues.
    /// Adds any validation errors to the provided errors list.
    /// </summary>
    /// <param name="config">The configuration object containing environments to validate.</param>
    /// <param name="errors">The list to which validation error messages will be added.</param>
    private static void ValidateEnvironments(SGuardConfig config, List<string> errors)
    {
        if (config.Environments.Count == 0)
        {
            errors.Add(SR.ConfigValidator_EnvironmentsEmpty);
            return;
        }

        var environmentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateIds = new HashSet<string>();

        for (var i = 0; i < config.Environments.Count; i++)
        {
            var env = config.Environments[i];

            // Validate required fields
            var jsonPath = $"$.environments[{i}]";

            if (string.IsNullOrWhiteSpace(env.Id))
            {
                errors.Add(string.Format(SR.ConfigValidator_EnvironmentIdRequired, jsonPath, i));
            }
            else
            {
                // Check for duplicate IDs
                if (!environmentIds.Add(env.Id))
                {
                    duplicateIds.Add(env.Id);
                }
            }

            if (string.IsNullOrWhiteSpace(env.Name))
            {
                var envIdInfo = !string.IsNullOrWhiteSpace(env.Id) ? $" (id: '{env.Id}')" : string.Empty;
                errors.Add(string.Format(SR.ConfigValidator_EnvironmentNameRequired, jsonPath, envIdInfo, i));
            }

            if (string.IsNullOrWhiteSpace(env.Path))
            {
                var envIdInfo = !string.IsNullOrWhiteSpace(env.Id) ? $" (id: '{env.Id}')" : string.Empty;
                errors.Add(string.Format(SR.ConfigValidator_EnvironmentPathRequired, jsonPath, envIdInfo, i));
            }
            else
            {
                // Validate a path format (basic validation - no file existence check)
                var pathValidationError = ValidatePathFormat(env.Path);

                if (string.IsNullOrWhiteSpace(pathValidationError))
                {
                    continue;
                }

                var envIdInfo = !string.IsNullOrWhiteSpace(env.Id) ? $" (id: '{env.Id}')" : string.Empty;
                errors.Add(string.Format(SR.ConfigValidator_EnvironmentPathInvalidFormat, jsonPath, envIdInfo, env.Path, pathValidationError));
            }
        }

        // Report duplicate IDs
        foreach (var duplicateId in duplicateIds)
        {
            var duplicateIndices = new List<int>();

            for (var i = 0; i < config.Environments.Count; i++)
            {
                if (string.Equals(config.Environments[i].Id, duplicateId, StringComparison.OrdinalIgnoreCase))
                {
                    duplicateIndices.Add(i);
                }
            }

            var indicesStr = string.Join(", ", duplicateIndices.Select(i => $"$.environments[{i}]"));
            errors.Add(string.Format(SR.ConfigValidator_DuplicateEnvironmentId, duplicateId, indicesStr, duplicateIndices.Count));
        }
    }

    /// <summary>
    /// Validates the rules section of the configuration.
    /// Checks for required fields, duplicate rule IDs, valid environment references,
    /// rule details, conditions, and supported validator types.
    /// Adds any validation errors to the provided errors list.
    /// </summary>
    /// <param name="config">The configuration object containing rules to validate.</param>
    /// <param name="errors">The list to which validation error messages will be added.</param>
    /// <param name="supportedValidators">A set of supported validator types for rule validation.</param>
    private void ValidateRules(SGuardConfig config, List<string> errors, HashSet<string> supportedValidators)
    {
        if (config.Rules.Count == 0)
        {
            var environmentCount = config.Environments.Count;
            errors.Add(string.Format(SR.ConfigValidator_RulesRequired, environmentCount));
            return;
        }

        var ruleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateRuleIds = new HashSet<string>();

        for (var i = 0; i < config.Rules.Count; i++)
        {
            var rule = config.Rules[i];

            var ruleJsonPath = $"$.rules[{i}]";

            // Validate rule ID
            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                errors.Add(string.Format(SR.ConfigValidator_RuleIdRequired, ruleJsonPath, i));
            }
            else
            {
                // Check for duplicate rule IDs
                if (!ruleIds.Add(rule.Id))
                {
                    duplicateRuleIds.Add(rule.Id);
                }
            }

            // Validate environments list
            if (rule.Environments.Count == 0)
            {
                var ruleIdInfo = !string.IsNullOrWhiteSpace(rule.Id) ? $" (id: '{rule.Id}')" : string.Empty;
                errors.Add(string.Format(SR.ConfigValidator_RuleEnvironmentsEmpty, ruleJsonPath, ruleIdInfo, i));
            }
            else
            {
                // Check for empty or null environment IDs in the list
                for (var j = 0; j < rule.Environments.Count; j++)
                {
                    var envId = rule.Environments[j];

                    if (!string.IsNullOrWhiteSpace(envId))
                    {
                        continue;
                    }

                    var ruleIdInfo = !string.IsNullOrWhiteSpace(rule.Id) ? $" (id: '{rule.Id}')" : string.Empty;
                    errors.Add(string.Format(SR.ConfigValidator_RuleEnvironmentIdEmpty, ruleJsonPath, j, ruleIdInfo, j, "null or empty"));
                }
            }

            // Validate rule detail
            if (string.IsNullOrWhiteSpace(rule.RuleDetail.Id))
            {
                var ruleIdInfo = !string.IsNullOrWhiteSpace(rule.Id) ? $" (id: '{rule.Id}')" : string.Empty;
                errors.Add(string.Format(SR.ConfigValidator_RuleDetailIdRequired, ruleJsonPath, ruleIdInfo));
            }
            else
            {
                ValidateRuleDetail(rule.RuleDetail, rule.Id, ruleJsonPath, errors, supportedValidators);
            }
        }

        // Report duplicate rule IDs
        foreach (var duplicateId in duplicateRuleIds)
        {
            var duplicateIndices = new List<int>();

            for (var i = 0; i < config.Rules.Count; i++)
            {
                if (string.Equals(config.Rules[i].Id, duplicateId, StringComparison.OrdinalIgnoreCase))
                {
                    duplicateIndices.Add(i);
                }
            }

            var indicesStr = string.Join(", ", duplicateIndices.Select(i => $"$.rules[{i}]"));
            errors.Add(string.Format(SR.ConfigValidator_DuplicateRuleId, duplicateId, indicesStr, duplicateIndices.Count));
        }
    }

    /// <summary>
    /// Validates the details of a rule, including its ID, conditions, and associated validators.
    /// Checks for required fields, valid condition keys, supported validator types, and required values for specific validators.
    /// Adds any validation errors to the provided errors list.
    /// </summary>
    /// <param name="ruleDetail">The rule detail object to validate.</param>
    /// <param name="ruleId">The ID of the parent rule for context in error messages.</param>
    /// <param name="ruleJsonPath">The JSON path to the rule for error reporting.</param>
    /// <param name="errors">The list to which validation error messages will be added.</param>
    /// <param name="supportedValidators">A set of supported validator types for validation.</param>
    private void ValidateRuleDetail(RuleDetail ruleDetail, string ruleId, string ruleJsonPath, List<string> errors,
                                    HashSet<string> supportedValidators)
    {
        ValidateRuleDetailId(ruleDetail, ruleId, ruleJsonPath, errors);

        if (!ValidateConditions(ruleDetail, ruleId, ruleJsonPath, errors))
        {
            return; // Don't process conditions if validation failed
        }

        for (var i = 0; i < ruleDetail.Conditions.Count; i++)
        {
            var condition = ruleDetail.Conditions[i];
            var conditionJsonPath = $"{ruleJsonPath}.rule.conditions[{i}]";
            ValidateCondition(condition, ruleId, conditionJsonPath, i, errors, supportedValidators);
        }
    }

    /// <summary>
    /// Validates the rule detail ID.
    /// </summary>
    /// <param name="ruleDetail">The rule detail object to validate.</param>
    /// <param name="ruleId">The ID of the parent rule for context in error messages.</param>
    /// <param name="ruleJsonPath">The JSON path to the rule for error reporting.</param>
    /// <param name="errors">The list to which validation error messages will be added.</param>
    private static void ValidateRuleDetailId(RuleDetail ruleDetail, string ruleId, string ruleJsonPath, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(ruleDetail.Id))
        {
            errors.Add(string.Format(SR.ConfigValidator_RuleDetailIdRequiredInDetail, ruleJsonPath, ruleId));
        }
    }

    /// <summary>
    /// Validates the conditions array of a rule detail.
    /// Checks for empty conditions array and condition count limits.
    /// </summary>
    /// <param name="ruleDetail">The rule detail object to validate.</param>
    /// <param name="ruleId">The ID of the parent rule for context in error messages.</param>
    /// <param name="ruleJsonPath">The JSON path to the rule for error reporting.</param>
    /// <param name="errors">The list to which validation error messages will be added.</param>
    /// <returns><c>true</c> if conditions are valid and should be processed; otherwise, <c>false</c>.</returns>
    private bool ValidateConditions(RuleDetail ruleDetail, string ruleId, string ruleJsonPath, List<string> errors)
    {
        string ruleDetailIdInfo;

        if (ruleDetail.Conditions.Count == 0)
        {
            ruleDetailIdInfo = !string.IsNullOrWhiteSpace(ruleDetail.Id) ? $" (rule detail id: '{ruleDetail.Id}')" : string.Empty;
            errors.Add(string.Format(SR.ConfigValidator_ConditionsEmpty, ruleJsonPath, ruleDetailIdInfo, ruleId));
            return false;
        }

        if (ruleDetail.Conditions.Count <= _securityOptions.MaxConditionsPerRule)
        {
            return true;
        }

        ruleDetailIdInfo = !string.IsNullOrWhiteSpace(ruleDetail.Id) ? $" (rule detail id: '{ruleDetail.Id}')" : string.Empty;

        errors.Add(string.Format(SR.ConfigValidator_ConditionCountExceedsLimit, ruleJsonPath, ruleDetailIdInfo, ruleId, ruleDetail.Conditions.Count,
                                 _securityOptions.MaxConditionsPerRule, ruleDetail.Conditions.Count - _securityOptions.MaxConditionsPerRule));

        return false;
    }

    /// <summary>
    /// Validates a single condition, including its key and validators.
    /// </summary>
    /// <param name="condition">The condition object to validate.</param>
    /// <param name="ruleId">The ID of the parent rule for context in error messages.</param>
    /// <param name="conditionJsonPath">The JSON path to the condition for error reporting.</param>
    /// <param name="conditionIndex">The index of the condition in the conditions array.</param>
    /// <param name="errors">The list to which validation error messages will be added.</param>
    /// <param name="supportedValidators">A set of supported validator types for validation.</param>
    private void ValidateCondition(Condition condition, string ruleId, string conditionJsonPath, int conditionIndex, List<string> errors,
                                   HashSet<string> supportedValidators)
    {
        // Validate a condition key
        if (string.IsNullOrWhiteSpace(condition.Key))
        {
            errors.Add(string.Format(SR.ConfigValidator_ConditionKeyRequired, conditionJsonPath, ruleId, conditionIndex));
        }

        // Validate validators
        if (condition.Validators.Count == 0)
        {
            var keyInfo = !string.IsNullOrWhiteSpace(condition.Key) ? $" (key: '{condition.Key}')" : string.Empty;
            errors.Add(string.Format(SR.ConfigValidator_ValidatorsEmpty, conditionJsonPath, keyInfo, ruleId, conditionIndex));
        }
        else if (condition.Validators.Count > _securityOptions.MaxValidatorsPerCondition)
        {
            var keyInfo = !string.IsNullOrWhiteSpace(condition.Key) ? $" (key: '{condition.Key}')" : string.Empty;

            errors.Add(string.Format(SR.ConfigValidator_ValidatorCountExceedsLimit, conditionJsonPath, keyInfo, ruleId, condition.Validators.Count,
                                     _securityOptions.MaxValidatorsPerCondition,
                                     condition.Validators.Count - _securityOptions.MaxValidatorsPerCondition));
        }
        else
        {
            ValidateValidators(condition.Validators, conditionJsonPath, ruleId, errors, supportedValidators);
        }
    }

    /// <summary>
    /// Validates the validators within a condition.
    /// Checks for validator type, supported types, error messages, and required values.
    /// </summary>
    /// <param name="validators">The list of validators to validate.</param>
    /// <param name="conditionJsonPath">The JSON path to the condition for error reporting.</param>
    /// <param name="ruleId">The ID of the parent rule for context in error messages.</param>
    /// <param name="errors">The list to which validation error messages will be added.</param>
    /// <param name="supportedValidators">A set of supported validator types for validation.</param>
    private static void ValidateValidators(IReadOnlyList<ValidatorCondition> validators, string conditionJsonPath, string ruleId, List<string> errors,
                                           HashSet<string> supportedValidators)
    {
        for (var j = 0; j < validators.Count; j++)
        {
            var validator = validators[j];
            var validatorJsonPath = $"{conditionJsonPath}.condition[{j}]";

            // Validate validator type
            if (string.IsNullOrWhiteSpace(validator.Validator))
            {
                errors.Add(string.Format(SR.ConfigValidator_ValidatorTypeRequired, validatorJsonPath, ruleId, j));
            }
            else if (!supportedValidators.Contains(validator.Validator))
            {
                var supportedList = string.Join(", ", supportedValidators.OrderBy(v => v));
                errors.Add(string.Format(SR.ConfigValidator_UnsupportedValidatorType, validatorJsonPath, ruleId, validator.Validator, supportedList));
            }

            // Validate error message
            if (string.IsNullOrWhiteSpace(validator.Message))
            {
                var validatorTypeInfo = !string.IsNullOrWhiteSpace(validator.Validator) ? $" (validator: '{validator.Validator}')" : string.Empty;
                errors.Add(string.Format(SR.ConfigValidator_ValidatorMessageRequired, validatorJsonPath, validatorTypeInfo, ruleId, j));
            }

            // Validate value requirement for validators that need it
            if (string.IsNullOrWhiteSpace(validator.Validator))
            {
                continue;
            }

            var validatorType = validator.Validator.ToLowerInvariant();

            if (ValidatorConstants.ValidatorsRequiringValue.Any(v => v.Equals(validatorType, StringComparison.OrdinalIgnoreCase)) &&
                validator.Value == null)
            {
                errors.Add(string.Format(SR.ConfigValidator_ValidatorValueRequired, validatorJsonPath, ruleId, validator.Validator,
                                         validator.Validator));
            }
        }
    }

    /// <summary>
    /// Validates that all environment IDs referenced by rules exist in the configuration's environments list.
    /// Adds any validation errors to the provided errors list.
    /// </summary>
    /// <param name="config">The configuration object containing environments and rules.</param>
    /// <param name="errors">The list to which validation error messages will be added.</param>
    private static void ValidateRuleEnvironmentMappings(SGuardConfig config, List<string> errors)
    {
        if (config.Environments.Count == 0 || config.Rules.Count == 0)
        {
            return;
        }

        // Optimized: foreach loop with direct HashSet.Add instead of LINQ Select()
        var environmentIdSet = new HashSet<string>(config.Environments.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var environment in config.Environments)
        {
            if (!string.IsNullOrWhiteSpace(environment.Id))
            {
                environmentIdSet.Add(environment.Id);
            }
        }

        foreach (var rule in config.Rules.Where(e => e.Environments.Count > 0))
        {
            foreach (var envId in rule.Environments)
            {
                if (string.IsNullOrWhiteSpace(envId))
                {
                    // Already reported in ValidateRules
                    continue;
                }

                if (environmentIdSet.Contains(envId))
                {
                    continue;
                }

                var ruleIndex = config.Rules.IndexOf(rule);
                var envIndex = rule.Environments.IndexOf(envId);
                var availableEnvs = string.Join(", ", environmentIdSet.OrderBy(e => e));
                errors.Add(string.Format(SR.ConfigValidator_RuleEnvironmentNotFound, ruleIndex, envIndex, rule.Id, envId, availableEnvs));
            }
        }
    }

    /// <summary>
    /// Validates the format of an environment path.
    /// Checks for invalid characters, excessive parent directory references,
    /// and path length constraints. Returns an error message if the path is invalid; otherwise, returns <c>null</c>.
    /// </summary>
    /// <param name="path">The environment path to validate.</param>
    /// <returns>
    /// An error message describing the path format issue if invalid; otherwise, <c>null</c>.
    /// </returns>
    private static string? ValidatePathFormat(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null; // Already validated in ValidateEnvironments
        }

        // Check for null bytes (common injection attack vector)
        if (path.Contains('\0'))
        {
            return string.Format(SR.ConfigValidator_PathContainsNullByte, path);
        }

        // Check for control characters (except common whitespace)
        if (path.Any(c => char.IsControl(c) && c != '\t' && c != '\n' && c != '\r'))
        {
            var controlChars = path.Where(c => char.IsControl(c) && c != '\t' && c != '\n' && c != '\r').Distinct().ToList();
            var controlCharInfo = string.Join(", ", controlChars.Select(c => $"U+{(int)c:X4}"));
            return string.Format(SR.ConfigValidator_PathContainsControlChars, path, controlCharInfo);
        }

        // Check for invalid characters in a path
        var invalidChars = Path.GetInvalidPathChars();
        var foundInvalidChars = path.Where(c => invalidChars.Contains(c)).Distinct().ToList();

        if (foundInvalidChars.Count > 0)
        {
            var invalidCharInfo = string.Join(", ", foundInvalidChars.Select(c => $"'{c}' (U+{(int)c:X4})"));
            return string.Format(SR.ConfigValidator_PathContainsInvalidChars, path, invalidCharInfo);
        }

        // Check for path traversal attempts (stricter)
        if (path.Contains("..", StringComparison.Ordinal))
        {
            // Count consecutive ... patterns at the start of the path
            var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            var consecutiveUpLevels = 0;
            var maxConsecutiveUpLevels = 0;

            foreach (var part in parts)
            {
                if (part == "..")
                {
                    consecutiveUpLevels++;
                    maxConsecutiveUpLevels = Math.Max(maxConsecutiveUpLevels, consecutiveUpLevels);
                }
                else if (!string.IsNullOrWhiteSpace(part) && part != ".")
                {
                    // Reset counter when we find a non-relative part
                    consecutiveUpLevels = 0;
                }
            }

            // Allow reasonable relative paths (up to 2 levels for security)
            if (maxConsecutiveUpLevels > 2)
            {
                return string.Format(SR.ConfigValidator_PathTooManyParentRefs, path, maxConsecutiveUpLevels);
            }
        }

        // Check for double slashes (potential path manipulation)
        if (path.Contains("//", StringComparison.Ordinal) || path.Contains("\\\\", StringComparison.Ordinal))
        {
            return string.Format(SR.ConfigValidator_PathConsecutiveSeparators, path);
        }

        return path.Length switch
        {
            // Check path length using SecurityConstants
            > SecurityConstants.MaxPathLength => string.Format(SR.ConfigValidator_PathTooLong, path.Length, SecurityConstants.MaxPathLength, path),
            // Check for suspicious patterns (absolute paths on Unix-like systems starting with /)
            // This is informational - we allow absolute paths but log them
            > 1 when path[0] == '/' && path.Contains("..") => string.Format(SR.ConfigValidator_PathAbsoluteWithParentRefs, path),
            _                                              => null
        };
    }
}