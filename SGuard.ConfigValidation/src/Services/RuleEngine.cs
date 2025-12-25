using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Exceptions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Resources;
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
    /// <param name="logger">Logger instance.</param>
    /// <param name="securityOptions">Security options configured via IOptions pattern.</param>
    /// <param name="pathResolver">Optional path resolver service. If not provided, a new instance will be created.</param>
    /// <param name="configValidator">Optional configuration validator for JSON-based validation.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public RuleEngine(
        IConfigLoader configLoader,
        IFileValidator fileValidator,
        IValidatorFactory validatorFactory,
        ILogger<RuleEngine> logger,
        IOptions<SecurityOptions> securityOptions,
        IPathResolver? pathResolver = null,
        IConfigValidator? configValidator = null)
    {
        ArgumentNullException.ThrowIfNull(configLoader);
        ArgumentNullException.ThrowIfNull(fileValidator);
        ArgumentNullException.ThrowIfNull(validatorFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(securityOptions);
        _configLoader = configLoader;
        _fileValidator = fileValidator;
        _validatorFactory = validatorFactory;
        _configValidator = configValidator;
        _pathResolver = pathResolver ?? new PathResolver(securityOptions);
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
            _logger.LogWarning("Validation failed: Environment ID is null or empty. Context: {ContextDescription}", contextDescription);
            return RuleEngineResult.CreateError(
                $"Validation failed: Environment ID is required but was null or empty. " +
                $"Context: {contextDescription}. " +
                "Please provide a valid environment ID to validate.");
        }

        try
        {
            var config = loadConfig();
            _logger.LogDebug("Configuration loaded successfully. Found {EnvironmentCount} environments and {RuleCount} rules", 
                config.Environments.Count, config.Rules.Count);

            var environment = FindEnvironment(config, environmentId);
            if (environment == null)
            {
                return CreateEnvironmentNotFoundError(config, environmentId, contextDescription);
            }

            // For file-based validation, check path validity
            if (requirePath && string.IsNullOrWhiteSpace(environment.Path))
            {
                _logger.LogWarning("Environment {EnvironmentId} has an invalid or empty path. Context: {ContextDescription}", environmentId, contextDescription);
                return RuleEngineResult.CreateError(
                    $"Validation failed: Environment '{environmentId}' has an invalid or empty path. " +
                    $"Context: {contextDescription}. " +
                    $"Environment name: '{environment.Name}'. " +
                    "Please ensure the environment definition includes a valid 'path' property pointing to the app settings file.");
            }

            var validationResult = validateEnvironment(config, environment);
            LogValidationResult(environmentId, validationResult);
            return RuleEngineResult.CreateSuccess(validationResult);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "File not found during validation of environment {EnvironmentId} {ContextDescription}. File path: {FilePath}", 
                environmentId, contextDescription, ex.FileName ?? "unknown");
            return RuleEngineResult.CreateError(
                $"Validation failed: Required file not found. " +
                $"Environment ID: '{environmentId}'. " +
                $"File path: '{ex.FileName ?? "unknown"}'. " +
                $"Context: {contextDescription}. " +
                $"Error: {ex.Message}. " +
                "Please ensure the configuration file and referenced app settings files exist and are accessible.", 
                ex);
        }
        catch (ConfigurationException ex)
        {
            _logger.LogError(ex, "Configuration error during validation of environment {EnvironmentId} {ContextDescription}. Error: {ErrorMessage}", 
                environmentId, contextDescription, ex.Message);
            return RuleEngineResult.CreateError(
                $"Validation failed: Configuration error detected. " +
                $"Environment ID: '{environmentId}'. " +
                $"Context: {contextDescription}. " +
                $"Error: {ex.Message}. " +
                "Please review the configuration file structure and fix any validation errors.", 
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during validation of environment {EnvironmentId} {ContextDescription}. Exception type: {ExceptionType}, Message: {ErrorMessage}", 
                environmentId, contextDescription, ex.GetType().Name, ex.Message);
            return HandleValidationError(ex, environmentId, contextDescription);
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
            _logger.LogError(ex, "File not found during validation of all environments {ContextDescription}. File path: {FilePath}", 
                contextDescription, ex.FileName ?? "unknown");
            return RuleEngineResult.CreateError(
                $"Validation failed: Required file not found. " +
                $"Context: {contextDescription}. " +
                $"File path: '{ex.FileName ?? "unknown"}'. " +
                $"Error: {ex.Message}. " +
                "Please ensure the configuration file and referenced app settings files exist and are accessible.", 
                ex);
        }
        catch (ConfigurationException ex)
        {
            _logger.LogError(ex, "Configuration error during validation of all environments {ContextDescription}. Error: {ErrorMessage}", 
                contextDescription, ex.Message);
            return RuleEngineResult.CreateError(
                $"Validation failed: Configuration error detected. " +
                $"Context: {contextDescription}. " +
                $"Error: {ex.Message}. " +
                "Please review the configuration file structure and fix any validation errors.", 
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during validation of all environments {ContextDescription}. Exception type: {ExceptionType}, Message: {ErrorMessage}", 
                contextDescription, ex.GetType().Name, ex.Message);
            return HandleValidationError(ex, null, contextDescription);
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
    private RuleEngineResult CreateEnvironmentNotFoundError(SGuardConfig config, string environmentId, string? contextDescription = null)
    {
        var availableEnvironments = string.Join(", ", config.Environments.Select(e => e.Id));
        var contextInfo = !string.IsNullOrWhiteSpace(contextDescription) 
            ? $" Context: {contextDescription}." 
            : string.Empty;
        _logger.LogWarning("Environment {EnvironmentId} not found in configuration. Available environments: {AvailableEnvironments}.{ContextInfo}", 
            environmentId, availableEnvironments, contextInfo);
        return RuleEngineResult.CreateError(
            $"Validation failed: Environment '{environmentId}' not found in configuration.{contextInfo} " +
            $"Total environments in configuration: {config.Environments.Count}. " +
            $"Available environment IDs: {availableEnvironments}. " +
            "Please verify the environment ID is correct and matches one of the available environments in the configuration file.");
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
                        ValidationResult.Failure(ValidationMessageFormatter.FormatEnvironmentError(environment.Id, "has an invalid or empty path", ""), "system", environment.Id, null)
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
                    ValidationMessageFormatter.FormatFileNotFoundError(environment.Id, environment.Path, ex), 
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
                    ValidationMessageFormatter.FormatConfigurationError(environment.Id, ex.Message, ex), 
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
                    ValidationMessageFormatter.FormatFailedToLoadEnvironmentError(environment.Id, ex.Message, ex), 
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
            throw This.ArgumentException(nameof(SR.ArgumentException_ConfigJsonNullOrEmpty), nameof(configJson));
        }

        try
        {
            var config = JsonSerializer.Deserialize<SGuardConfig>(configJson, JsonOptions.Deserialization);
            
            if (config == null)
            {
                This.ConfigurationException(nameof(SR.ConfigurationException_JsonDeserializationFailed));
            }

            if (config.Environments == null || config.Environments.Count == 0)
            {
                This.ConfigurationException(nameof(SR.ConfigurationException_JsonNoEnvironments));
            }

            // Validate configuration structure and integrity if a validator is available
            if (_configValidator != null)
            {
                var supportedValidators = _validatorFactory.GetSupportedValidators();
                var validationErrors = _configValidator.Validate(config, supportedValidators);
                
                if (validationErrors.Count > 0)
                {
                    var errorMessage = string.Join(System.Environment.NewLine, validationErrors);
                    This.ConfigurationException(nameof(SR.ConfigurationException_JsonValidationFailed), System.Environment.NewLine, errorMessage);
                }
            }

            return config;
        }
        catch (JsonException ex)
        {
            throw This.ConfigurationException(nameof(SR.ConfigurationException_InvalidJsonFormatSimple), ex, ex.Message);
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
    private static RuleEngineResult HandleValidationError(Exception ex, string? environmentId = null, string? contextDescription = null)
    {
        return ex switch
        {
            FileNotFoundException fileEx => RuleEngineResult.CreateError(
                ValidationMessageFormatter.FormatValidationFailureWithContext(
                    "Validation failed: Required file not found.",
                    environmentId,
                    contextDescription,
                    $"File path: '{fileEx.FileName ?? "unknown"}'. Error: {ex.Message}.",
                    "Please ensure the configuration file and referenced app settings files exist and are accessible."),
                ex),
            ConfigurationException => RuleEngineResult.CreateError(
                ValidationMessageFormatter.FormatValidationFailureWithContext(
                    "Validation failed: Configuration error detected.",
                    environmentId,
                    contextDescription,
                    $"Error: {ex.Message}.",
                    "Please review the configuration file structure and fix any validation errors."),
                ex),
            ValidationException => RuleEngineResult.CreateError(
                ValidationMessageFormatter.FormatValidationFailureWithContext(
                    "Validation failed: Validation error occurred.",
                    environmentId,
                    contextDescription,
                    $"Error: {ex.Message}.",
                    "Please review the validation rules and fix any rule violations."),
                ex),
            _ => RuleEngineResult.CreateError(
                ValidationMessageFormatter.FormatValidationFailureWithContext(
                    "Validation failed: Unexpected error occurred.",
                    environmentId,
                    contextDescription,
                    $"Exception type: {ex.GetType().Name}. Error: {ex.Message}.",
                    "This is an unexpected error. Please check the logs for more details and contact support if the issue persists."),
                ex)
        };
    }
}
