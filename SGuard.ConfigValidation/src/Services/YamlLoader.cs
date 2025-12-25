using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Exceptions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Resources;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using JsonElement = System.Text.Json.JsonElement;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Service for loading YAML configuration files and app settings.
/// </summary>
public sealed class YamlLoader : IYamlLoader
{
    private readonly IDeserializer _yamlDeserializer;
    private readonly SecurityOptions _securityOptions;
    private readonly ILogger<YamlLoader> _logger;

    /// <summary>
    /// Initializes a new instance of the YamlLoader class.
    /// </summary>
    /// <param name="logger">Logger instance for logging YAML loading operations.</param>
    /// <param name="securityOptions">Security options configured via IOptions pattern.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> or <paramref name="securityOptions"/> is null.</exception>
    public YamlLoader(ILogger<YamlLoader> logger, IOptions<SecurityOptions> securityOptions)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(securityOptions);

        _logger = logger;
        _securityOptions = securityOptions.Value;

        // Configure YAML deserializer with camelCase naming convention
        _yamlDeserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).IgnoreUnmatchedProperties().Build();
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
            throw This.ArgumentException(nameof(SR.ArgumentException_ConfigPathNullOrEmpty), nameof(yamlPath));
        }

        if (!SafeFileSystem.FileExists(yamlPath))
        {
            var fullPath = Path.GetFullPath(yamlPath);

            _logger.LogError("YAML configuration file not found: {YamlPath} (resolved to: {FullPath})", yamlPath, fullPath);
            throw This.FileNotFoundException(yamlPath, nameof(SR.ConfigurationException_YamlFileNotFound), yamlPath, fullPath);
        }

        try
        {
            // Check file size before reading to prevent DoS attacks
            var fileInfo = new FileInfo(yamlPath);

            if (fileInfo.Length > _securityOptions.MaxFileSizeBytes)
            {
                var fileSizeMb = fileInfo.Length / (1024.0 * 1024.0);
                var maxSizeMb = _securityOptions.MaxFileSizeBytes / (1024.0 * 1024.0);

                _logger.LogError(
                    "YAML configuration file {YamlPath} exceeds maximum size limit. Size: {FileSize} bytes ({FileSizeMB:F2} MB), Limit: {MaxSize} bytes ({MaxSizeMB:F2} MB)",
                    yamlPath, fileInfo.Length, fileSizeMb, _securityOptions.MaxFileSizeBytes, maxSizeMb);

                throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlFileSizeExceedsLimit), yamlPath, Path.GetFullPath(yamlPath),
                                             fileSizeMb, fileInfo.Length, maxSizeMb, _securityOptions.MaxFileSizeBytes, fileSizeMb - maxSizeMb);
            }

            _logger.LogDebug("Reading YAML configuration file content from {YamlPath}", yamlPath);
            var yamlContent = SafeFileSystem.SafeReadAllText(yamlPath);

            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                _logger.LogError("YAML configuration file {YamlPath} is empty (file exists but contains no content)", yamlPath);
                throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlFileEmpty), yamlPath, Path.GetFullPath(yamlPath));
            }

            _logger.LogDebug("YAML configuration file read successfully. File size: {FileSize} bytes", yamlContent.Length);

            // Convert YAML to JSON for deserialization (using System.Text.Json)
            _logger.LogDebug("Converting YAML to JSON for deserialization");
            var jsonContent = ConvertYamlToJson(yamlContent);

            _logger.LogDebug("Deserializing configuration from JSON");
            var config = JsonSerializer.Deserialize<SGuardConfig>(jsonContent, JsonOptions.Deserialization);

            if (config == null)
            {
                _logger.LogError(
                    "Failed to deserialize YAML configuration from {YamlPath}. JSON deserialization returned null after YAML-to-JSON conversion",
                    yamlPath);

                throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlDeserializationReturnedNull), yamlPath, Path.GetFullPath(yamlPath),
                                             yamlContent.Length);
            }

            _logger.LogDebug("YAML configuration deserialized successfully. Found {EnvironmentCount} environments and {RuleCount} rules",
                             config.Environments?.Count ?? 0, config.Rules?.Count ?? 0);

            if (config.Environments == null || config.Environments.Count == 0)
            {
                _logger.LogError("YAML configuration file {YamlPath} contains no environments. Found {RuleCount} rules", yamlPath,
                                 config.Rules?.Count ?? 0);

                throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlNoEnvironments), yamlPath, Path.GetFullPath(yamlPath),
                                             config.Rules?.Count ?? 0);
            }

            // Validate environment count to prevent DoS attacks
            if (config.Environments.Count > _securityOptions.MaxEnvironmentsCount)
            {
                _logger.LogError("YAML configuration file {YamlPath} contains too many environments. Count: {Count}, Limit: {Limit}", yamlPath,
                                 config.Environments.Count, _securityOptions.MaxEnvironmentsCount);

                throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlEnvironmentCountExceedsLimit), yamlPath, Path.GetFullPath(yamlPath),
                                             config.Environments.Count, _securityOptions.MaxEnvironmentsCount,
                                             config.Environments.Count - _securityOptions.MaxEnvironmentsCount);
            }

            if (config.Rules == null)
            {
                _logger.LogDebug("No rules found in YAML configuration, initializing empty rules list");
                config.Rules = [];
            }
            else
            {
                // Validate rule count to prevent DoS attacks
                if (config.Rules.Count > _securityOptions.MaxRulesCount)
                {
                    _logger.LogError("YAML configuration file {YamlPath} contains too many rules. Count: {Count}, Limit: {Limit}", yamlPath,
                                     config.Rules.Count, _securityOptions.MaxRulesCount);

                    throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlRuleCountExceedsLimit), yamlPath, Path.GetFullPath(yamlPath),
                                                 config.Rules.Count, _securityOptions.MaxRulesCount,
                                                 config.Rules.Count - _securityOptions.MaxRulesCount);
                }
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
            var lineInfo = ex.Start.Line > 0 ? $"Line {ex.Start.Line}, Column {ex.Start.Column}" : "unknown location";

            _logger.LogError(ex, "YAML parsing error in configuration file {YamlPath}. Error at {LineInfo}: {ErrorMessage}", yamlPath, lineInfo,
                             ex.Message);

            throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlParsingError), ex, yamlPath, Path.GetFullPath(yamlPath), ex.Message,
                                         lineInfo);
        }
        catch (JsonException ex)
        {
            var lineNumber = ex.LineNumber > 0 ? $"Line {ex.BytePositionInLine / 1024 + 1}" : "unknown line";

            _logger.LogError(ex, "JSON conversion error for YAML configuration file {YamlPath}. Error at {LineNumber}: {ErrorMessage}", yamlPath,
                             lineNumber, ex.Message);

            throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlJsonConversionError), ex, yamlPath, Path.GetFullPath(yamlPath),
                                         ex.Message, lineNumber);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error reading YAML configuration file {YamlPath}. Error: {ErrorMessage}", yamlPath, ex.Message);
            throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlIOException), ex, yamlPath, Path.GetFullPath(yamlPath), ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied reading YAML configuration file {YamlPath}. Error: {ErrorMessage}", yamlPath, ex.Message);
            throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlAccessDenied), ex, yamlPath, Path.GetFullPath(yamlPath), ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Unexpected error loading YAML configuration from {YamlPath}. Exception type: {ExceptionType}, Message: {ErrorMessage}", yamlPath,
                ex.GetType().Name, ex.Message);

            throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlUnexpectedError), ex, yamlPath, Path.GetFullPath(yamlPath),
                                         ex.GetType().Name, ex.Message);
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
            throw This.ArgumentException(nameof(SR.ArgumentException_AppSettingsPathNullOrEmpty), nameof(yamlPath));
        }

        if (!SafeFileSystem.FileExists(yamlPath))
        {
            var fullPath = Path.GetFullPath(yamlPath);
            _logger.LogError("YAML app settings file not found: {YamlPath} (resolved to: {FullPath})", yamlPath, fullPath);
            throw This.FileNotFoundException(yamlPath, nameof(SR.ConfigurationException_AppSettingsFileNotFound), yamlPath, fullPath);
        }

        try
        {
            // Check file size before reading to prevent DoS attacks
            var fileInfo = new FileInfo(yamlPath);

            if (fileInfo.Length > _securityOptions.MaxFileSizeBytes)
            {
                var fileSizeMb = fileInfo.Length / (1024.0 * 1024.0);
                var maxSizeMb = _securityOptions.MaxFileSizeBytes / (1024.0 * 1024.0);

                _logger.LogError(
                    "YAML app settings file {YamlPath} exceeds maximum size limit. Size: {FileSize} bytes ({FileSizeMB:F2} MB), Limit: {MaxSize} bytes ({MaxSizeMB:F2} MB)",
                    yamlPath, fileInfo.Length, fileSizeMb, _securityOptions.MaxFileSizeBytes, maxSizeMb);

                throw This.ConfigurationException(nameof(SR.ConfigurationException_AppSettingsFileSizeExceedsLimit), yamlPath, Path.GetFullPath(yamlPath),
                                             fileSizeMb, fileInfo.Length, maxSizeMb, _securityOptions.MaxFileSizeBytes, fileSizeMb - maxSizeMb);
            }

            _logger.LogDebug("Reading YAML app settings file content from {YamlPath}", yamlPath);
            var yamlContent = SafeFileSystem.SafeReadAllText(yamlPath);

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
            var estimatedCapacity = Math.Max(SharedConstants.DefaultDictionaryCapacity,
                                             Math.Min(SharedConstants.MaxDictionaryCapacity,
                                                      yamlContent.Length / (1024 / SharedConstants.EstimatedKeysPerKilobyte)));
            var appSettings = new Dictionary<string, object>(estimatedCapacity);

            FlattenJson(document.RootElement, "", appSettings);

            _logger.LogInformation("YAML app settings loaded successfully from {YamlPath}. Found {SettingCount} settings", yamlPath,
                                   appSettings.Count);
            _logger.LogDebug("App settings keys: {Keys}", string.Join(", ", appSettings.Keys.Take(10)));

            return appSettings;
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            var lineInfo = ex.Start.Line > 0 ? $"Line {ex.Start.Line}, Column {ex.Start.Column}" : "unknown location";

            _logger.LogError(ex, "YAML parsing error in app settings file {YamlPath}. Error at {LineInfo}: {ErrorMessage}", yamlPath, lineInfo,
                             ex.Message);

            throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlParsingError), ex, yamlPath, Path.GetFullPath(yamlPath), ex.Message,
                                         lineInfo);
        }
        catch (JsonException ex)
        {
            var lineNumber = ex.LineNumber > 0 ? $"Line {ex.BytePositionInLine / 1024 + 1}" : "unknown line";

            _logger.LogError(ex, "JSON conversion error for YAML app settings file {YamlPath}. Error at {LineNumber}: {ErrorMessage}", yamlPath,
                             lineNumber, ex.Message);

            throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlJsonConversionError), ex, yamlPath, Path.GetFullPath(yamlPath),
                                         ex.Message, lineNumber);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error reading YAML app settings file {YamlPath}. Error: {ErrorMessage}", yamlPath, ex.Message);
            throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlIOException), ex, yamlPath, Path.GetFullPath(yamlPath), ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied reading YAML app settings file {YamlPath}. Error: {ErrorMessage}", yamlPath, ex.Message);
            throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlAccessDenied), ex, yamlPath, Path.GetFullPath(yamlPath), ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Unexpected error loading YAML app settings from {YamlPath}. Exception type: {ExceptionType}, Message: {ErrorMessage}", yamlPath,
                ex.GetType().Name, ex.Message);

            throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlUnexpectedError), ex, yamlPath, Path.GetFullPath(yamlPath),
                                         ex.GetType().Name, ex.Message);
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

            if (!ReferenceEquals(yamlObject, null))
            {
                // Serialize to JSON using System.Text.Json directly (internal use, no indentation for performance)
                return JsonSerializer.Serialize(yamlObject, JsonOptions.Internal);
            }

            throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlDeserializationNull));
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            _logger.LogError(ex, "YAML parsing error during conversion to JSON");
            throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlParseFailed), ex, ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON serialization error during YAML to JSON conversion");
            throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlSerializeFailed), ex, ex.Message);
        }
        catch (Exception ex) when (!(ex is ConfigurationException))
        {
            _logger.LogError(ex, "Unexpected error converting YAML to JSON");
            throw This.ConfigurationException(nameof(SR.ConfigurationException_YamlConvertFailed), ex, ex.Message);
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
                    var newPrefix = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";
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
                result[prefix] = element.ValueKind == JsonValueKind.String ? element.GetString() ?? string.Empty : element.GetRawText();
                break;
        }
    }
}