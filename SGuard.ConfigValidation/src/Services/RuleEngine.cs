using System.Text.Json;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Exceptions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Engine for executing validation rules against configurations.
/// </summary>
public sealed class RuleEngine : IRuleEngine
{
    private readonly IConfigLoader _configLoader;
    private readonly IFileValidator _fileValidator;
    private readonly IValidatorFactory _validatorFactory;
    private readonly IConfigValidator? _configValidator;
    private readonly IPathResolver _pathResolver;
    private readonly ILogger<RuleEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the RuleEngine class.
    /// </summary>
    /// <param name="configLoader">The configuration loader service.</param>
    /// <param name="fileValidator">The file validator service.</param>
    /// <param name="validatorFactory">The validator factory.</param>
    /// <param name="pathResolver">The path resolver service.</param>
    /// <param name="configValidator">Optional configuration validator for JSON-based validation.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public RuleEngine(
        IConfigLoader configLoader,
        IFileValidator fileValidator,
        IValidatorFactory validatorFactory,
        ILogger<RuleEngine> logger,
        IPathResolver? pathResolver = null,
        IConfigValidator? configValidator = null)
    {
        _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
        _fileValidator = fileValidator ?? throw new ArgumentNullException(nameof(fileValidator));
        _validatorFactory = validatorFactory ?? throw new ArgumentNullException(nameof(validatorFactory));
        _configValidator = configValidator;
        _pathResolver = pathResolver ?? new PathResolver();
        _logger = logger;
    }

    /// <summary>
    /// Validates a specific environment from a configuration file.
    /// </summary>
    /// <param name="configPath">The path to the configuration file.</param>
    /// <param name="environmentId">The ID of the environment to validate.</param>
    /// <returns>A <see cref="RuleEngineResult"/> containing the validation results for the specified environment.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the configuration file or environment file does not exist.</exception>
    /// <exception cref="ConfigurationException">Thrown when the configuration is invalid or the environment is not found.</exception>
    public RuleEngineResult ValidateEnvironment(string configPath, string environmentId)
    {
        return ValidateEnvironmentInternal(
            () => _configLoader.LoadConfig(configPath),
            environmentId,
            (config, env) => ValidateSingleEnvironment(env, config, environmentId, configPath),
            $"from config {configPath}",
            requirePath: true);
    }

    /// <summary>
    /// Validates all environments from a configuration file.
    /// </summary>
    /// <param name="configPath">The path to the configuration file.</param>
    /// <returns>A <see cref="RuleEngineResult"/> containing validation results for all environments.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the configuration file does not exist.</exception>
    /// <exception cref="ConfigurationException">Thrown when the configuration is invalid.</exception>
    public RuleEngineResult ValidateAllEnvironments(string configPath)
    {
        return ValidateAllEnvironmentsCommon(
            () => _configLoader.LoadConfig(configPath),
            config => ValidateAllEnvironmentsInternal(config, configPath),
            $"from config {configPath}");
    }

    /// <summary>
    /// Validates a specific environment from a JSON configuration string.
    /// </summary>
    /// <param name="configJson">The JSON configuration string.</param>
    /// <param name="environmentId">The ID of the environment to validate.</param>
    /// <returns>A <see cref="RuleEngineResult"/> containing the validation results for the specified environment.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="configJson"/> is null or empty, or when <paramref name="environmentId"/> is null or empty.</exception>
    /// <exception cref="ConfigurationException">Thrown when the JSON configuration is invalid or the environment is not found.</exception>
    public RuleEngineResult ValidateEnvironmentFromJson(string configJson, string environmentId)
    {
        return ValidateEnvironmentInternal(
            () => ParseConfigJson(configJson),
            environmentId,
            (config, env) => ValidateSingleEnvironmentFromJson(env, config, environmentId),
            "from JSON",
            requirePath: false);
    }

    /// <summary>
    /// Validates all environments from a JSON configuration string.
    /// </summary>
    /// <param name="configJson">The JSON configuration string.</param>
    /// <returns>A <see cref="RuleEngineResult"/> containing validation results for all environments.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="configJson"/> is null or empty.</exception>
    /// <exception cref="ConfigurationException">Thrown when the JSON configuration is invalid.</exception>
    public RuleEngineResult ValidateAllEnvironmentsFromJson(string configJson)
    {
        return ValidateAllEnvironmentsCommon(
            () => ParseConfigJson(configJson),
            ValidateAllEnvironmentsFromJsonInternal,
            "from JSON");
    }

    /// <summary>
    /// Gets the list of supported validator types.
    /// </summary>
    /// <returns>An enumerable of supported validator type names (e.g., "required", "eq", "gt").</returns>
    public IEnumerable<string> GetSupportedValidators()
    {
        return _validatorFactory.GetSupportedValidators();
    }

    /// <summary>
    /// Common validation logic for single environment validation (file-based or JSON-based).
    /// </summary>
    private RuleEngineResult ValidateEnvironmentInternal(
        Func<SGuardConfig> loadConfig,
        string environmentId,
        Func<SGuardConfig, Models.Environment, FileValidationResult> validateEnvironment,
        string contextDescription,
        bool requirePath = false)
    {
        _logger.LogDebug("Starting validation for environment {EnvironmentId} {ContextDescription}", 
            environmentId, contextDescription);

        if (string.IsNullOrWhiteSpace(environmentId))
        {
            _logger.LogWarning("Validation failed: Environment ID is null or empty");
            return RuleEngineResult.CreateError("Environment ID cannot be null or empty.");
        }

        try
        {
            var config = loadConfig();
            _logger.LogDebug("Configuration loaded successfully. Found {EnvironmentCount} environments and {RuleCount} rules", 
                config.Environments.Count, config.Rules.Count);

            var environment = FindEnvironment(config, environmentId);
            if (environment == null)
            {
                return CreateEnvironmentNotFoundError(config, environmentId);
            }

            // For file-based validation, check path validity
            if (requirePath && string.IsNullOrWhiteSpace(environment.Path))
            {
                _logger.LogWarning("Environment {EnvironmentId} has an invalid or empty path", environmentId);
                return RuleEngineResult.CreateError($"Environment '{environmentId}' has an invalid or empty path.");
            }

            var validationResult = validateEnvironment(config, environment);
            LogValidationResult(environmentId, validationResult);
            return RuleEngineResult.CreateSuccess(validationResult);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "File not found during validation of environment {EnvironmentId} {ContextDescription}", 
                environmentId, contextDescription);
            return RuleEngineResult.CreateError($"File not found: {ex.Message}", ex);
        }
        catch (ConfigurationException ex)
        {
            _logger.LogError(ex, "Configuration error during validation of environment {EnvironmentId} {ContextDescription}", 
                environmentId, contextDescription);
            return RuleEngineResult.CreateError($"Configuration error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during validation of environment {EnvironmentId} {ContextDescription}", 
                environmentId, contextDescription);
            return HandleValidationError(ex);
        }
    }

    /// <summary>
    /// Common validation logic for all environments validation (file-based or JSON-based).
    /// </summary>
    private RuleEngineResult ValidateAllEnvironmentsCommon(
        Func<SGuardConfig> loadConfig,
        Func<SGuardConfig, List<FileValidationResult>> validateAllEnvironments,
        string contextDescription)
    {
        _logger.LogDebug("Starting validation for all environments {ContextDescription}", contextDescription);

        try
        {
            var config = loadConfig();
            _logger.LogDebug("Configuration loaded successfully. Found {EnvironmentCount} environments and {RuleCount} rules", 
                config.Environments.Count, config.Rules.Count);

            if (config.Environments.Count == 0)
            {
                _logger.LogWarning("No environments found in configuration {ContextDescription}", contextDescription);
                return RuleEngineResult.CreateError("No environments found in configuration.");
            }

            _logger.LogInformation("Validating {EnvironmentCount} environments {ContextDescription}", 
                config.Environments.Count, contextDescription);
            var results = validateAllEnvironments(config);
            
            var totalErrors = results.Sum(r => r.Results.Count(res => !res.IsValid));
            var successfulEnvironments = results.Count(r => r.Results.All(res => res.IsValid));
            
            _logger.LogInformation("Validation completed for all environments {ContextDescription}. Successful: {SuccessfulCount}, Total errors: {TotalErrors}", 
                contextDescription, successfulEnvironments, totalErrors);

            return RuleEngineResult.CreateSuccess(results);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "File not found during validation of all environments {ContextDescription}", contextDescription);
            return RuleEngineResult.CreateError($"File not found: {ex.Message}", ex);
        }
        catch (ConfigurationException ex)
        {
            _logger.LogError(ex, "Configuration error during validation of all environments {ContextDescription}", contextDescription);
            return RuleEngineResult.CreateError($"Configuration error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during validation of all environments {ContextDescription}", contextDescription);
            return HandleValidationError(ex);
        }
    }

    /// <summary>
    /// Finds an environment by ID in the configuration.
    /// </summary>
    private static Models.Environment? FindEnvironment(SGuardConfig config, string environmentId)
    {
        // Optimized: foreach loop with early return instead of LINQ FirstOrDefault
        foreach (var environment in config.Environments)
        {
            if (string.Equals(environment.Id, environmentId, StringComparison.OrdinalIgnoreCase))
            {
                return environment;
            }
        }
        return null;
    }

    /// <summary>
    /// Creates an error result when environment is not found.
    /// </summary>
    private RuleEngineResult CreateEnvironmentNotFoundError(SGuardConfig config, string environmentId)
    {
        var availableEnvironments = string.Join(", ", config.Environments.Select(e => e.Id));
        _logger.LogWarning("Environment {EnvironmentId} not found in configuration. Available environments: {AvailableEnvironments}", 
            environmentId, availableEnvironments);
        return RuleEngineResult.CreateError($"Environment '{environmentId}' not found in configuration. Available environments: {availableEnvironments}");
    }

    /// <summary>
    /// Validates a single environment from a file-based configuration.
    /// </summary>
    private FileValidationResult ValidateSingleEnvironment(
        Models.Environment environment, 
        SGuardConfig config,
        string environmentId,
        string configPath)
    {
        var applicableRules = GetApplicableRules(config, environmentId);
        _logger.LogDebug("Found {RuleCount} applicable rules for environment {EnvironmentId}", 
            applicableRules.Count, environmentId);

        _logger.LogDebug("Validating environment {EnvironmentId} with path {EnvironmentPath}", 
            environment.Id, environment.Path);

        // Resolve a relative path to an absolute path based on the config file location
        var appSettingsPath = _pathResolver.ResolvePath(environment.Path, configPath);
        _logger.LogDebug("Resolved app settings path: {AppSettingsPath}", appSettingsPath);

        var appSettings = _configLoader.LoadAppSettings(appSettingsPath);
        _logger.LogDebug("Loaded {SettingCount} app settings from {AppSettingsPath}", 
            appSettings.Count, appSettingsPath);

        var validationResult = _fileValidator.ValidateFile(appSettingsPath, applicableRules, appSettings);
        return validationResult;
    }

    /// <summary>
    /// Validates a single environment from JSON configuration.
    /// </summary>
    private FileValidationResult ValidateSingleEnvironmentFromJson(
        Models.Environment environment,
        SGuardConfig config,
        string environmentId)
    {
        var applicableRules = GetApplicableRules(config, environmentId);
        _logger.LogDebug("Found {RuleCount} applicable rules for environment {EnvironmentId}", 
            applicableRules.Count, environmentId);

        // For JSON-only validation, create empty appSettings since no file exists
        var appSettings = new Dictionary<string, object>();
        var validationResult = _fileValidator.ValidateFile(environment.Id, applicableRules, appSettings);
        return validationResult;
    }

    /// <summary>
    /// Validates all environments from a file-based configuration.
    /// </summary>
    private List<FileValidationResult> ValidateAllEnvironmentsInternal(SGuardConfig config, string configPath)
    {
        var results = new List<FileValidationResult>();

        foreach (var environment in config.Environments)
        {
            try
            {
                _logger.LogDebug("Processing environment {EnvironmentId}", environment.Id);

                if (string.IsNullOrWhiteSpace(environment.Path))
                {
                    _logger.LogWarning("Environment {EnvironmentId} has an invalid or empty path", environment.Id);
                    results.Add(new FileValidationResult(environment.Id,
                    [
                        ValidationResult.Failure($"Environment '{environment.Id}' has an invalid or empty path.", "system", environment.Id, null)
                    ]));
                    continue;
                }

                var validationResult = ValidateSingleEnvironment(environment, config, environment.Id, configPath);
                LogEnvironmentValidationResult(environment.Id, validationResult);
                results.Add(validationResult);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "File not found during validation of environment {EnvironmentId}", environment.Id);
                var errorValidation = ValidationResult.Failure(
                    $"File not found for environment '{environment.Id}': {ex.Message}", 
                    "system", 
                    environment.Path, 
                    null, 
                    ex);
                results.Add(new FileValidationResult(environment.Id, [errorValidation]));
            }
            catch (ConfigurationException ex)
            {
                _logger.LogError(ex, "Configuration error during validation of environment {EnvironmentId}", environment.Id);
                var errorValidation = ValidationResult.Failure(
                    $"Configuration error for environment '{environment.Id}': {ex.Message}", 
                    "system", 
                    environment.Path, 
                    null, 
                    ex);
                results.Add(new FileValidationResult(environment.Id, [errorValidation]));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during validation of environment {EnvironmentId}", environment.Id);
                // Create an error result for this environment but continue with others
                var errorValidation = ValidationResult.Failure(
                    $"Failed to load or validate environment '{environment.Id}': {ex.Message}", 
                    "system", 
                    environment.Path, 
                    null, 
                    ex);
                
                results.Add(new FileValidationResult(environment.Path, [errorValidation]));
            }
        }

        return results;
    }

    /// <summary>
    /// Validates all environments from JSON configuration.
    /// </summary>
    private List<FileValidationResult> ValidateAllEnvironmentsFromJsonInternal(SGuardConfig config)
    {
        var results = new List<FileValidationResult>();

        foreach (var environment in config.Environments)
        {
            _logger.LogDebug("Processing environment {EnvironmentId} from JSON", environment.Id);

            var validationResult = ValidateSingleEnvironmentFromJson(environment, config, environment.Id);
            LogEnvironmentValidationResult(environment.Id, validationResult);
            results.Add(validationResult);
        }

        return results;
    }

    /// <summary>
    /// Logs validation result for a single environment.
    /// </summary>
    private void LogValidationResult(string environmentId, FileValidationResult validationResult)
    {
        var errorCount = validationResult.Results.Count(r => !r.IsValid);
        if (errorCount > 0)
        {
            _logger.LogWarning("Validation completed for environment {EnvironmentId} with {ErrorCount} errors", 
                environmentId, errorCount);
        }
        else
        {
            _logger.LogInformation("Validation completed successfully for environment {EnvironmentId}", environmentId);
        }
    }

    /// <summary>
    /// Logs validation result for an environment in a batch operation.
    /// </summary>
    private void LogEnvironmentValidationResult(string environmentId, FileValidationResult validationResult)
    {
        var errorCount = validationResult.Results.Count(r => !r.IsValid);
        if (errorCount > 0)
        {
            _logger.LogWarning("Environment {EnvironmentId} validation completed with {ErrorCount} errors", 
                environmentId, errorCount);
        }
        else
        {
            _logger.LogDebug("Environment {EnvironmentId} validation completed successfully", environmentId);
        }
    }

    private SGuardConfig ParseConfigJson(string configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
        {
            throw new ArgumentException("Configuration JSON cannot be null or empty.", nameof(configJson));
        }

        try
        {
            var config = JsonSerializer.Deserialize<SGuardConfig>(configJson, JsonOptions.Deserialization);
            
            if (config == null)
            {
                throw new ConfigurationException("Failed to deserialize configuration JSON. The JSON may be malformed or empty.");
            }

            if (config.Environments == null || config.Environments.Count == 0)
            {
                throw new ConfigurationException("Configuration JSON must contain at least one environment definition.");
            }

            // Validate configuration structure and integrity if a validator is available
            if (_configValidator != null)
            {
                var supportedValidators = _validatorFactory.GetSupportedValidators();
                var validationErrors = _configValidator.Validate(config, supportedValidators);
                
                if (validationErrors.Count > 0)
                {
                    var errorMessage = string.Join(System.Environment.NewLine, validationErrors);
                    throw new ConfigurationException(
                        $"Configuration JSON validation failed:{System.Environment.NewLine}{errorMessage}");
                }
            }

            return config;
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"Invalid JSON format in configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets applicable rules for a specific environment.
    /// </summary>
    private List<Rule> GetApplicableRules(SGuardConfig config, string environmentId)
    {
        // Optimized: foreach loop with a pre-allocated list instead of LINQ Where().ToList()
        var rules = new List<Rule>(config.Rules.Count);
        
        foreach (var rule in config.Rules)
        {
            if (rule.Environments.Count == 0)
            {
                continue;
            }
            
            foreach (var env in rule.Environments)
            {
                if (string.Equals(env, environmentId, StringComparison.OrdinalIgnoreCase))
                {
                    rules.Add(rule);
                    break; // Early exit after finding a match
                }
            }
        }
        
        _logger.LogDebug("Retrieved {RuleCount} applicable rules for environment {EnvironmentId} from {TotalRules} total rules", 
            rules.Count, environmentId, config.Rules.Count);
        
        return rules;
    }

    /// <summary>
    /// Handles validation errors and converts them to RuleEngineResult.
    /// </summary>
    private static RuleEngineResult HandleValidationError(Exception ex)
    {
        return ex switch
        {
            FileNotFoundException => RuleEngineResult.CreateError($"File not found: {ex.Message}", ex),
            ConfigurationException => RuleEngineResult.CreateError($"Configuration error: {ex.Message}", ex),
            ValidationException => RuleEngineResult.CreateError($"Validation error: {ex.Message}", ex),
            _ => RuleEngineResult.CreateError($"Validation failed: {ex.Message}", ex)
        };
    }
}
