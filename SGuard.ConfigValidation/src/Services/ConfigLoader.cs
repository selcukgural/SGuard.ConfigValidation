using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Exceptions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Resources;
using SGuard.ConfigValidation.Services.Abstract;
using SGuard.ConfigValidation.Telemetry;
using SGuard.ConfigValidation.Validators;
using static SGuard.ConfigValidation.Common.Throw;
using JsonElement = System.Text.Json.JsonElement;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Service for loading configuration files and app settings.
/// </summary>
public sealed class ConfigLoader : IConfigLoader
{
    // Use shared static options for better performance (immutable, thread-safe)
    private readonly ISchemaValidator? _schemaValidator;
    private readonly IConfigValidator? _configValidator;
    private readonly IYamlLoader? _yamlLoader;
    private readonly SecurityOptions _securityOptions;
    private readonly ILogger<ConfigLoader> _logger;

    /// <summary>
    /// Initializes a new instance of the ConfigLoader class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="securityOptions">Security options configured via IOptions pattern.</param>
    /// <param name="schemaValidator">Optional schema validator. If provided, configuration files will be validated against the schema.</param>
    /// <param name="configValidator">Optional configuration validator. If provided, configuration structure will be validated.</param>
    /// <param name="yamlLoader">Optional YAML loader. If provided, YAML files will be supported.</param>
    public ConfigLoader(ILogger<ConfigLoader> logger, IOptions<SecurityOptions> securityOptions, ISchemaValidator? schemaValidator = null, IConfigValidator? configValidator = null, IYamlLoader? yamlLoader = null)
    {
        System.ArgumentNullException.ThrowIfNull(logger);
        System.ArgumentNullException.ThrowIfNull(securityOptions);
        
        _logger = logger;
        _securityOptions = securityOptions.Value;
        _schemaValidator = schemaValidator;
        _configValidator = configValidator;
        _yamlLoader = yamlLoader;
    }

        /// <summary>
        /// Loads the SGuard configuration from the specified file path asynchronously.
        /// Supports both JSON (default) and YAML files (if the YAML loader is provided).
        /// JSON is the default format for configuration files like `sguard.json`.
        /// Validates the configuration against a schema if a schema validator is provided.
        /// Validates the configuration structure if a config validator is provided.
        /// </summary>
        /// <param name="configPath">The path to the configuration file (JSON or YAML). JSON is the default format.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the loaded and validated <see cref="SGuardConfig"/> instance.</returns>
        /// <exception cref="System.ArgumentException">Thrown when <paramref name="configPath"/> is null or empty.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when the configuration file does not exist.</exception>
        /// <exception cref="SGuard.ConfigValidation.Exceptions.ConfigurationException">Thrown when the configuration file is invalid, empty, malformed, fails schema validation, or fails structure validation.</exception>
        /// <exception cref="System.Text.Json.JsonException">Thrown when the JSON content is invalid.</exception>
        /// <exception cref="System.IO.IOException">Thrown when an I/O error occurs while reading the file.</exception>
        /// <example>
        /// <code>
        /// using Microsoft.Extensions.Logging;
        /// using Microsoft.Extensions.Logging.Abstractions;
        /// using Microsoft.Extensions.Options;
        /// using SGuard.ConfigValidation.Common;
        /// using SGuard.ConfigValidation.Services;
        /// 
        /// var securityOptions = Options.Create(new SecurityOptions());
        /// var logger = NullLogger&lt;ConfigLoader&gt;.Instance;
        /// var configLoader = new ConfigLoader(logger, securityOptions);
        /// 
        /// try
        /// {
        ///     var config = await configLoader.LoadConfigAsync("sguard.json");
        ///     Console.WriteLine($"Loaded {config.Environments.Count} environment(s)");
        ///     Console.WriteLine($"Loaded {config.Rules.Count} rule(s)");
        /// }
        /// catch (FileNotFoundException ex)
        /// {
        ///     Console.WriteLine($"Configuration file not found: {ex.FileName}");
        /// }
        /// catch (ConfigurationException ex)
        /// {
        ///     Console.WriteLine($"Configuration error: {ex.Message}");
        /// }
        /// </code>
        /// </example>
        public async Task<SGuardConfig> LoadConfigAsync(string configPath, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrWhiteSpace(configPath))
                {
                    _logger.LogError("Configuration file path is null or empty");
                    throw ArgumentException(nameof(SR.ArgumentException_ConfigPathNullOrEmpty), nameof(configPath));
                }

                FileSecurity.EnsureFileExists(configPath, nameof(SR.ConfigurationException_FileNotFound), _logger);

                // Check if a file is YAML
                if (IsYamlFile(configPath) && _yamlLoader != null)
                {
                    var result = await _yamlLoader.LoadConfigAsync(configPath, cancellationToken).ConfigureAwait(false);
                    stopwatch.Stop();
                    ValidationMetrics.RecordFileLoadingDuration(stopwatch.ElapsedMilliseconds);
                    return result;
                }
                // Check file size before reading to prevent DoS attacks
                FileSecurity.ValidateFileSize(configPath, _securityOptions.MaxFileSizeBytes, _logger, nameof(SR.ConfigurationException_FileSizeExceedsLimit));

                var json = await SafeFileSystem.SafeReadAllTextAsync(configPath, cancellationToken).ConfigureAwait(false);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogError("Configuration file {ConfigPath} is empty (file exists but contains no content)", configPath);
                    throw ConfigurationException(nameof(SR.ConfigurationException_FileEmpty), configPath, Path.GetFullPath(configPath));
                }

                // Validate against schema if validator is provided
                if (_schemaValidator != null)
                {
                    var schemaPath = GetSchemaPath(configPath);
                    
                    if (SafeFileSystem.FileExists(schemaPath))
                    {
                        _logger.LogInformation("Validating configuration against schema {SchemaPath}", schemaPath);
                        var schemaValidationResult = await _schemaValidator.ValidateAgainstFileAsync(json, schemaPath, cancellationToken).ConfigureAwait(false);
                        
                        if (!schemaValidationResult.IsValid)
                        {
                            _logger.LogError("Schema validation failed for {ConfigPath} against schema {SchemaPath}. Errors: {Errors}", 
                                configPath, schemaPath, schemaValidationResult.ErrorMessage);
                            throw ConfigurationException(nameof(SR.ConfigurationException_SchemaValidationFailed), 
                                configPath, Path.GetFullPath(configPath), schemaPath, Path.GetFullPath(schemaPath), System.Environment.NewLine, schemaValidationResult.ErrorMessage);
                        }
                    }
                }

                var config = JsonSerializer.Deserialize<SGuardConfig>(json, JsonOptions.Deserialization);
                
                if (config == null)
                {
                    _logger.LogError("Failed to deserialize configuration from {ConfigPath}. JSON deserialization returned null", configPath);
                    throw ConfigurationException(nameof(SR.ConfigurationException_DeserializationReturnedNull), 
                        configPath, Path.GetFullPath(configPath), json.Length);
                }

                if (config.Environments.Count == 0)
                {
                    _logger.LogError("Configuration file {ConfigPath} contains no environments. Found {RuleCount} rules", configPath, config.Rules.Count);
                    throw ConfigurationException(nameof(SR.ConfigurationException_NoEnvironments), 
                        configPath, Path.GetFullPath(configPath), config.Rules.Count);
                }

                // Validate environment count to prevent DoS attacks
                if (config.Environments.Count > _securityOptions.MaxEnvironmentsCount)
                {
                    _logger.LogError("Configuration file {ConfigPath} contains too many environments. Count: {Count}, Limit: {Limit}", 
                        configPath, config.Environments.Count, _securityOptions.MaxEnvironmentsCount);
                    throw ConfigurationException(nameof(SR.ConfigurationException_EnvironmentCountExceedsLimit), 
                        configPath, Path.GetFullPath(configPath), config.Environments.Count, _securityOptions.MaxEnvironmentsCount, config.Environments.Count - _securityOptions.MaxEnvironmentsCount);
                }

                if (config.Rules.Count == 0)
                {
                    _logger.LogError("Configuration file {ConfigPath} contains no rules. Found {EnvironmentCount} environment(s)", configPath, config.Environments.Count);
                    
                    throw ConfigurationException(nameof(SR.ConfigurationException_NoRules), 
                        configPath, Path.GetFullPath(configPath), config.Environments.Count);
                }

                // Validate rule count to prevent DoS attacks
                if (config.Rules.Count > _securityOptions.MaxRulesCount)
                {
                    _logger.LogError("Configuration file {ConfigPath} contains too many rules. Count: {Count}, Limit: {Limit}", 
                                     configPath, config.Rules.Count, _securityOptions.MaxRulesCount);
                    throw ConfigurationException(nameof(SR.ConfigurationException_RuleCountExceedsLimit), 
                                                      configPath, Path.GetFullPath(configPath), config.Rules.Count, _securityOptions.MaxRulesCount, config.Rules.Count - _securityOptions.MaxRulesCount);
                }

                // Validate configuration structure and integrity
                if (_configValidator != null)
                {
                    // ConfigValidator will use IValidatorFactory if provided, otherwise use default set
                    var supportedValidators = ValidatorConstants.AllValidatorTypes;
                    var validationErrors = _configValidator.Validate(config, supportedValidators);
                    
                    if (validationErrors.Count > 0)
                    {
                        var errorMessage = string.Join(System.Environment.NewLine, validationErrors);
                        _logger.LogError("Configuration structure validation failed for {ConfigPath}. Found {ErrorCount} error(s). Errors: {Errors}", 
                            configPath, validationErrors.Count, errorMessage);
                        throw ConfigurationException(nameof(SR.ConfigurationException_StructureValidationFailed), 
                            validationErrors.Count, configPath, Path.GetFullPath(configPath), System.Environment.NewLine, errorMessage);
                    }
                }

                _logger.LogInformation("Configuration loaded successfully from {ConfigPath}. Environments: {EnvironmentCount}, Rules: {RuleCount}", 
                    configPath, config.Environments.Count, config.Rules.Count);

                return config;
            }
            catch (JsonException ex)
            {
                var lineNumber = ex.LineNumber > 0 ? $"Line {ex.BytePositionInLine / 1024 + 1}" : "unknown line";
                _logger.LogError(ex, "Invalid JSON format in configuration file {ConfigPath}. Error at {LineNumber}: {ErrorMessage}", 
                    configPath, lineNumber, ex.Message);
                throw ConfigurationException(nameof(SR.ConfigurationException_InvalidJsonFormat), ex, 
                    configPath, Path.GetFullPath(configPath), ex.Message, lineNumber);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "I/O error reading configuration file {ConfigPath}. Error: {ErrorMessage}", configPath, ex.Message);
                throw ConfigurationException(nameof(SR.ConfigurationException_IOException), ex, 
                    configPath, Path.GetFullPath(configPath), ex.Message);
            }
            catch (ConfigurationException)
            {
                // Re-throw ConfigurationException as-is (already logged with detailed context)
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading configuration from {ConfigPath}. Exception type: {ExceptionType}, Message: {ErrorMessage}", 
                    configPath, ex.GetType().Name, ex.Message);
                throw ConfigurationException(nameof(SR.ConfigurationException_UnexpectedError), ex, 
                    configPath, Path.GetFullPath(configPath), ex.GetType().Name, ex.Message);
            }
        }

        /// <summary>
        /// Loads app settings from the specified file path and flattens them into a dictionary asynchronously.
        /// Supports both JSON (default) and YAML files (if the YAML loader is provided).
        /// JSON is the default format for app settings files like `appsettings.json`.
        /// Nested JSON objects are flattened with colon-separated keys (e.g., "Logging:LogLevel:Default").
        /// </summary>
        /// <param name="appSettingsPath">The path to the app settings file (JSON or YAML). JSON is the default format.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary with flattened app settings with colon-separated keys.</returns>
        /// <exception cref="System.ArgumentException">Thrown when <paramref name="appSettingsPath"/> is null or empty.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when the app settings file does not exist.</exception>
        /// <exception cref="SGuard.ConfigValidation.Exceptions.ConfigurationException">Thrown when the app settings file is invalid or cannot be deserialized.</exception>
        /// <exception cref="System.Text.Json.JsonException">Thrown when the JSON content is invalid.</exception>
        /// <exception cref="System.IO.IOException">Thrown when an I/O error occurs while reading the file.</exception>
        /// <example>
        /// <code>
        /// using Microsoft.Extensions.Logging;
        /// using Microsoft.Extensions.Logging.Abstractions;
        /// using Microsoft.Extensions.Options;
        /// using SGuard.ConfigValidation.Common;
        /// using SGuard.ConfigValidation.Services;
        /// 
        /// var securityOptions = Options.Create(new SecurityOptions());
        /// var logger = NullLogger&lt;ConfigLoader&gt;.Instance;
        /// var configLoader = new ConfigLoader(logger, securityOptions);
        /// 
        /// try
        /// {
        ///     var appSettings = configLoader.LoadAppSettings("appsettings.Production.json");
        ///     
        ///     // Access nested values using colon-separated keys
        ///     if (appSettings.TryGetValue("ConnectionStrings:DefaultConnection", out var connectionString))
        ///     {
        ///         Console.WriteLine($"Connection string: {connectionString}");
        ///     }
        ///     
        ///     if (appSettings.TryGetValue("Logging:LogLevel:Default", out var logLevel))
        ///     {
        ///         Console.WriteLine($"Log level: {logLevel}");
        ///     }
        /// }
        /// catch (FileNotFoundException ex)
        /// {
        ///     Console.WriteLine($"App settings file not found: {ex.FileName}");
        /// }
        /// catch (ConfigurationException ex)
        /// {
        ///     Console.WriteLine($"Configuration error: {ex.Message}");
        /// }
        /// </code>
        /// </example>
        public async Task<Dictionary<string, object>> LoadAppSettingsAsync(string appSettingsPath, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrWhiteSpace(appSettingsPath))
                {
                    _logger.LogError("AppSettings file path is null or empty");
                    throw ArgumentException(nameof(SR.ArgumentException_AppSettingsPathNullOrEmpty), nameof(appSettingsPath));
                }

                FileSecurity.EnsureFileExists(appSettingsPath, nameof(SR.ConfigurationException_AppSettingsFileNotFound), _logger);

                // Check if a file is YAML
                if (IsYamlFile(appSettingsPath) && _yamlLoader != null)
                {
                    var result = await _yamlLoader.LoadAppSettingsAsync(appSettingsPath, cancellationToken).ConfigureAwait(false);
                    stopwatch.Stop();
                    ValidationMetrics.RecordFileLoadingDuration(stopwatch.ElapsedMilliseconds);
                    return result;
                }
                // Check file size before reading to prevent DoS attacks
                FileSecurity.ValidateFileSize(appSettingsPath, _securityOptions.MaxFileSizeBytes, _logger, nameof(SR.ConfigurationException_AppSettingsFileSizeExceedsLimit));

                // Get file size to determine if we should use streaming
                var fileInfo = new FileInfo(appSettingsPath);
                var useStreaming = fileInfo.Length > SharedConstants.StreamingThresholdBytes;

                if (useStreaming)
                {
                    return await LoadAppSettingsStreamingAsync(appSettingsPath, cancellationToken).ConfigureAwait(false);
                }

                var json = await SafeFileSystem.SafeReadAllTextAsync(appSettingsPath, cancellationToken).ConfigureAwait(false);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("AppSettings file {AppSettingsPath} is empty, returning empty dictionary", appSettingsPath);
                    return new Dictionary<string, object>();
                }

                using var document = JsonDocument.Parse(json, JsonOptions.Document);
                
                // Pre-allocate dictionary with estimated capacity based on JSON size
                var estimatedCapacity = Math.Max(
                    SharedConstants.DefaultDictionaryCapacity, 
                    Math.Min(
                        SharedConstants.MaxDictionaryCapacity, 
                        json.Length / (1024 / SharedConstants.EstimatedKeysPerKilobyte)));
                var appSettings = new Dictionary<string, object>(estimatedCapacity);
                
                FlattenJson(document.RootElement, "", appSettings);
                
                _logger.LogInformation("App settings loaded successfully from {AppSettingsPath}. Found {SettingCount} settings", 
                    appSettingsPath, appSettings.Count);

                stopwatch.Stop();
                ValidationMetrics.RecordFileLoadingDuration(stopwatch.ElapsedMilliseconds);
                return appSettings;
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                ValidationMetrics.RecordFileLoadingDuration(stopwatch.ElapsedMilliseconds);
                var lineNumber = ex.LineNumber > 0 ? $"Line {ex.BytePositionInLine / 1024 + 1}" : "unknown line";
                _logger.LogError(ex, "Invalid JSON format in AppSettings file {AppSettingsPath}. Error at {LineNumber}: {ErrorMessage}", 
                    appSettingsPath, lineNumber, ex.Message);
                
                throw ConfigurationException(nameof(SR.ConfigurationException_AppSettingsInvalidJsonFormat), ex, 
                    appSettingsPath, Path.GetFullPath(appSettingsPath), ex.Message, lineNumber);
            }
            catch (IOException ex)
            {
                stopwatch.Stop();
                ValidationMetrics.RecordFileLoadingDuration(stopwatch.ElapsedMilliseconds);
                _logger.LogError(ex, "I/O error reading AppSettings file {AppSettingsPath}. Error: {ErrorMessage}", appSettingsPath, ex.Message);
                
                throw ConfigurationException(nameof(SR.ConfigurationException_AppSettingsIOException), ex, 
                    appSettingsPath, Path.GetFullPath(appSettingsPath), ex.Message);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ValidationMetrics.RecordFileLoadingDuration(stopwatch.ElapsedMilliseconds);
                _logger.LogError(ex, "Unexpected error loading app settings from {AppSettingsPath}. Exception type: {ExceptionType}, Message: {ErrorMessage}", 
                    appSettingsPath, ex.GetType().Name, ex.Message);
                
                throw ConfigurationException(nameof(SR.ConfigurationException_AppSettingsUnexpectedError), ex, 
                    appSettingsPath, Path.GetFullPath(appSettingsPath), ex.GetType().Name, ex.Message);
            }
        }

        /// <summary>
        /// Loads app settings using streaming JSON reader for large files.
        /// Uses FileStream with optimized buffer size for better memory efficiency.
        /// </summary>
        private async Task<Dictionary<string, object>> LoadAppSettingsStreamingAsync(string appSettingsPath, CancellationToken cancellationToken = default)
        {
            // Pre-allocate dictionary with estimated capacity
            var estimatedCapacity = (int)Math.Min(
                SharedConstants.MaxDictionaryCapacity, 
                new FileInfo(appSettingsPath).Length / 1024 * SharedConstants.EstimatedKeysPerMegabyte);
            var appSettings = new Dictionary<string, object>(estimatedCapacity);
            
            // Use FileStream with optimized buffer for large files
            using var fileStream = new FileStream(
                appSettingsPath, 
                FileMode.Open, 
                FileAccess.Read, 
                FileShare.Read, 
                bufferSize: SharedConstants.FileStreamBufferSize, 
                useAsync: true);
            
            using var document = await JsonDocument.ParseAsync(fileStream, JsonOptions.Document, cancellationToken).ConfigureAwait(false);
            
            FlattenJson(document.RootElement, "", appSettings);
            
            _logger.LogInformation("App settings loaded successfully from {AppSettingsPath} using streaming. Found {SettingCount} settings", 
                appSettingsPath, appSettings.Count);
            
            return appSettings;
        }

    private static void FlattenJson(JsonElement element, string prefix, Dictionary<string, object> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    // Optimized: string interpolation is more efficient than string.Concat for small strings
                    var newPrefix = string.IsNullOrEmpty(prefix) 
                        ? property.Name 
                        : $"{prefix}:{property.Name}";
                    FlattenJson(property.Value, newPrefix, result);
                }
                break;
                
            case JsonValueKind.Array:
                // For arrays, we store the raw JSON string
                result[prefix] = element.GetRawText();
                break;
                
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                // Use GetRawText() for better performance (avoids ToString() allocation)
                result[prefix] = element.ValueKind == JsonValueKind.String 
                    ? element.GetString() ?? string.Empty
                    : element.GetRawText();
                break;
        }
    }

    /// <summary>
    /// Gets the schema file path for a given configuration file path.
    /// </summary>
    /// <param name="configPath">The configuration file path.</param>
    /// <returns>The schema file path.</returns>
    private string GetSchemaPath(string configPath)
    {
        var directory = Path.GetDirectoryName(configPath);
        var fileName = Path.GetFileNameWithoutExtension(configPath);
        var extension = Path.GetExtension(configPath);
        
        // Try common schema file names
        var possibleSchemaPaths = new[]
        {
            Path.Combine(directory ?? ".", $"{fileName}.schema{extension}"),
            Path.Combine(directory ?? ".", $"{fileName}.schema.json"),
            Path.Combine(directory ?? ".", "sguard.schema.json"),
            Path.Combine(directory ?? ".", "appsettings.sguard.schema.json")
        };

        // Return the first existing schema file, or the first one as default
        var schemaPath = possibleSchemaPaths.FirstOrDefault(path => SafeFileSystem.FileExists(path)) 
                        ?? possibleSchemaPaths[0];

        return schemaPath;
    }

    /// <summary>
    /// Determines if a file is a YAML file based on its extension.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file is a YAML file, false otherwise.</returns>
    private static bool IsYamlFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".yaml" or ".yml";
    }
}