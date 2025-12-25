using System.Text.Json;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Exceptions;
using SGuard.ConfigValidation.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Service for loading YAML configuration files and app settings.
/// </summary>
public sealed class YamlLoader : IYamlLoader
{
    private readonly IDeserializer _yamlDeserializer;
    private readonly ISerializer _yamlSerializer;
    private readonly ILogger<YamlLoader> _logger;

    public YamlLoader(ILogger<YamlLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Configure YAML deserializer with camelCase naming convention
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
            
        // Configure YAML serializer (for conversion to JSON)
        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    /// <summary>
    /// Loads the SGuard configuration from a YAML file.
    /// Converts YAML to JSON internally for deserialization using System.Text.Json.
    /// </summary>
    /// <param name="yamlPath">The path to the YAML configuration file.</param>
    /// <returns>The loaded <see cref="SGuardConfig"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="yamlPath"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the YAML file does not exist.</exception>
    /// <exception cref="ConfigurationException">Thrown when the YAML file is empty, invalid, or cannot be deserialized.</exception>
    /// <exception cref="YamlDotNet.Core.YamlException">Thrown when YAML parsing fails.</exception>
    /// <exception cref="JsonException">Thrown when JSON conversion or deserialization fails.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while reading the file.</exception>
    public SGuardConfig LoadConfig(string yamlPath)
    {
        _logger.LogDebug("Loading YAML configuration from {YamlPath}", yamlPath);

        if (string.IsNullOrWhiteSpace(yamlPath))
        {
            _logger.LogError("YAML configuration file path is null or empty");
            throw new ArgumentException("YAML configuration file path cannot be null or empty.", nameof(yamlPath));
        }

        if (!SafeFileSystemHelper.SafeFileExists(yamlPath))
        {
            _logger.LogError("YAML configuration file not found: {YamlPath}", yamlPath);
            throw new FileNotFoundException($"YAML configuration file not found: {yamlPath}. Please ensure the file exists and the path is correct.", yamlPath);
        }

        try
        {
            _logger.LogDebug("Reading YAML configuration file content from {YamlPath}", yamlPath);
            var yamlContent = SafeFileSystemHelper.SafeReadAllText(yamlPath);
            
            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                _logger.LogError("YAML configuration file {YamlPath} is empty", yamlPath);
                throw new ConfigurationException($"YAML configuration file '{yamlPath}' is empty.");
            }

            _logger.LogDebug("YAML configuration file read successfully. File size: {FileSize} bytes", yamlContent.Length);

            // Convert YAML to JSON for deserialization (using System.Text.Json)
            _logger.LogDebug("Converting YAML to JSON for deserialization");
            var jsonContent = ConvertYamlToJson(yamlContent);
            
            _logger.LogDebug("Deserializing configuration from JSON");
            var config = JsonSerializer.Deserialize<SGuardConfig>(jsonContent, JsonOptions.Deserialization);
            
            if (config == null)
            {
                _logger.LogError("Failed to deserialize YAML configuration from {YamlPath}", yamlPath);
                throw new ConfigurationException($"Failed to deserialize YAML configuration from '{yamlPath}'. The file may be malformed.");
            }

            _logger.LogDebug("YAML configuration deserialized successfully. Found {EnvironmentCount} environments and {RuleCount} rules", 
                config.Environments?.Count ?? 0, config.Rules?.Count ?? 0);

            if (config.Environments == null || config.Environments.Count == 0)
            {
                _logger.LogError("YAML configuration file {YamlPath} contains no environments", yamlPath);
                throw new ConfigurationException($"YAML configuration file '{yamlPath}' must contain at least one environment definition.");
            }

            if (config.Rules == null)
            {
                _logger.LogDebug("No rules found in YAML configuration, initializing empty rules list");
                config.Rules = [];
            }

            _logger.LogInformation("YAML configuration loaded successfully from {YamlPath}. Environments: {EnvironmentCount}, Rules: {RuleCount}", 
                yamlPath, config.Environments.Count, config.Rules.Count);

            return config;
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (ConfigurationException)
        {
            throw;
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            _logger.LogError(ex, "YAML parsing error in configuration file {YamlPath}", yamlPath);
            throw new ConfigurationException($"YAML parsing error in configuration file '{yamlPath}': {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON conversion error for YAML configuration file {YamlPath}", yamlPath);
            throw new ConfigurationException($"JSON conversion error for YAML configuration file '{yamlPath}': {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error reading YAML configuration file {YamlPath}", yamlPath);
            throw new ConfigurationException($"I/O error reading YAML configuration file '{yamlPath}': {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied reading YAML configuration file {YamlPath}", yamlPath);
            throw new ConfigurationException($"Access denied reading YAML configuration file '{yamlPath}': {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading YAML configuration from {YamlPath}", yamlPath);
            throw new ConfigurationException($"Unexpected error loading YAML configuration from '{yamlPath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Loads app settings from a YAML file and flattens them into a dictionary.
    /// Converts YAML to JSON internally, then flattens nested objects with colon-separated keys.
    /// </summary>
    /// <param name="yamlPath">The path to the YAML app settings file.</param>
    /// <returns>A dictionary containing flattened app settings with colon-separated keys (e.g., "Logging:LogLevel:Default").</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="yamlPath"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the YAML file does not exist.</exception>
    /// <exception cref="ConfigurationException">Thrown when the YAML file is invalid or cannot be deserialized.</exception>
    /// <exception cref="YamlDotNet.Core.YamlException">Thrown when YAML parsing fails.</exception>
    /// <exception cref="JsonException">Thrown when JSON conversion or parsing fails.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while reading the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access to the file is denied.</exception>
    public Dictionary<string, object> LoadAppSettings(string yamlPath)
    {
        _logger.LogDebug("Loading YAML app settings from {YamlPath}", yamlPath);

        if (string.IsNullOrWhiteSpace(yamlPath))
        {
            _logger.LogError("YAML app settings file path is null or empty");
            throw new ArgumentException("YAML app settings file path cannot be null or empty.", nameof(yamlPath));
        }

        if (!SafeFileSystemHelper.SafeFileExists(yamlPath))
        {
            _logger.LogError("YAML app settings file not found: {YamlPath}", yamlPath);
            throw new FileNotFoundException($"YAML app settings file not found: {yamlPath}. Please ensure the file exists and the path is correct.", yamlPath);
        }

        try
        {
            _logger.LogDebug("Reading YAML app settings file content from {YamlPath}", yamlPath);
            var yamlContent = SafeFileSystemHelper.SafeReadAllText(yamlPath);
            
            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                _logger.LogWarning("YAML app settings file {YamlPath} is empty, returning empty dictionary", yamlPath);
                return new Dictionary<string, object>();
            }

            _logger.LogDebug("YAML app settings file read successfully. File size: {FileSize} bytes", yamlContent.Length);

            // Convert YAML to JSON for processing
            _logger.LogDebug("Converting YAML to JSON for processing");
            var jsonContent = ConvertYamlToJson(yamlContent);
            
            _logger.LogDebug("Parsing and flattening JSON structure");
            using var document = JsonDocument.Parse(jsonContent, JsonOptions.Document);
            
            // Pre-allocate dictionary with estimated capacity
            var estimatedCapacity = Math.Max(
                SharedConstants.DefaultDictionaryCapacity, 
                Math.Min(
                    SharedConstants.MaxDictionaryCapacity, 
                    yamlContent.Length / (1024 / SharedConstants.EstimatedKeysPerKilobyte)));
            var appSettings = new Dictionary<string, object>(estimatedCapacity);
            
            FlattenJson(document.RootElement, "", appSettings);
            
            _logger.LogInformation("YAML app settings loaded successfully from {YamlPath}. Found {SettingCount} settings", 
                yamlPath, appSettings.Count);
            _logger.LogDebug("App settings keys: {Keys}", string.Join(", ", appSettings.Keys.Take(10)));

            return appSettings;
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            _logger.LogError(ex, "YAML parsing error in app settings file {YamlPath}", yamlPath);
            throw new ConfigurationException($"YAML parsing error in app settings file '{yamlPath}': {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON conversion error for YAML app settings file {YamlPath}", yamlPath);
            throw new ConfigurationException($"JSON conversion error for YAML app settings file '{yamlPath}': {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error reading YAML app settings file {YamlPath}", yamlPath);
            throw new ConfigurationException($"I/O error reading YAML app settings file '{yamlPath}': {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied reading YAML app settings file {YamlPath}", yamlPath);
            throw new ConfigurationException($"Access denied reading YAML app settings file '{yamlPath}': {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading YAML app settings from {YamlPath}", yamlPath);
            throw new ConfigurationException($"Unexpected error loading YAML app settings from '{yamlPath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Converts YAML content to JSON string for deserialization.
    /// </summary>
    private string ConvertYamlToJson(string yamlContent)
    {
        try
        {
            // Deserialize YAML to object
            var yamlObject = _yamlDeserializer.Deserialize<object>(yamlContent);
            
            if (yamlObject == null)
            {
                throw new ConfigurationException("YAML deserialization returned null");
            }
            
            // Serialize to JSON using System.Text.Json directly (internal use, no indentation for performance)
            return JsonSerializer.Serialize(yamlObject, JsonOptions.Internal);
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            _logger.LogError(ex, "YAML parsing error during conversion to JSON");
            throw new ConfigurationException($"Failed to parse YAML: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON serialization error during YAML to JSON conversion");
            throw new ConfigurationException($"Failed to serialize YAML to JSON: {ex.Message}", ex);
        }
        catch (Exception ex) when (!(ex is ConfigurationException))
        {
            _logger.LogError(ex, "Unexpected error converting YAML to JSON");
            throw new ConfigurationException($"Failed to convert YAML to JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Flattens JSON structure into a dictionary with colon-separated keys.
    /// </summary>
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
                result[prefix] = element.GetRawText();
                break;
                
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                result[prefix] = element.ValueKind == JsonValueKind.String 
                    ? element.GetString() ?? string.Empty
                    : element.GetRawText();
                break;
        }
    }
}

