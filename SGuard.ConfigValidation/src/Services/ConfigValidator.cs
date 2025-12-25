using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Validates SGuard configuration structure, uniqueness, and integrity.
/// </summary>
public sealed class ConfigValidator : IConfigValidator
{
    private readonly IValidatorFactory? _validatorFactory;
    private readonly ILogger<ConfigValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the ConfigValidator class.
    /// </summary>
    /// <param name="validatorFactory">Optional validator factory. If provided, supported validators will be retrieved from the factory.</param>
    /// <param name="logger">Logger instance for logging validation operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public ConfigValidator(IValidatorFactory? validatorFactory, ILogger<ConfigValidator> logger)
    {
        _validatorFactory = validatorFactory;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            errors.Add("Configuration 'version' is required and cannot be empty.");
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
            errors.Add("Configuration must contain at least one environment.");
            return;
        }

        var environmentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateIds = new HashSet<string>();

        for (var i = 0; i < config.Environments.Count; i++)
        {
            var env = config.Environments[i];
            var prefix = $"Environment[{i}]";

            // Validate required fields
            if (string.IsNullOrWhiteSpace(env.Id))
            {
                errors.Add($"{prefix}: 'id' is required and cannot be empty.");
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
                errors.Add($"{prefix} (id: '{env.Id}'): 'name' is required and cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(env.Path))
            {
                errors.Add($"{prefix} (id: '{env.Id}'): 'path' is required and cannot be empty.");
            }
            else
            {
                // Validate a path format (basic validation - no file existence check)
                var pathValidationError = ValidatePathFormat(env.Path);

                if (!string.IsNullOrWhiteSpace(pathValidationError))
                {
                    errors.Add($"{prefix} (id: '{env.Id}'): {pathValidationError}");
                }
            }
        }

        // Report duplicate IDs
        foreach (var duplicateId in duplicateIds)
        {
            errors.Add($"Duplicate environment ID found: '{duplicateId}'. Environment IDs must be unique.");
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
    private static void ValidateRules(SGuardConfig config, List<string> errors, HashSet<string> supportedValidators)
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
            var prefix = $"Rule[{i}]";

            // Validate rule ID
            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                errors.Add($"{prefix}: 'id' is required and cannot be empty.");
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
                errors.Add($"{prefix} (id: '{rule.Id}'): 'environments' array is required and must contain at least one environment ID.");
            }
            else
            {
                // Check for empty or null environment IDs in the list
                for (var j = 0; j < rule.Environments.Count; j++)
                {
                    var envId = rule.Environments[j];

                    if (string.IsNullOrWhiteSpace(envId))
                    {
                        errors.Add($"{prefix} (id: '{rule.Id}'): 'environments[{j}]' cannot be null or empty.");
                    }
                }
            }

            // Validate rule detail
            if (rule.RuleDetail == null)
            {
                errors.Add($"{prefix} (id: '{rule.Id}'): 'rule' object is required.");
            }
            else if (string.IsNullOrWhiteSpace(rule.RuleDetail.Id))
            {
                errors.Add($"{prefix} (id: '{rule.Id}').rule: 'id' is required and cannot be empty.");
            }
            else
            {
                ValidateRuleDetail(rule.RuleDetail, rule.Id, errors, supportedValidators);
            }
        }

        // Report duplicate rule IDs
        foreach (var duplicateId in duplicateRuleIds)
        {
            errors.Add($"Duplicate rule ID found: '{duplicateId}'. Rule IDs must be unique.");
        }
    }

    /// <summary>
    /// Validates the details of a rule, including its ID, conditions, and associated validators.
    /// Checks for required fields, valid condition keys, supported validator types, and required values for specific validators.
    /// Adds any validation errors to the provided errors list.
    /// </summary>
    /// <param name="ruleDetail">The rule detail object to validate.</param>
    /// <param name="ruleId">The ID of the parent rule for context in error messages.</param>
    /// <param name="errors">The list to which validation error messages will be added.</param>
    /// <param name="supportedValidators">A set of supported validator types for validation.</param>
    private static void ValidateRuleDetail(RuleDetail ruleDetail, string ruleId, List<string> errors, HashSet<string> supportedValidators)
    {
        var prefix = $"Rule (id: '{ruleId}').rule";

        // Validate rule detail ID
        if (string.IsNullOrWhiteSpace(ruleDetail.Id))
        {
            errors.Add($"{prefix}: 'id' is required and cannot be empty.");
        }

        // Validate conditions
        if (ruleDetail.Conditions.Count == 0)
        {
            errors.Add($"{prefix} (id: '{ruleDetail.Id}'): 'conditions' array is required and must contain at least one condition.");
            return;
        }

        for (var i = 0; i < ruleDetail.Conditions.Count; i++)
        {
            var condition = ruleDetail.Conditions[i];
            var conditionPrefix = $"{prefix}.conditions[{i}]";

            // Validate a condition key
            if (string.IsNullOrWhiteSpace(condition.Key))
            {
                errors.Add($"{conditionPrefix}: 'key' is required and cannot be empty.");
            }

            // Validate validators
            if (condition.Validators.Count == 0)
            {
                errors.Add($"{conditionPrefix} (key: '{condition.Key}'): 'condition' array is required and must contain at least one validator.");
            }
            else
            {
                for (var j = 0; j < condition.Validators.Count; j++)
                {
                    var validator = condition.Validators[j];
                    var validatorPrefix = $"{conditionPrefix}.condition[{j}]";

                    // Validate validator type
                    if (string.IsNullOrWhiteSpace(validator.Validator))
                    {
                        errors.Add($"{validatorPrefix}: 'validator' is required and cannot be empty.");
                    }
                    else if (!supportedValidators.Contains(validator.Validator))
                    {
                        errors.Add(
                            $"{validatorPrefix}: 'validator' type '{validator.Validator}' is not supported. Supported validators: {string.Join(", ", supportedValidators)}.");
                    }

                    // Validate error message
                    if (string.IsNullOrWhiteSpace(validator.Message))
                    {
                        errors.Add($"{validatorPrefix} (validator: '{validator.Validator}'): 'message' is required and cannot be empty.");
                    }

                    // Validate value requirement for validators that need it
                    if (string.IsNullOrWhiteSpace(validator.Validator))
                    {
                        continue;
                    }

                    var validatorType = validator.Validator.ToLowerInvariant();

                    if (ValidatorConstants.ValidatorsRequiringValue.Contains(validatorType) && validator.Value == null)
                    {
                        errors.Add($"{validatorPrefix} (validator: '{validator.Validator}'): 'value' is required for this validator type.");
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
                    errors.Add(
                        $"Rule (id: '{rule.Id}') references environment ID '{envId}' which does not exist. Available environment IDs: {string.Join(", ", environmentIdSet)}.");
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

        // Check for invalid characters in a path
        var invalidChars = Path.GetInvalidPathChars();

        if (path.IndexOfAny(invalidChars) >= 0)
        {
            return $"Path contains invalid characters: '{path}'";
        }

        // Check for relative path indicators that might be problematic
        // Relative paths starting with .. are allowed but warn if they go too far up
        if (!path.StartsWith("..", StringComparison.Ordinal))
        {
            return path.Length > 500 ? $"Path is too long (maximum 500 characters): '{path}'" : null;
        }

        var upLevels = 0;
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (var part in parts)
        {
            if (part == "..")
            {
                upLevels++;
            }
            else if (!string.IsNullOrWhiteSpace(part) && part != ".")
            {
                upLevels = 0; // Reset when we find a non-relative part
            }
        }

        // Allow reasonable relative paths (up to 3 levels)
        if (upLevels > 3)
        {
            return
                $"Path contains too many parent directory references (..): '{path}'. Consider using absolute paths or paths relative to the configuration file.";
        }

        // Check path length (Windows MAX_PATH is 260, but we'll be more lenient)
        return path.Length > 500 ? $"Path is too long (maximum 500 characters): '{path}'" : null; // Path format is valid
    }
}