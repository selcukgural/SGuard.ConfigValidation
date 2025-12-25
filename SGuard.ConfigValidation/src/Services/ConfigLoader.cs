using System.Text.Json;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Exceptions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators;

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
    private readonly ILogger<ConfigLoader> _logger;

    /// <summary>
    /// Initializes a new instance of the ConfigLoader class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="schemaValidator">Optional schema validator. If provided, configuration files will be validated against the schema.</param>
    /// <param name="configValidator">Optional configuration validator. If provided, configuration structure will be validated.</param>
    /// <param name="yamlLoader">Optional YAML loader. If provided, YAML files will be supported.</param>
    public ConfigLoader(ILogger<ConfigLoader> logger, ISchemaValidator? schemaValidator = null, IConfigValidator? configValidator = null, IYamlLoader? yamlLoader = null)
    {
        _schemaValidator = schemaValidator;
        _configValidator = configValidator;
        _yamlLoader = yamlLoader;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

        /// <summary>
        /// Loads the SGuard configuration from the specified file path.
        /// Supports both JSON and YAML files (if YAML loader is provided).
        /// Validates the configuration against a schema if a schema validator is provided.
        /// Validates the configuration structure if a config validator is provided.
        /// </summary>
        /// <param name="configPath">The path to the configuration file (JSON or YAML).</param>
        /// <returns>The loaded and validated <see cref="SGuardConfig"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="configPath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the configuration file does not exist.</exception>
        /// <exception cref="ConfigurationException">Thrown when the configuration file is invalid, empty, malformed, fails schema validation, or fails structure validation.</exception>
        /// <exception cref="JsonException">Thrown when the JSON content is invalid.</exception>
        /// <exception cref="IOException">Thrown when an I/O error occurs while reading the file.</exception>
        public SGuardConfig LoadConfig(string configPath)
        {
            _logger.LogDebug("Loading configuration from {ConfigPath}", configPath);

            if (string.IsNullOrWhiteSpace(configPath))
            {
                _logger.LogError("Configuration file path is null or empty");
                throw new ArgumentException("Configuration file path cannot be null or empty.", nameof(configPath));
            }

            if (!SafeFileSystemHelper.SafeFileExists(configPath))
            {
                _logger.LogError("Configuration file not found: {ConfigPath}", configPath);
                throw new FileNotFoundException($"Configuration file not found: {configPath}. Please ensure the file exists and the path is correct.", configPath);
            }

            // Check if a file is YAML
            if (IsYamlFile(configPath) && _yamlLoader != null)
            {
                _logger.LogDebug("Detected YAML file, using YAML loader");
                return _yamlLoader.LoadConfig(configPath);
            }

            try
            {
                _logger.LogDebug("Reading configuration file content from {ConfigPath}", configPath);
                var json = SafeFileSystemHelper.SafeReadAllText(configPath);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogError("Configuration file {ConfigPath} is empty", configPath);
                    throw new ConfigurationException($"Configuration file '{configPath}' is empty.");
                }

                _logger.LogDebug("Configuration file read successfully. File size: {FileSize} bytes", json.Length);

                // Validate against schema if validator is provided
                if (_schemaValidator != null)
                {
                    var schemaPath = GetSchemaPath(configPath);
                    _logger.LogDebug("Checking for schema file at {SchemaPath}", schemaPath);
                    
                    if (SafeFileSystemHelper.SafeFileExists(schemaPath))
                    {
                        _logger.LogInformation("Validating configuration against schema {SchemaPath}", schemaPath);
                        var schemaValidationResult = _schemaValidator.ValidateAgainstFile(json, schemaPath);
                        
                        if (!schemaValidationResult.IsValid)
                        {
                            _logger.LogError("Schema validation failed for {ConfigPath}. Errors: {Errors}", 
                                configPath, schemaValidationResult.ErrorMessage);
                            throw new ConfigurationException(
                                $"Configuration file '{configPath}' does not match the schema. Errors:{System.Environment.NewLine}{schemaValidationResult.ErrorMessage}");
                        }
                        
                        _logger.LogDebug("Schema validation passed for {ConfigPath}", configPath);
                    }
                    else
                    {
                        _logger.LogDebug("Schema file not found at {SchemaPath}, skipping schema validation", schemaPath);
                    }
                }
                else
                {
                    _logger.LogDebug("No schema validator provided, skipping schema validation");
                }

                _logger.LogDebug("Deserializing configuration JSON");
                var config = JsonSerializer.Deserialize<SGuardConfig>(json, JsonOptions.Deserialization);
                
                if (config == null)
                {
                    _logger.LogError("Failed to deserialize configuration from {ConfigPath}", configPath);
                    throw new ConfigurationException($"Failed to deserialize configuration from '{configPath}'. The file may be malformed.");
                }

                _logger.LogDebug("Configuration deserialized successfully. Found {EnvironmentCount} environments and {RuleCount} rules", 
                    config.Environments?.Count ?? 0, config.Rules?.Count ?? 0);

                if (config.Environments == null || config.Environments.Count == 0)
                {
                    _logger.LogError("Configuration file {ConfigPath} contains no environments", configPath);
                    throw new ConfigurationException($"Configuration file '{configPath}' must contain at least one environment definition.");
                }

                if (config.Rules == null)
                {
                    _logger.LogDebug("No rules found in configuration, initializing empty rules list");
                    config.Rules = [];
                }

                // Validate configuration structure and integrity
                if (_configValidator != null)
                {
                    _logger.LogDebug("Validating configuration structure and integrity");
                    // ConfigValidator will use IValidatorFactory if provided, otherwise use default set
                    var supportedValidators = ValidatorConstants.AllValidatorTypes;
                    var validationErrors = _configValidator.Validate(config, supportedValidators);
                    
                    if (validationErrors.Count > 0)
                    {
                        var errorMessage = string.Join(System.Environment.NewLine, validationErrors);
                        _logger.LogError("Configuration validation failed for {ConfigPath}. Errors: {Errors}", 
                            configPath, errorMessage);
                        throw new ConfigurationException(
                            $"Configuration file '{configPath}' validation failed:{System.Environment.NewLine}{errorMessage}");
                    }
                    
                    _logger.LogDebug("Configuration structure validation passed");
                }
                else
                {
                    _logger.LogDebug("No configuration validator provided, skipping structure validation");
                }

                _logger.LogInformation("Configuration loaded successfully from {ConfigPath}. Environments: {EnvironmentCount}, Rules: {RuleCount}", 
                    configPath, config.Environments.Count, config.Rules.Count);

                return config;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON format in configuration file {ConfigPath}", configPath);
                throw new ConfigurationException($"Invalid JSON format in configuration file '{configPath}': {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error reading configuration file {ConfigPath}", configPath);
                throw new ConfigurationException($"Error reading configuration file '{configPath}': {ex.Message}", ex);
            }
            catch (ConfigurationException)
            {
                // Re-throw ConfigurationException as-is (already logged)
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading configuration from {ConfigPath}", configPath);
                throw new ConfigurationException($"Unexpected error loading configuration from '{configPath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads app settings from the specified file path and flattens them into a dictionary.
        /// Supports both JSON and YAML files (if YAML loader is provided).
        /// Nested JSON objects are flattened with colon-separated keys (e.g., "Logging:LogLevel:Default").
        /// </summary>
        /// <param name="appSettingsPath">The path to the app settings file (JSON or YAML).</param>
        /// <returns>A dictionary containing flattened app settings with colon-separated keys.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="appSettingsPath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the app settings file does not exist.</exception>
        /// <exception cref="ConfigurationException">Thrown when the app settings file is invalid or cannot be deserialized.</exception>
        /// <exception cref="JsonException">Thrown when the JSON content is invalid.</exception>
        /// <exception cref="IOException">Thrown when an I/O error occurs while reading the file.</exception>
        public Dictionary<string, object> LoadAppSettings(string appSettingsPath)
        {
            _logger.LogDebug("Loading app settings from {AppSettingsPath}", appSettingsPath);

            if (string.IsNullOrWhiteSpace(appSettingsPath))
            {
                _logger.LogError("AppSettings file path is null or empty");
                throw new ArgumentException("AppSettings file path cannot be null or empty.", nameof(appSettingsPath));
            }

            if (!SafeFileSystemHelper.SafeFileExists(appSettingsPath))
            {
                _logger.LogError("AppSettings file not found: {AppSettingsPath}", appSettingsPath);
                throw new FileNotFoundException($"AppSettings file not found: {appSettingsPath}. Please ensure the file exists and the path is correct.", appSettingsPath);
            }

            // Check if a file is YAML
            if (IsYamlFile(appSettingsPath) && _yamlLoader != null)
            {
                _logger.LogDebug("Detected YAML file, using YAML loader");
                return _yamlLoader.LoadAppSettings(appSettingsPath);
            }

            try
            {
                // Get file size to determine if we should use streaming
                var fileInfo = new FileInfo(appSettingsPath);
                var useStreaming = fileInfo.Length > SharedConstants.StreamingThresholdBytes;

                if (useStreaming)
                {
                    _logger.LogDebug("Using streaming JSON reader for large file ({FileSize} bytes)", fileInfo.Length);
                    return LoadAppSettingsStreaming(appSettingsPath);
                }

                _logger.LogDebug("Reading app settings file content from {AppSettingsPath}", appSettingsPath);
                var json = SafeFileSystemHelper.SafeReadAllText(appSettingsPath);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("AppSettings file {AppSettingsPath} is empty, returning empty dictionary", appSettingsPath);
                    return new Dictionary<string, object>();
                }

                _logger.LogDebug("AppSettings file read successfully. File size: {FileSize} bytes", json.Length);

                _logger.LogDebug("Parsing and flattening JSON structure");
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
                _logger.LogDebug("App settings keys: {Keys}", string.Join(", ", appSettings.Keys));

                return appSettings;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON format in AppSettings file {AppSettingsPath}", appSettingsPath);
                throw new ConfigurationException($"Invalid JSON format in AppSettings file '{appSettingsPath}': {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error reading AppSettings file {AppSettingsPath}", appSettingsPath);
                throw new ConfigurationException($"Error reading AppSettings file '{appSettingsPath}': {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading app settings from {AppSettingsPath}", appSettingsPath);
                throw new ConfigurationException($"Unexpected error loading app settings from '{appSettingsPath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads app settings using streaming JSON reader for large files.
        /// Uses FileStream with optimized buffer size for better memory efficiency.
        /// </summary>
        private Dictionary<string, object> LoadAppSettingsStreaming(string appSettingsPath)
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
                useAsync: false);
            
            using var document = JsonDocument.Parse(fileStream, JsonOptions.Document);
            
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
        var schemaPath = possibleSchemaPaths.FirstOrDefault(SafeFileSystemHelper.SafeFileExists) 
                        ?? possibleSchemaPaths[0];
        
        _logger.LogDebug("Resolved schema path: {SchemaPath} (exists: {Exists})", 
            schemaPath, SafeFileSystemHelper.SafeFileExists(schemaPath));

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