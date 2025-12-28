using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Exceptions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Resources;
using SGuard.ConfigValidation.Results;
using SGuard.ConfigValidation.Security;
using SGuard.ConfigValidation.Services.Abstract;
using SGuard.ConfigValidation.Telemetry;
using SGuard.ConfigValidation.Validators;
using static SGuard.ConfigValidation.Common.Throw;

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
    private readonly SecurityOptions _securityOptions;

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
    public RuleEngine(IConfigLoader configLoader, IFileValidator fileValidator, IValidatorFactory validatorFactory, ILogger<RuleEngine> logger,
                      IOptions<SecurityOptions> securityOptions, IPathResolver? pathResolver = null, IConfigValidator? configValidator = null)
    {
        System.ArgumentNullException.ThrowIfNull(configLoader);
        System.ArgumentNullException.ThrowIfNull(fileValidator);
        System.ArgumentNullException.ThrowIfNull(validatorFactory);
        System.ArgumentNullException.ThrowIfNull(logger);
        System.ArgumentNullException.ThrowIfNull(securityOptions);

        _pathResolver = pathResolver ?? new PathResolver(securityOptions);

        _configLoader = configLoader;
        _fileValidator = fileValidator;
        _validatorFactory = validatorFactory;
        _configValidator = configValidator;
        _logger = logger;
        _securityOptions = securityOptions.Value;
    }

    /// <summary>
    /// Validates a specific environment from a configuration file.
    /// </summary>
    /// <param name="configPath">The path to the configuration file.</param>
    /// <param name="environmentId">The ID of the environment to validate.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="RuleEngineResult"/> containing the validation results for the specified environment.</returns>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when the configuration file or environment file does not exist.</exception>
    /// <exception cref="SGuard.ConfigValidation.Exceptions.ConfigurationException">Thrown when the configuration is invalid or the environment is not found.</exception>
    /// <example>
    /// <code>
    /// using Microsoft.Extensions.Logging;
    /// using Microsoft.Extensions.Logging.Abstractions;
    /// using Microsoft.Extensions.Options;
    /// using SGuard.ConfigValidation.Common;
    /// using SGuard.ConfigValidation.Services;
    /// using SGuard.ConfigValidation.Validators;
    /// 
    /// var securityOptions = Options.Create(new SecurityOptions());
    /// var logger = NullLogger&lt;RuleEngine&gt;.Instance;
    /// var validatorFactory = new ValidatorFactory(NullLogger&lt;ValidatorFactory&gt;.Instance);
    /// var configLoader = new ConfigLoader(NullLogger&lt;ConfigLoader&gt;.Instance, securityOptions);
    /// var fileValidator = new FileValidator(validatorFactory, NullLogger&lt;FileValidator&gt;.Instance);
    /// var ruleEngine = new RuleEngine(configLoader, fileValidator, validatorFactory, logger, securityOptions);
    /// 
    /// var result = ruleEngine.ValidateEnvironment("sguard.json", "prod");
    /// 
    /// if (result.IsSuccess &amp;&amp; !result.HasValidationErrors)
    /// {
    ///     Console.WriteLine("Validation passed!");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Validation failed: {result.ErrorMessage}");
    ///     foreach (var validationResult in result.ValidationResults)
    ///     {
    ///         foreach (var error in validationResult.Errors)
    ///         {
    ///             Console.WriteLine($"  - {error.Key}: {error.Message}");
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public async Task<RuleEngineResult> ValidateEnvironmentAsync(string configPath, string environmentId,
                                                                 CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await ValidateEnvironmentInternalAsync(async () => await _configLoader.LoadConfigAsync(configPath, cancellationToken),
                                                                environmentId,
                                                                async (config, env) =>
                                                                    await ValidateSingleEnvironmentAsync(
                                                                        env, config, environmentId, configPath, cancellationToken),
                                                                $"from config {configPath}", requirePath: true, cancellationToken);

            stopwatch.Stop();
            ValidationMetrics.RecordValidationDuration(stopwatch.ElapsedMilliseconds);
            ValidationMetrics.RecordEnvironmentValidationDuration(stopwatch.ElapsedMilliseconds);
            ValidationMetrics.RecordEnvironmentValidation();

            if (result.IsSuccess)
            {
                ValidationMetrics.RecordValidationSuccess();
            }
            else
            {
                ValidationMetrics.RecordValidationFailure();
            }

            return result;
        }
        catch
        {
            stopwatch.Stop();
            ValidationMetrics.RecordValidationDuration(stopwatch.ElapsedMilliseconds);
            ValidationMetrics.RecordValidationFailure();
            throw;
        }
    }

    /// <summary>
    /// Validates all environments from a configuration file.
    /// </summary>
    /// <param name="configPath">The path to the configuration file.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="RuleEngineResult"/> containing validation results for all environments.</returns>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when the configuration file does not exist.</exception>
    /// <exception cref="SGuard.ConfigValidation.Exceptions.ConfigurationException">Thrown when the configuration is invalid.</exception>
    /// <example>
    /// <code>
    /// using Microsoft.Extensions.Logging;
    /// using Microsoft.Extensions.Logging.Abstractions;
    /// using Microsoft.Extensions.Options;
    /// using SGuard.ConfigValidation.Common;
    /// using SGuard.ConfigValidation.Services;
    /// using SGuard.ConfigValidation.Validators;
    /// 
    /// var securityOptions = Options.Create(new SecurityOptions());
    /// var logger = NullLogger&lt;RuleEngine&gt;.Instance;
    /// var validatorFactory = new ValidatorFactory(NullLogger&lt;ValidatorFactory&gt;.Instance);
    /// var configLoader = new ConfigLoader(NullLogger&lt;ConfigLoader&gt;.Instance, securityOptions);
    /// var fileValidator = new FileValidator(validatorFactory, NullLogger&lt;FileValidator&gt;.Instance);
    /// var ruleEngine = new RuleEngine(configLoader, fileValidator, validatorFactory, logger, securityOptions);
    /// 
    /// var result = ruleEngine.ValidateAllEnvironments("sguard.json");
    /// 
    /// var successCount = result.ValidationResults.Count(r => r.IsValid);
    /// var failureCount = result.ValidationResults.Count - successCount;
    /// 
    /// Console.WriteLine($"Validated {result.ValidationResults.Count} environment(s)");
    /// Console.WriteLine($"  Passed: {successCount}");
    /// Console.WriteLine($"  Failed: {failureCount}");
    /// </code>
    /// </example>
    public async Task<RuleEngineResult> ValidateAllEnvironmentsAsync(string configPath, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await ValidateAllEnvironmentsCommonAsync(async () => await _configLoader.LoadConfigAsync(configPath, cancellationToken),
                                                                  async config =>
                                                                      await ValidateAllEnvironmentsInternalAsync(
                                                                          config, configPath, cancellationToken), $"from config {configPath}",
                                                                  cancellationToken);

            stopwatch.Stop();
            ValidationMetrics.RecordValidationDuration(stopwatch.ElapsedMilliseconds);

            if (result.IsSuccess)
            {
                ValidationMetrics.RecordValidationSuccess();
            }
            else
            {
                ValidationMetrics.RecordValidationFailure();
            }

            return result;
        }
        catch
        {
            stopwatch.Stop();
            ValidationMetrics.RecordValidationDuration(stopwatch.ElapsedMilliseconds);
            ValidationMetrics.RecordValidationFailure();
            throw;
        }
    }

    /// <summary>
    /// Validates a specific environment from a JSON configuration string.
    /// </summary>
    /// <param name="configJson">The JSON configuration string.</param>
    /// <param name="environmentId">The ID of the environment to validate.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="RuleEngineResult"/> containing the validation results for the specified environment.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="configJson"/> is null or empty, or when <paramref name="environmentId"/> is null or empty.</exception>
    /// <exception cref="SGuard.ConfigValidation.Exceptions.ConfigurationException">Thrown when the JSON configuration is invalid or the environment is not found.</exception>
    public async Task<RuleEngineResult> ValidateEnvironmentFromJsonAsync(string configJson, string environmentId,
                                                                         CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await ValidateEnvironmentInternalAsync(async () => await Task.FromResult(ParseConfigJson(configJson, cancellationToken)), environmentId,
                                                      async (config, env) =>
                                                          await Task.FromResult(ValidateSingleEnvironmentFromJson(env, config, environmentId, cancellationToken)),
                                                      "from JSON", requirePath: false, cancellationToken);
    }

    /// <summary>
    /// Validates all environments from a JSON configuration string.
    /// </summary>
    /// <param name="configJson">The JSON configuration string.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="RuleEngineResult"/> containing validation results for all environments.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="configJson"/> is null or empty.</exception>
    /// <exception cref="SGuard.ConfigValidation.Exceptions.ConfigurationException">Thrown when the JSON configuration is invalid.</exception>
    public async Task<RuleEngineResult> ValidateAllEnvironmentsFromJsonAsync(string configJson, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await ValidateAllEnvironmentsCommonAsync(async () => await Task.FromResult(ParseConfigJson(configJson, cancellationToken)),
                                                        async config => await ValidateAllEnvironmentsFromJsonInternalAsync(config, cancellationToken),
                                                        "from JSON", cancellationToken);
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
    private async Task<RuleEngineResult> ValidateEnvironmentInternalAsync(Func<Task<SGuardConfig>> loadConfig, string environmentId,
                                                                          Func<SGuardConfig, Models.Environment, Task<FileValidationResult>>
                                                                              validateEnvironment, string contextDescription,
                                                                          bool requirePath = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(environmentId))
        {
            _logger.LogWarning("Validation failed: Environment ID is null or empty. Context: {ContextDescription}", contextDescription);
            var message = SR.ResourceManager.GetString(nameof(SR.RuleEngine_ValidationFailed_EnvironmentIdRequired));

            return RuleEngineResult.CreateError(
                string.Format(
                    message ??
                    "Validation failed: Environment ID is required but was null or empty. Context: {0}. Please provide a valid environment ID to validate.",
                    contextDescription));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var config = await loadConfig();

            var environment = FindEnvironment(config, environmentId);

            if (environment == null)
            {
                return CreateEnvironmentNotFoundError(config, environmentId, contextDescription);
            }

            // For file-based validation, check path validity
            if (requirePath && string.IsNullOrWhiteSpace(environment.Path))
            {
                _logger.LogWarning("Environment {EnvironmentId} has an invalid or empty path. Context: {ContextDescription}", environmentId,
                                   contextDescription);


                return RuleEngineResult.CreateError(
                    string.Format(
                        SR.ResourceManager.GetString(nameof(SR.RuleEngine_ValidationFailed_EnvironmentInvalidPath)) ??
                        "Validation failed: Environment '{0}' has an invalid or empty path. Context: {1}. Environment name: '{2}'. Please ensure the environment definition includes a valid 'path' property pointing to the app settings file.",
                        environmentId, contextDescription, environment.Name));
            }

            var validationResult = await validateEnvironment(config, environment);
            LogValidationResult(environmentId, validationResult);
            return RuleEngineResult.CreateSuccess(validationResult);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "File not found during validation of environment {EnvironmentId} {ContextDescription}. File path: {FilePath}",
                             environmentId, contextDescription, ex.FileName ?? "unknown");
            var message = SR.ResourceManager.GetString(nameof(SR.RuleEngine_ValidationFailed_FileNotFound));

            return RuleEngineResult.CreateError(
                string.Format(
                    message ??
                    "Validation failed: Required file not found. Environment ID: '{0}'. File path: '{1}'. Context: {2}. Error: {3}. Please ensure the configuration file and referenced app settings files exist and are accessible.",
                    environmentId, ex.FileName ?? "unknown", contextDescription, ex.Message), ex);
        }
        catch (ConfigurationException ex)
        {
            _logger.LogError(ex, "Configuration error during validation of environment {EnvironmentId} {ContextDescription}. Error: {ErrorMessage}",
                             environmentId, contextDescription, ex.Message);

            var message = SR.ResourceManager.GetString(nameof(SR.RuleEngine_ValidationFailed_ConfigurationError));

            return RuleEngineResult.CreateError(
                string.Format(
                    message ??
                    "Validation failed: Configuration error detected. Environment ID: '{0}'. Context: {1}. Error: {2}. Please review the configuration file structure and fix any validation errors.",
                    environmentId, contextDescription, ex.Message), ex);
        }
        catch (Exception ex)
        {
            // Re-throw critical exceptions immediately - they indicate severe system problems
            if (Throw.IsCriticalException(ex))
            {
                throw;
            }

            _logger.LogError(
                ex,
                "Unexpected error during validation of environment {EnvironmentId} {ContextDescription}. Exception type: {ExceptionType}, Message: {ErrorMessage}",
                environmentId, contextDescription, ex.GetType().Name, ex.Message);
            
            return HandleValidationError(ex, environmentId, contextDescription);
        }
    }

    /// <summary>
    /// Common validation logic for all environments validation (file-based or JSON-based).
    /// </summary>
    private async Task<RuleEngineResult> ValidateAllEnvironmentsCommonAsync(Func<Task<SGuardConfig>> loadConfig,
                                                                            Func<SGuardConfig, Task<List<FileValidationResult>>>
                                                                                validateAllEnvironments, string contextDescription,
                                                                            CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var config = await loadConfig();

            if (config.Environments.Count == 0)
            {
                _logger.LogWarning("No environments found in configuration {ContextDescription}", contextDescription);
                var message = SR.ResourceManager.GetString(nameof(SR.RuleEngine_NoEnvironmentsFound));
                return RuleEngineResult.CreateError(message ?? "No environments found in configuration.");
            }

            _logger.LogInformation("Validating {EnvironmentCount} environments {ContextDescription}", config.Environments.Count, contextDescription);
            var results = await validateAllEnvironments(config);

            // Use ErrorCount property to avoid nested enumeration
            var totalErrors = results.Sum(r => r.ErrorCount);
            var successfulEnvironments = results.Count(r => r.ErrorCount == 0);

            _logger.LogInformation(
                "Validation completed for all environments {ContextDescription}. Successful: {SuccessfulCount}, Total errors: {TotalErrors}",
                contextDescription, successfulEnvironments, totalErrors);

            return RuleEngineResult.CreateSuccess(results);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "File not found during validation of all environments {ContextDescription}. File path: {FilePath}",
                             contextDescription, ex.FileName ?? "unknown");

            return RuleEngineResult.CreateError(
                string.Format(
                    SR.ResourceManager.GetString(nameof(SR.RuleEngine_ValidationFailed_FileNotFound_AllEnvironments)) ??
                    "Validation failed: Required file not found. Context: {0}. File path: '{1}'. Error: {2}. Please ensure the configuration file and referenced app settings files exist and are accessible.",
                    contextDescription, ex.FileName ?? "unknown", ex.Message), ex);
        }
        catch (ConfigurationException ex)
        {
            _logger.LogError(ex, "Configuration error during validation of all environments {ContextDescription}. Error: {ErrorMessage}",
                             contextDescription, ex.Message);

            return RuleEngineResult.CreateError(
                string.Format(
                    SR.ResourceManager.GetString(nameof(SR.RuleEngine_ValidationFailed_ConfigurationError_AllEnvironments)) ??
                    "Validation failed: Configuration error detected. Context: {0}. Error: {1}. Please review the configuration file structure and fix any validation errors.",
                    contextDescription, ex.Message), ex);
        }
        catch (Exception ex)
        {
            // Re-throw critical exceptions immediately - they indicate severe system problems
            if (Throw.IsCriticalException(ex))
            {
                throw;
            }

            _logger.LogError(
                ex,
                "Unexpected error during validation of all environments {ContextDescription}. Exception type: {ExceptionType}, Message: {ErrorMessage}",
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
        var contextInfo = !string.IsNullOrWhiteSpace(contextDescription) ? $" Context: {contextDescription}." : string.Empty;

        _logger.LogWarning("Environment {EnvironmentId} not found in configuration. Available environments: {AvailableEnvironments}.{ContextInfo}",
                           environmentId, availableEnvironments, contextInfo);
        var message = SR.ResourceManager.GetString(nameof(SR.RuleEngine_EnvironmentNotFound));

        return RuleEngineResult.CreateError(
            string.Format(
                message ??
                "Validation failed: Environment '{0}' not found in configuration.{1} Total environments in configuration: {2}. Available environment IDs: {3}. Please verify the environment ID is correct and matches one of the available environments in the configuration file.",
                environmentId, contextInfo, config.Environments.Count, availableEnvironments));
    }

    /// <summary>
    /// Validates a single environment from a file-based configuration.
    /// </summary>
    private async Task<FileValidationResult> ValidateSingleEnvironmentAsync(Models.Environment environment, SGuardConfig config, string environmentId,
                                                                            string configPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var applicableRules = GetApplicableRules(config, environmentId, cancellationToken);

        // Resolve a relative path to an absolute path based on the config file location
        var appSettingsPath = _pathResolver.ResolvePath(environment.Path, configPath);

        var appSettings = await _configLoader.LoadAppSettingsAsync(appSettingsPath, cancellationToken).ConfigureAwait(false);

        return _fileValidator.ValidateFile(appSettingsPath, applicableRules, appSettings);
    }

    /// <summary>
    /// Validates a single environment from JSON configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method performs validation against an empty dictionary since no appsettings file exists for JSON-based validation.
    /// This means that validators checking for required keys will always fail, as there are no actual configuration values to validate.
    /// </para>
    /// <para>
    /// This method is intended for validating the structure and rules of the sguard.json configuration itself,
    /// not for validating actual application settings. To validate actual appsettings values, use file-based validation methods
    /// such as <see cref="ValidateSingleEnvironmentAsync"/>.
    /// </para>
    /// </remarks>
    /// <param name="environment">The environment to validate.</param>
    /// <param name="config">The configuration containing rules.</param>
    /// <param name="environmentId">The ID of the environment to validate.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="FileValidationResult"/> containing validation results. Note that required validators will fail since no appsettings are provided.</returns>
    private FileValidationResult ValidateSingleEnvironmentFromJson(Models.Environment environment, SGuardConfig config, string environmentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var applicableRules = GetApplicableRules(config, environmentId, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // For JSON-only validation, create empty appSettings since no file exists
        // This means validators checking for required keys will always fail
        // This is intentional - JSON-based validation is for validating sguard.json structure, not appsettings values
        _logger.LogDebug(
            "Validating environment {EnvironmentId} from JSON configuration. Note: No appsettings file is loaded, so required validators will fail.",
            environmentId);

        cancellationToken.ThrowIfCancellationRequested();

        return _fileValidator.ValidateFile(environment.Id, applicableRules, new Dictionary<string, object>());
    }

    /// <summary>
    /// Validates all environments from a file-based configuration.
    /// </summary>
    private async Task<List<FileValidationResult>> ValidateAllEnvironmentsInternalAsync(SGuardConfig config, string configPath,
                                                                                        CancellationToken cancellationToken = default)
    {
        // Use ConcurrentBag for lock-free thread-safe collection
        var results = new ConcurrentBag<FileValidationResult>();
        // Track critical exceptions separately to throw them after all environments are processed
        var criticalExceptions = new ConcurrentBag<Exception>();
        var maxParallelism = _securityOptions.MaxParallelEnvironments;

        // Use Parallel.ForEachAsync for parallel execution with controlled concurrency
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(config.Environments, parallelOptions, async (environment, ct) =>
        {
            ct.ThrowIfCancellationRequested();

            var envStopwatch = Stopwatch.StartNew();
            FileValidationResult? validationResult;

            try
            {
                if (string.IsNullOrWhiteSpace(environment.Path))
                {
                    _logger.LogWarning("Environment {EnvironmentId} has an invalid or empty path", environment.Id);

                    validationResult = new FileValidationResult(environment.Id,
                    [
                        ValidationResult.Failure(
                            ValidationMessageFormatter.FormatEnvironmentError(environment.Id, "has an invalid or empty path", ""), "system",
                            environment.Id, null)
                    ]);
                }
                else
                {
                    validationResult = await ValidateSingleEnvironmentAsync(environment, config, environment.Id, configPath, ct);
                    LogEnvironmentValidationResult(environment.Id, validationResult);
                }

                envStopwatch.Stop();
                ValidationMetrics.RecordEnvironmentValidationDuration(envStopwatch.ElapsedMilliseconds);
                ValidationMetrics.RecordEnvironmentValidation();

                if (validationResult.ErrorCount == 0)
                {
                    ValidationMetrics.RecordValidationSuccess();
                }
                else
                {
                    ValidationMetrics.RecordValidationFailure();
                }
            }
            catch (FileNotFoundException ex)
            {
                envStopwatch.Stop();
                ValidationMetrics.RecordEnvironmentValidationDuration(envStopwatch.ElapsedMilliseconds);
                ValidationMetrics.RecordEnvironmentValidation();
                ValidationMetrics.RecordValidationFailure();

                _logger.LogError(ex, "File not found during validation of environment {EnvironmentId}", environment.Id);

                var errorValidation = ValidationResult.Failure(
                    ValidationMessageFormatter.FormatFileNotFoundError(environment.Id, environment.Path, ex), "system", environment.Path, null, ex);
                validationResult = new FileValidationResult(environment.Id, [errorValidation]);
            }
            catch (ConfigurationException ex)
            {
                envStopwatch.Stop();
                ValidationMetrics.RecordEnvironmentValidationDuration(envStopwatch.ElapsedMilliseconds);
                ValidationMetrics.RecordEnvironmentValidation();
                ValidationMetrics.RecordValidationFailure();

                _logger.LogError(ex, "Configuration error during validation of environment {EnvironmentId}", environment.Id);

                var errorValidation = ValidationResult.Failure(ValidationMessageFormatter.FormatConfigurationError(environment.Id, ex.Message, ex),
                                                               "system", environment.Path, null, ex);
                validationResult = new FileValidationResult(environment.Id, [errorValidation]);
            }
            catch (Exception ex)
            {
                envStopwatch.Stop();
                ValidationMetrics.RecordEnvironmentValidationDuration(envStopwatch.ElapsedMilliseconds);
                ValidationMetrics.RecordEnvironmentValidation();
                ValidationMetrics.RecordValidationFailure();

                // Handle critical exceptions: capture them but don't throw immediately
                // This allows other environments to be processed, preserving partial results
                if (Throw.IsCriticalException(ex))
                {
                    _logger.LogCritical(ex, "Critical exception during validation of environment {EnvironmentId}. This will be thrown after all environments are processed.", environment.Id);
                    criticalExceptions.Add(ex);
                }
                else
                {
                    _logger.LogError(ex, "Unexpected error during validation of environment {EnvironmentId}", environment.Id);
                }

                // Create an error result for this environment but continue with others
                var errorValidation = ValidationResult.Failure(
                    ValidationMessageFormatter.FormatFailedToLoadEnvironmentError(environment.Id, ex.Message, ex), "system", environment.Path, null,
                    ex);

                validationResult = new FileValidationResult(environment.Path, [errorValidation]);
            }

            // Thread-safe add to a results collection (ConcurrentBag is lock-free)
            results.Add(validationResult);
        });

        // After all environments are processed, throw critical exceptions if any occurred
        // This ensures partial results are preserved while still signaling critical failures
        if (criticalExceptions.Count > 0)
        {
            var exceptions = criticalExceptions.ToList();
            if (exceptions.Count == 1)
            {
                throw exceptions[0];
            }

            throw new AggregateException(
                "One or more critical exceptions occurred during parallel validation. Partial results may be available.",
                exceptions);
        }

        // Convert ConcurrentBag to List and sort by path for deterministic output
        // Sorting ensures consistent ordering across multiple runs, as ConcurrentBag does not guarantee order
        return results.OrderBy(r => r.Path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Validates all environments from JSON configuration.
    /// Uses parallel execution for better performance, similar to file-based validation.
    /// </summary>
    private async Task<List<FileValidationResult>> ValidateAllEnvironmentsFromJsonInternalAsync(SGuardConfig config, CancellationToken cancellationToken = default)
    {
        // Use ConcurrentBag for lock-free thread-safe collection
        var results = new ConcurrentBag<FileValidationResult>();
        // Track critical exceptions separately to throw them after all environments are processed
        var criticalExceptions = new ConcurrentBag<Exception>();
        var maxParallelism = _securityOptions.MaxParallelEnvironments;

        // Use Parallel.ForEachAsync for parallel execution with controlled concurrency
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(config.Environments, parallelOptions, async (environment, ct) =>
        {
            ct.ThrowIfCancellationRequested();

            var envStopwatch = Stopwatch.StartNew();
            FileValidationResult? validationResult;

            try
            {
                // ValidateSingleEnvironmentFromJson is CPU-bound, run it on thread pool
                // Pass cancellation token to allow cancellation during validation
                validationResult = await Task.Run(() => ValidateSingleEnvironmentFromJson(environment, config, environment.Id, ct), ct);
                LogEnvironmentValidationResult(environment.Id, validationResult);

                envStopwatch.Stop();
                ValidationMetrics.RecordEnvironmentValidationDuration(envStopwatch.ElapsedMilliseconds);
                ValidationMetrics.RecordEnvironmentValidation();

                if (validationResult.ErrorCount == 0)
                {
                    ValidationMetrics.RecordValidationSuccess();
                }
                else
                {
                    ValidationMetrics.RecordValidationFailure();
                }
            }
            catch (Exception ex)
            {
                envStopwatch.Stop();
                ValidationMetrics.RecordEnvironmentValidationDuration(envStopwatch.ElapsedMilliseconds);
                ValidationMetrics.RecordEnvironmentValidation();
                ValidationMetrics.RecordValidationFailure();

                // Handle critical exceptions: capture them but don't throw immediately
                // This allows other environments to be processed, preserving partial results
                if (Throw.IsCriticalException(ex))
                {
                    _logger.LogCritical(ex, "Critical exception during validation of environment {EnvironmentId}. This will be thrown after all environments are processed.", environment.Id);
                    criticalExceptions.Add(ex);
                }
                else
                {
                    _logger.LogError(ex, "Unexpected error during validation of environment {EnvironmentId}", environment.Id);
                }

                // Create an error result for this environment but continue with others
                var errorValidation = ValidationResult.Failure(
                    ValidationMessageFormatter.FormatFailedToLoadEnvironmentError(environment.Id, ex.Message, ex), "system", environment.Id, null,
                    ex);

                validationResult = new FileValidationResult(environment.Id, [errorValidation]);
            }

            // Thread-safe add to a results collection (ConcurrentBag is lock-free)
            results.Add(validationResult);
        });

        // After all environments are processed, throw critical exceptions if any occurred
        // This ensures partial results are preserved while still signaling critical failures
        if (criticalExceptions.Count > 0)
        {
            var exceptions = criticalExceptions.ToList();
            if (exceptions.Count == 1)
            {
                throw exceptions[0];
            }

            throw new AggregateException(
                "One or more critical exceptions occurred during parallel validation. Partial results may be available.",
                exceptions);
        }

        // Convert ConcurrentBag to List and sort by path for deterministic output
        // Sorting ensures consistent ordering across multiple runs, as ConcurrentBag does not guarantee order
        return results.OrderBy(r => r.Path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Logs validation result for a single environment.
    /// </summary>
    private void LogValidationResult(string environmentId, FileValidationResult validationResult)
    {
        var errorCount = validationResult.Results.Count(r => !r.IsValid);

        if (errorCount > 0)
        {
            _logger.LogWarning("Validation completed for environment {EnvironmentId} with {ErrorCount} errors", environmentId, errorCount);
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
            _logger.LogWarning("Environment {EnvironmentId} validation completed with {ErrorCount} errors", environmentId, errorCount);
        }
    }

    /// <summary>
    /// Parses a JSON configuration string into a SGuardConfig object.
    /// </summary>
    /// <param name="configJson">The JSON configuration string to parse.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The parsed SGuardConfig object.</returns>
    /// <exception cref="System.ArgumentException">Thrown when configJson is null or empty.</exception>
    /// <exception cref="SGuard.ConfigValidation.Exceptions.ConfigurationException">Thrown when the JSON configuration is invalid.</exception>
    private SGuardConfig ParseConfigJson(string configJson, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(configJson))
        {
            throw ArgumentException(nameof(SR.ArgumentException_ConfigJsonNullOrEmpty), nameof(configJson));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var config = JsonSerializer.Deserialize<SGuardConfig>(configJson, JsonOptions.Deserialization);

            cancellationToken.ThrowIfCancellationRequested();

            if (config == null)
            {
                throw ConfigurationException(nameof(SR.ConfigurationException_JsonDeserializationFailed));
            }

            if (config.Environments.Count == 0)
            {
                throw ConfigurationException(nameof(SR.ConfigurationException_JsonNoEnvironments));
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (_configValidator == null)
            {
                return config;
            }

            // Validate configuration structure and integrity if a validator is available
            var supportedValidators = _validatorFactory.GetSupportedValidators();
            var validationErrors = _configValidator.Validate(config, supportedValidators);

            cancellationToken.ThrowIfCancellationRequested();

            if (validationErrors.Count <= 0)
            {
                return config;
            }

            var errorMessage = string.Join(System.Environment.NewLine, validationErrors);
            throw ConfigurationException(nameof(SR.ConfigurationException_JsonValidationFailed), System.Environment.NewLine, errorMessage);
        }
        catch (JsonException ex)
        {
            throw ConfigurationException(nameof(SR.ConfigurationException_InvalidJsonFormatSimple), ex, ex.Message);
        }
    }

    /// <summary>
    /// Gets applicable rules for a specific environment.
    /// </summary>
    /// <param name="config">The configuration containing rules.</param>
    /// <param name="environmentId">The ID of the environment to get rules for.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A list of rules applicable to the specified environment.</returns>
    private static List<Rule> GetApplicableRules(SGuardConfig config, string environmentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Optimized: Use HashSet for O(1) lookup instead of nested loop O(n*m)
        // Pre-allocate list with estimated capacity
        var rules = new List<Rule>(config.Rules.Count);

        foreach (var rule in config.Rules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (rule.Environments.Count == 0)
            {
                continue;
            }

            // Convert rule's environment list to HashSet for O(1) lookup
            // Use case-insensitive comparer to match the original string.Equals behavior
            var environmentSet = new HashSet<string>(rule.Environments, StringComparer.OrdinalIgnoreCase);

            if (environmentSet.Contains(environmentId))
            {
                rules.Add(rule);
            }
        }

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
                ValidationMessageFormatter.FormatValidationFailureWithContext("Validation failed: Required file not found.", environmentId,
                                                                              contextDescription,
                                                                              $"File path: '{fileEx.FileName ?? "unknown"}'. Error: {ex.Message}.",
                                                                              "Please ensure the configuration file and referenced app settings files exist and are accessible."),
                ex),
            Exceptions.ConfigurationException => RuleEngineResult.CreateError(
                ValidationMessageFormatter.FormatValidationFailureWithContext("Validation failed: Configuration error detected.", environmentId,
                                                                              contextDescription, $"Error: {ex.Message}.",
                                                                              "Please review the configuration file structure and fix any validation errors."),
                ex),
            ValidationException => RuleEngineResult.CreateError(
                ValidationMessageFormatter.FormatValidationFailureWithContext("Validation failed: Validation error occurred.", environmentId,
                                                                              contextDescription, $"Error: {ex.Message}.",
                                                                              "Please review the validation rules and fix any rule violations."), ex),
            _ => RuleEngineResult.CreateError(
                ValidationMessageFormatter.FormatValidationFailureWithContext("Validation failed: Unexpected error occurred.", environmentId,
                                                                              contextDescription,
                                                                              $"Exception type: {ex.GetType().Name}. Error: {ex.Message}.",
                                                                              "This is an unexpected error. Please check the logs for more details and contact support if the issue persists."),
                ex)
        };
    }
}