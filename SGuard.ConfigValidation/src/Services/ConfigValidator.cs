using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Models;
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> or <paramref name="securityOptions"/> is null.</exception>
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> or <paramref name="supportedValidators"/> is null.</exception>
    public List<string> Validate(SGuardConfig config, IEnumerable<string> supportedValidators)
    {
        _logger.LogDebug("Starting configuration validation");
        var errors = new List<string>();

        // Get supported validators from factory if available, otherwise use a provided list
        HashSet<string> supportedValidatorSet;

        if (_validatorFactory != null)
        {
            supportedValidatorSet = new HashSet<string>(_validatorFactory.GetSupportedValidators(), StringComparer.OrdinalIgnoreCase);
            _logger.LogDebug("Using validators from ValidatorFactory: {Validators}", string.Join(", ", supportedValidatorSet));
        }
        else
        {
            supportedValidatorSet = new HashSet<string>(supportedValidators, StringComparer.OrdinalIgnoreCase);
            _logger.LogDebug("Using provided validators: {Validators}", string.Join(", ", supportedValidatorSet));
        }

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
        else
        {
            _logger.LogDebug("Configuration validation passed");
        }

        return errors;
    }

    /// <summary>
    /// Validates the version field of the configuration.
    /// Checks that version is not null or empty.
    /// </summary>
    /// <param name="config">The configuration object to validate.</param>
    /// <param name="errors">The list to which validation error messages will be added.</param>
    private static void ValidateVersion(SGuardConfig config, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(config.Version))
        {
            errors.Add("Configuration validation failed: Required property 'version' is missing or empty at JSON path '$.version'. " +
                       "The 'version' property is required and must contain a non-empty string value.");
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
            errors.Add("Configuration validation failed: Required array 'environments' is empty at JSON path '$.environments'. " +
                       "At least one environment definition is required. " +
                       "Please add an 'environments' array with at least one environment object containing 'id', 'name', and 'path' properties.");
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
                errors.Add($"Configuration validation failed: Required property 'id' is missing or empty at JSON path '{jsonPath}.id'. " +
                          $"Environment at index {i} must have a non-empty 'id' property.");
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
                errors.Add($"Configuration validation failed: Required property 'name' is missing or empty at JSON path '{jsonPath}.name'{envIdInfo}. " +
                          $"Environment at index {i} must have a non-empty 'name' property.");
            }

            if (string.IsNullOrWhiteSpace(env.Path))
            {
                var envIdInfo = !string.IsNullOrWhiteSpace(env.Id) ? $" (id: '{env.Id}')" : string.Empty;
                errors.Add($"Configuration validation failed: Required property 'path' is missing or empty at JSON path '{jsonPath}.path'{envIdInfo}. " +
                          $"Environment at index {i} must have a non-empty 'path' property pointing to the app settings file.");
            }
            else
            {
                // Validate a path format (basic validation - no file existence check)
                var pathValidationError = ValidatePathFormat(env.Path);

                if (!string.IsNullOrWhiteSpace(pathValidationError))
                {
                    var envIdInfo = !string.IsNullOrWhiteSpace(env.Id) ? $" (id: '{env.Id}')" : string.Empty;
                    errors.Add($"Configuration validation failed: Invalid path format at JSON path '{jsonPath}.path'{envIdInfo}. " +
                              $"Path value: '{env.Path}'. " +
                              $"{pathValidationError}");
                }
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
            errors.Add($"Configuration validation failed: Duplicate environment ID '{duplicateId}' found at JSON paths: {indicesStr}. " +
                      $"Environment IDs must be unique. " +
                      $"Found {duplicateIndices.Count} environment(s) with the same ID.");
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
            // Rules can be empty, but if present, they should be valid
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
                errors.Add($"Configuration validation failed: Required property 'id' is missing or empty at JSON path '{ruleJsonPath}.id'. " +
                          $"Rule at index {i} must have a non-empty 'id' property.");
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
                errors.Add($"Configuration validation failed: Required array 'environments' is empty at JSON path '{ruleJsonPath}.environments'{ruleIdInfo}. " +
                          $"Rule at index {i} must have at least one environment ID in the 'environments' array.");
            }
            else
            {
                // Check for empty or null environment IDs in the list
                for (var j = 0; j < rule.Environments.Count; j++)
                {
                    var envId = rule.Environments[j];

                    if (string.IsNullOrWhiteSpace(envId))
                    {
                        var ruleIdInfo = !string.IsNullOrWhiteSpace(rule.Id) ? $" (id: '{rule.Id}')" : string.Empty;
                        errors.Add($"Configuration validation failed: Empty or null environment ID at JSON path '{ruleJsonPath}.environments[{j}]'{ruleIdInfo}. " +
                                  $"Environment ID at index {j} in the 'environments' array cannot be null or empty. " +
                                  $"Value found: {(envId == null ? "null" : "empty string")}.");
                    }
                }
            }

            // Validate rule detail
            if (rule.RuleDetail == null)
            {
                var ruleIdInfo = !string.IsNullOrWhiteSpace(rule.Id) ? $" (id: '{rule.Id}')" : string.Empty;
                errors.Add($"Configuration validation failed: Required property 'rule' is missing or null at JSON path '{ruleJsonPath}.rule'{ruleIdInfo}. " +
                          $"Rule at index {i} must have a 'rule' object containing validation conditions.");
            }
            else if (string.IsNullOrWhiteSpace(rule.RuleDetail.Id))
            {
                var ruleIdInfo = !string.IsNullOrWhiteSpace(rule.Id) ? $" (id: '{rule.Id}')" : string.Empty;
                errors.Add($"Configuration validation failed: Required property 'id' is missing or empty at JSON path '{ruleJsonPath}.rule.id'{ruleIdInfo}. " +
                          $"The 'rule' object must have a non-empty 'id' property.");
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
            errors.Add($"Configuration validation failed: Duplicate rule ID '{duplicateId}' found at JSON paths: {indicesStr}. " +
                      $"Rule IDs must be unique. " +
                      $"Found {duplicateIndices.Count} rule(s) with the same ID.");
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
    private void ValidateRuleDetail(RuleDetail ruleDetail, string ruleId, string ruleJsonPath, List<string> errors, HashSet<string> supportedValidators)
    {
        // Validate rule detail ID
        if (string.IsNullOrWhiteSpace(ruleDetail.Id))
        {
            errors.Add($"Configuration validation failed: Required property 'id' is missing or empty at JSON path '{ruleJsonPath}.rule.id'. " +
                      $"Rule ID: '{ruleId}'. " +
                      "The 'rule' object must have a non-empty 'id' property.");
        }

        // Validate conditions
        if (ruleDetail.Conditions.Count == 0)
        {
            var ruleDetailIdInfo = !string.IsNullOrWhiteSpace(ruleDetail.Id) ? $" (rule detail id: '{ruleDetail.Id}')" : string.Empty;
            errors.Add($"Configuration validation failed: Required array 'conditions' is empty at JSON path '{ruleJsonPath}.rule.conditions'{ruleDetailIdInfo}. " +
                      $"Rule ID: '{ruleId}'. " +
                      "At least one condition is required. " +
                      "Please add a 'conditions' array with at least one condition object containing 'key' and 'condition' properties.");
            return;
        }

        // Validate condition count to prevent DoS attacks
        if (ruleDetail.Conditions.Count > _securityOptions.MaxConditionsPerRule)
        {
            var ruleDetailIdInfo = !string.IsNullOrWhiteSpace(ruleDetail.Id) ? $" (rule detail id: '{ruleDetail.Id}')" : string.Empty;
            errors.Add($"Configuration validation failed: Condition count exceeds security limit at JSON path '{ruleJsonPath}.rule.conditions'{ruleDetailIdInfo}. " +
                      $"Rule ID: '{ruleId}'. " +
                      $"Actual count: {ruleDetail.Conditions.Count} conditions. " +
                      $"Maximum allowed: {_securityOptions.MaxConditionsPerRule} conditions. " +
                      $"Exceeded by: {ruleDetail.Conditions.Count - _securityOptions.MaxConditionsPerRule} conditions. " +
                      "This may indicate a DoS attack attempt. " +
                      "Please reduce the number of conditions or contact your administrator to adjust the security limits.");
            return; // Don't process excessive conditions
        }

        for (var i = 0; i < ruleDetail.Conditions.Count; i++)
        {
            var condition = ruleDetail.Conditions[i];
            var conditionJsonPath = $"{ruleJsonPath}.rule.conditions[{i}]";

            // Validate a condition key
            if (string.IsNullOrWhiteSpace(condition.Key))
            {
                errors.Add($"Configuration validation failed: Required property 'key' is missing or empty at JSON path '{conditionJsonPath}.key'. " +
                          $"Rule ID: '{ruleId}'. " +
                          $"Condition at index {i} must have a non-empty 'key' property specifying the configuration key to validate.");
            }

            // Validate validators
            if (condition.Validators.Count == 0)
            {
                var keyInfo = !string.IsNullOrWhiteSpace(condition.Key) ? $" (key: '{condition.Key}')" : string.Empty;
                errors.Add($"Configuration validation failed: Required array 'condition' is empty at JSON path '{conditionJsonPath}.condition'{keyInfo}. " +
                          $"Rule ID: '{ruleId}'. " +
                          $"Condition at index {i} must have at least one validator in the 'condition' array.");
            }
            else if (condition.Validators.Count > _securityOptions.MaxValidatorsPerCondition)
            {
                var keyInfo = !string.IsNullOrWhiteSpace(condition.Key) ? $" (key: '{condition.Key}')" : string.Empty;
                errors.Add($"Configuration validation failed: Validator count exceeds security limit at JSON path '{conditionJsonPath}.condition'{keyInfo}. " +
                          $"Rule ID: '{ruleId}'. " +
                          $"Actual count: {condition.Validators.Count} validators. " +
                          $"Maximum allowed: {_securityOptions.MaxValidatorsPerCondition} validators. " +
                          $"Exceeded by: {condition.Validators.Count - _securityOptions.MaxValidatorsPerCondition} validators. " +
                          "This may indicate a DoS attack attempt. " +
                          "Please reduce the number of validators or contact your administrator to adjust the security limits.");
                // Don't process excessive validators, but continue with other conditions
                continue;
            }
            else
            {
                for (var j = 0; j < condition.Validators.Count; j++)
                {
                    var validator = condition.Validators[j];
                    var validatorJsonPath = $"{conditionJsonPath}.condition[{j}]";

                    // Validate validator type
                    if (string.IsNullOrWhiteSpace(validator.Validator))
                    {
                        errors.Add($"Configuration validation failed: Required property 'validator' is missing or empty at JSON path '{validatorJsonPath}.validator'. " +
                                  $"Rule ID: '{ruleId}'. " +
                                  $"Validator at index {j} must have a non-empty 'validator' property specifying the validator type.");
                    }
                    else if (!supportedValidators.Contains(validator.Validator))
                    {
                        var supportedList = string.Join(", ", supportedValidators.OrderBy(v => v));
                        errors.Add($"Configuration validation failed: Unsupported validator type at JSON path '{validatorJsonPath}.validator'. " +
                                  $"Rule ID: '{ruleId}'. " +
                                  $"Found validator type: '{validator.Validator}'. " +
                                  $"Supported validator types: {supportedList}. " +
                                  "Please use one of the supported validator types.");
                    }

                    // Validate error message
                    if (string.IsNullOrWhiteSpace(validator.Message))
                    {
                        var validatorTypeInfo = !string.IsNullOrWhiteSpace(validator.Validator) ? $" (validator: '{validator.Validator}')" : string.Empty;
                        errors.Add($"Configuration validation failed: Required property 'message' is missing or empty at JSON path '{validatorJsonPath}.message'{validatorTypeInfo}. " +
                                  $"Rule ID: '{ruleId}'. " +
                                  $"Validator at index {j} must have a non-empty 'message' property containing the error message to display when validation fails.");
                    }

                    // Validate value requirement for validators that need it
                    if (string.IsNullOrWhiteSpace(validator.Validator))
                    {
                        continue;
                    }

                    var validatorType = validator.Validator.ToLowerInvariant();

                    if (ValidatorConstants.ValidatorsRequiringValue.Contains(validatorType) && validator.Value == null)
                    {
                        errors.Add($"Configuration validation failed: Required property 'value' is missing or null at JSON path '{validatorJsonPath}.value'. " +
                                  $"Rule ID: '{ruleId}'. " +
                                  $"Validator type: '{validator.Validator}'. " +
                                  $"The '{validator.Validator}' validator requires a 'value' property to specify the expected value for comparison. " +
                                  "Please add a 'value' property to this validator.");
                    }
                }
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
                
                if (!environmentIdSet.Contains(envId))
                {
                    var ruleIndex = config.Rules.IndexOf(rule);
                    var envIndex = rule.Environments.IndexOf(envId);
                    var availableEnvs = string.Join(", ", environmentIdSet.OrderBy(e => e));
                    errors.Add(
                        $"Configuration validation failed: Rule references non-existent environment ID at JSON path '$.rules[{ruleIndex}].environments[{envIndex}]'. " +
                        $"Rule ID: '{rule.Id}'. " +
                        $"Referenced environment ID: '{envId}'. " +
                        $"Available environment IDs: {availableEnvs}. " +
                        "Please ensure the environment ID exists in the 'environments' array or remove it from the rule's 'environments' list.");
                }
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
            return $"Path contains null byte (\\0), which is not allowed. " +
                   $"Path value: '{path}'. " +
                   "Null bytes are not valid in file paths and may indicate a security issue. " +
                   "Please remove any null bytes from the path.";
        }

        // Check for control characters (except common whitespace)
        if (path.Any(c => char.IsControl(c) && c != '\t' && c != '\n' && c != '\r'))
        {
            var controlChars = path.Where(c => char.IsControl(c) && c != '\t' && c != '\n' && c != '\r').Distinct().ToList();
            var controlCharInfo = string.Join(", ", controlChars.Select(c => $"U+{(int)c:X4}"));
            return $"Path contains control characters, which are not allowed. " +
                   $"Path value: '{path}'. " +
                   $"Control characters found: {controlCharInfo}. " +
                   "Control characters (except tab, newline, carriage return) are not valid in file paths. " +
                   "Please remove any control characters from the path.";
        }

        // Check for invalid characters in a path
        var invalidChars = Path.GetInvalidPathChars();
        var foundInvalidChars = path.Where(c => invalidChars.Contains(c)).Distinct().ToList();
        if (foundInvalidChars.Count > 0)
        {
            var invalidCharInfo = string.Join(", ", foundInvalidChars.Select(c => $"'{c}' (U+{(int)c:X4})"));
            return $"Path contains invalid characters for the current platform. " +
                   $"Path value: '{path}'. " +
                   $"Invalid characters found: {invalidCharInfo}. " +
                   "These characters are not allowed in file paths on this operating system. " +
                   "Please remove or replace these characters.";
        }

        // Check for path traversal attempts (more strict)
        if (path.Contains("..", StringComparison.Ordinal))
        {
            // Count consecutive .. patterns at the start of the path
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
                return $"Path contains too many parent directory references (..). " +
                       $"Path value: '{path}'. " +
                       $"Found {maxConsecutiveUpLevels} consecutive parent directory references. " +
                       $"Maximum allowed: 2 levels. " +
                       "This may indicate a path traversal attempt. " +
                       "Please use absolute paths or paths relative to the configuration file with no more than 2 parent directory references.";
            }
        }

        // Check for double slashes (potential path manipulation)
        if (path.Contains("//", StringComparison.Ordinal) || path.Contains("\\\\", StringComparison.Ordinal))
        {
            return $"Path contains consecutive path separators, which may indicate path manipulation: '{path}'";
        }

        // Check path length using SecurityConstants
        if (path.Length > SecurityConstants.MaxPathLength)
        {
            return $"Path is too long ({path.Length} characters). Maximum allowed: {SecurityConstants.MaxPathLength} characters: '{path}'";
        }

        // Check for suspicious patterns (absolute paths on Unix-like systems starting with /)
        // This is informational - we allow absolute paths but log them
        if (path.Length > 1 && path[0] == '/' && path.Contains(".."))
        {
            // Absolute path with .. is suspicious
            return $"Absolute path contains parent directory references (..), which may indicate path manipulation: '{path}'";
        }

        return null; // Path format is valid
    }
}