using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NJsonSchema;
using NJsonSchema.Validation;
using SGuard.ConfigValidation.Resources;
using SGuard.ConfigValidation.Results;
using SGuard.ConfigValidation.Services.Abstract;
using SGuard.ConfigValidation.Utils;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Validates JSON content against JSON Schema using NJsonSchema.
/// Monitors schema files for changes and automatically invalidates cache when files are modified.
/// </summary>
public sealed partial class JsonSchemaValidator : ISchemaValidator, IDisposable
{
    // Cache for schema instances (file path -> schema instance)
    // Includes file modification time for cache invalidation
    private readonly ConcurrentDictionary<string, (JsonSchema Schema, DateTime LastModified)> _schemaCache = new();

    // Cache for schema instances parsed from content strings (schema content hash -> schema instance)
    // Used to avoid re-parsing the same schema content multiple times
    private readonly ConcurrentDictionary<string, JsonSchema> _schemaContentCache = new();

    // FileSystemWatcher instances for monitoring schema file changes (normalized path -> watcher)
    private readonly ConcurrentDictionary<string, FileSystemWatcher> _fileWatchers = new();

    // Lock object for thread-safe file watcher operations
    private readonly object _watcherLock = new();

    // Flag to track disposal state
    private bool _disposed;

    /// <summary>
    /// Validates JSON content against a schema.
    /// Uses caching to improve performance when the same schema content is validated multiple times.
    /// </summary>
    /// <param name="jsonContent">The JSON content to validate.</param>
    /// <param name="schemaContent">The JSON schema content.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous validation operation. The task result contains a <see cref="SchemaValidationResult"/> with validation errors if any.</returns>
    /// <exception cref="System.Text.Json.JsonException">Thrown when the JSON or schema format is invalid.</exception>
    /// <example>
    /// <code>
    /// using SGuard.ConfigValidation.Services;
    /// 
    /// var schemaValidator = new JsonSchemaValidator();
    /// 
    /// var jsonContent = """
    /// {
    ///   "version": "1",
    ///   "environments": [{"id": "dev", "name": "Development", "path": "appsettings.Dev.json"}],
    ///   "rules": []
    /// }
    /// """;
    /// 
    /// var schemaContent = """
    /// {
    ///   "type": "object",
    ///   "properties": {
    ///     "version": {"type": "string"},
    ///     "environments": {"type": "array"},
    ///     "rules": {"type": "array"}
    ///   },
    ///   "required": ["version", "environments"]
    /// }
    /// """;
    /// 
    /// var result = await schemaValidator.ValidateAsync(jsonContent, schemaContent);
    /// 
    /// if (!result.IsValid)
    /// {
    ///     Console.WriteLine("Schema validation failed:");
    ///     foreach (var error in result.Errors)
    ///     {
    ///         Console.WriteLine($"  - {error}");
    ///     }
    /// }
    /// else
    /// {
    ///     Console.WriteLine("Schema validation passed!");
    /// }
    /// </code>
    /// </example>
    public async Task<SchemaValidationResult> ValidateAsync(string jsonContent, string schemaContent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            var message = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_JsonContentRequired));

            return SchemaValidationResult.Failure(
                message ??
                "Schema validation failed: JSON content is required but was null or empty. Please provide valid JSON content to validate against the schema.");
        }

        if (string.IsNullOrWhiteSpace(schemaContent))
        {
            var message = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_SchemaContentRequired));

            return SchemaValidationResult.Failure(
                message ??
                "Schema validation failed: Schema content is required but was null or empty. Please provide valid JSON schema content to validate against.");
        }

        try
        {
            // Compute hash of schema content for caching
            var schemaHash = ComputeSchemaContentHash(schemaContent);
            cancellationToken.ThrowIfCancellationRequested();

            // Check cache
            if (_schemaContentCache.TryGetValue(schemaHash, out var cachedSchema))
            {
                return ValidateWithSchema(jsonContent, cachedSchema);
            }

            // Parse schema and cache it
            var schema = await JsonSchema.FromJsonAsync(schemaContent, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            // Cache the schema (use GetOrAdd to handle concurrent additions)
            var cached = _schemaContentCache.GetOrAdd(schemaHash, schema);

            return ValidateWithSchema(jsonContent, cached);
        }
        catch (JsonException ex)
        {
            var lineInfo = ex.LineNumber > 0 ? $" at line {ex.BytePositionInLine / 1024 + 1}" : "";

            var message = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_InvalidJsonFormat));

            return SchemaValidationResult.Failure(
                string.Format(
                    message ??
                    "Schema validation failed: Invalid JSON format detected{0}. JSON parsing error: {1}. Please verify the JSON syntax is valid (check for missing commas, brackets, quotes, etc.) and try again.",
                    lineInfo, ex.Message));
        }
        catch (Exception ex)
        {
            var message = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_UnexpectedErrorSchemaParsing));

            return SchemaValidationResult.Failure(
                string.Format(
                    message ??
                    "Schema validation failed: Unexpected error occurred during schema parsing. Exception type: {0}. Error: {1}. This is an unexpected error. Please check the logs for more details.",
                    ex.GetType().Name, ex.Message));
        }
    }

    /// <summary>
    /// Validates JSON content against a schema file.
    /// Uses caching to improve performance when the same schema file is validated multiple times.
    /// Cache is invalidated when the schema file is modified.
    /// </summary>
    /// <param name="jsonContent">The JSON content to validate.</param>
    /// <param name="schemaPath">The path to the JSON schema file.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous validation operation. The task result contains a <see cref="SchemaValidationResult"/> with validation errors if any.</returns>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when the schema file does not exist.</exception>
    /// <exception cref="System.IO.IOException">Thrown when an I/O error occurs while reading the schema file.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when access to the schema file is denied.</exception>
    /// <exception cref="System.Text.Json.JsonException">Thrown when the JSON or schema format is invalid.</exception>
    /// <example>
    /// <code>
    /// using SGuard.ConfigValidation.Services;
    /// 
    /// var schemaValidator = new JsonSchemaValidator();
    /// 
    /// var jsonContent = await File.ReadAllTextAsync("sguard.json");
    /// var result = await schemaValidator.ValidateAgainstFileAsync(jsonContent, "sguard.schema.json");
    /// 
    /// if (!result.IsValid)
    /// {
    ///     Console.WriteLine("Schema validation failed:");
    ///     foreach (var error in result.Errors)
    ///     {
    ///         Console.WriteLine($"  - {error}");
    ///     }
    /// }
    /// else
    /// {
    ///     Console.WriteLine("Schema validation passed!");
    /// }
    /// </code>
    /// </example>
    public async Task<SchemaValidationResult> ValidateAgainstFileAsync(string jsonContent, string schemaPath,
                                                                       CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return SchemaValidationResult.Failure(SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_JsonContentRequired)) ??
                                                  "Schema validation failed: JSON content is required but was null or empty. Please provide valid JSON content to validate against the schema.");
        }

        if (string.IsNullOrWhiteSpace(schemaPath))
        {
            return SchemaValidationResult.Failure(SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_SchemaFilePathRequired)) ??
                                                  "Schema validation failed: Schema file path is required but was null or empty. Please provide a valid path to the JSON schema file.");
        }

        if (!FileUtility.FileExists(schemaPath))
        {
            return SchemaValidationResult.Failure(
                string.Format(
                    SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_SchemaFileNotFound)) ??
                    "Schema validation failed: Schema file not found. Schema path: '{0}' (resolved to: '{1}'). Please ensure the schema file exists and the path is correct. Check for typos, verify the file location, and ensure you have read permissions.",
                    schemaPath, Path.GetFullPath(schemaPath)));
        }

        try
        {
            // Normalize path for consistent cache and watcher management
            var normalizedPath = Path.GetFullPath(schemaPath);

            // Ensure file watcher is set up for this schema file
            EnsureFileWatcher(normalizedPath);

            // Get file modification time for cache invalidation
            var fileInfo = new FileInfo(normalizedPath);
            var lastModified = fileInfo.LastWriteTimeUtc;

            cancellationToken.ThrowIfCancellationRequested();

            // Check cache
            if (_schemaCache.TryGetValue(normalizedPath, out var cached))
            {
                // If a file hasn't changed, use cached schema
                if (cached.LastModified == lastModified)
                {
                    return ValidateWithSchema(jsonContent, cached.Schema);
                }

                // File changed, remove from cache
                _schemaCache.TryRemove(normalizedPath, out _);
            }

            // Load schema from a file
            var schemaContent = await FileUtility.ReadAllTextAsync(normalizedPath, cancellationToken: cancellationToken).ConfigureAwait(false);

            var schema = await JsonSchema.FromJsonAsync(schemaContent, cancellationToken).ConfigureAwait(false);

            // Cache the schema with modification time
            _schemaCache.TryAdd(normalizedPath, (schema, lastModified));

            return ValidateWithSchema(jsonContent, schema);
        }
        catch (FileNotFoundException ex)
        {
            return SchemaValidationResult.Failure(
                string.Format(
                    SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_SchemaFileNotFound_WithError)) ??
                    "Schema validation failed: Schema file not found. Schema path: '{0}' (resolved to: '{1}'). Error: {2}. Please ensure the schema file exists and the path is correct.",
                    schemaPath, Path.GetFullPath(schemaPath), ex.Message));
        }
        catch (IOException ex)
        {
            return SchemaValidationResult.Failure(
                string.Format(
                    SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_IOError)) ??
                    "Schema validation failed: I/O error occurred while reading the schema file. Schema path: '{0}' (resolved to: '{1}'). Error: {2}. Possible causes: file is locked by another process, disk is full, network path is unavailable, or insufficient permissions. Please check file access permissions and ensure the file is not in use by another application.",
                    schemaPath, Path.GetFullPath(schemaPath), ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return SchemaValidationResult.Failure(
                string.Format(
                    SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_AccessDenied)) ??
                    "Schema validation failed: Access denied reading schema file. Schema path: '{0}' (resolved to: '{1}'). Error: {2}. The current user does not have read permissions for this file. Please check file permissions and ensure the file is readable by the current user or process.",
                    schemaPath, Path.GetFullPath(schemaPath), ex.Message));
        }
        catch (Exception ex)
        {
            return SchemaValidationResult.Failure(
                string.Format(
                    SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_UnexpectedErrorReadingFile)) ??
                    "Schema validation failed: Unexpected error occurred while reading schema file. Schema path: '{0}' (resolved to: '{1}'). Exception type: {2}. Error: {3}. This is an unexpected error. Please check the logs for more details.",
                    schemaPath, Path.GetFullPath(schemaPath), ex.GetType().Name, ex.Message));
        }
    }

    /// <summary>
    /// Validates JSON content against a schema instance.
    /// </summary>
    private static SchemaValidationResult ValidateWithSchema(string jsonContent, JsonSchema schema)
    {
        try
        {
            var errors = schema.Validate(jsonContent);

            return errors.Count == 0 ? SchemaValidationResult.Success() : SchemaValidationResult.Failure(errors.Select(FormatValidationError));
        }
        catch (JsonException ex)
        {
            var lineInfo = ex.LineNumber > 0 ? $" at line {ex.BytePositionInLine / 1024 + 1}" : "";

            return SchemaValidationResult.Failure(
                string.Format(
                    SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_InvalidJsonFormat)) ??
                    "Schema validation failed: Invalid JSON format detected{0}. JSON parsing error: {1}. Please verify the JSON syntax is valid (check for missing commas, brackets, quotes, etc.) and try again.",
                    lineInfo, ex.Message));
        }
        catch (Exception ex)
        {
            return SchemaValidationResult.Failure(
                string.Format(
                    SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_UnexpectedErrorValidation)) ??
                    "Schema validation failed: Unexpected error occurred during schema validation. Exception type: {0}. Error: {1}. This is an unexpected error. Please check the logs for more details.",
                    ex.GetType().Name, ex.Message));
        }
    }

    /// <summary>
    /// Formats a validation error into a user-friendly message.
    /// </summary>
    private static string FormatValidationError(ValidationError error)
    {
        var path = string.IsNullOrWhiteSpace(error.Path) ? "root" : error.Path;
        var kind = error.Kind.ToString();
        var message = error.ToString();
        var jsonPath = string.IsNullOrWhiteSpace(error.Path) ? "$" : $"$.{error.Path}";

        // Extract a more user-friendly message
        if (message.Contains("Required properties"))
        {
            var missingProperties = ExtractMissingProperties(message);

            if (missingProperties != null)
            {
                return string.Format(
                    SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_MissingRequiredProperty)) ??
                    "Schema validation failed: Missing required property at JSON path '{0}'. Property path: '{1}'. Error kind: {2}. Missing properties: {3}. Please add the required property to the JSON document.",
                    jsonPath, path, kind, missingProperties);
            }

            return string.Format(
                SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_MissingRequiredProperty_NoProperties)) ??
                "Schema validation failed: Missing required property at JSON path '{0}'. Property path: '{1}'. Error kind: {2}. Please add the required property to the JSON document.",
                jsonPath, path, kind);
        }

        if (!message.Contains("Expected type"))
        {
            return string.Format(
                SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_ValidationError)) ??
                "Schema validation failed: Validation error at JSON path '{0}'. Property path: '{1}'. Error kind: {2}. Error details: {3}. Please review the schema requirements and fix the JSON document accordingly.",
                jsonPath, path, kind, message);
        }

        var expectedType = ExtractExpectedType(message);
        var actualType = ExtractActualType(message);

        if (actualType != null)
        {
            return string.Format(
                SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_InvalidType)) ??
                "Schema validation failed: Invalid type at JSON path '{0}'. Property path: '{1}'. Expected type: {2}. Actual type: {3}. Please ensure the value matches the expected type specified in the schema.",
                jsonPath, path, expectedType ?? kind, actualType);
        }

        return string.Format(
            SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_InvalidType_NoActualType)) ??
            "Schema validation failed: Invalid type at JSON path '{0}'. Property path: '{1}'. Expected type: {2}. Please ensure the value matches the expected type specified in the schema.",
            jsonPath, path, expectedType ?? kind);
    }

    private static string? ExtractMissingProperties(string message)
    {
        // Try to extract missing property names from the error message
        // This is a simple extraction - may need refinement based on the actual error format
        var match = ExtractMissingPropertiesRegex().Match(message);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractExpectedType(string message)
    {
        // Try to extract the expected type from the error message
        var match = ExtractExpectedTypeRegex().Match(message);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractActualType(string message)
    {
        // Try to extract the actual type from the error message
        var match = ExtractActualTypeRegex().Match(message);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"Expected\s+(\w+)", RegexOptions.IgnoreCase, "en-TR")]
    private static partial Regex ExtractExpectedTypeRegex();

    [GeneratedRegex(@"Actual\s+(\w+)", RegexOptions.IgnoreCase, "en-TR")]
    private static partial Regex ExtractActualTypeRegex();

    [GeneratedRegex(@"\[(.*?)\]")]
    private static partial Regex ExtractMissingPropertiesRegex();

    /// <summary>
    /// Computes a SHA256 hash of the schema content for use as a cache key.
    /// </summary>
    /// <param name="schemaContent">The schema content to hash.</param>
    /// <returns>A hexadecimal string representation of the hash.</returns>
    private static string ComputeSchemaContentHash(string schemaContent)
    {
        var bytes = Encoding.UTF8.GetBytes(schemaContent);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Ensures a FileSystemWatcher is set up for the specified schema file path.
    /// Creates a watcher if one doesn't exist for this file.
    /// </summary>
    /// <param name="schemaPath">The normalized path to the schema file.</param>
    private void EnsureFileWatcher(string schemaPath)
    {
        if (_disposed)
        {
            return;
        }

        // Check if watcher already exists
        if (_fileWatchers.ContainsKey(schemaPath))
        {
            return;
        }

        lock (_watcherLock)
        {
            // Double-check after acquiring lock
            if (_fileWatchers.ContainsKey(schemaPath) || _disposed)
            {
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(schemaPath);
                var fileName = Path.GetFileName(schemaPath);

                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                {
                    return;
                }

                var watcher = new FileSystemWatcher(directory, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };

                // Handle file changes
                watcher.Changed += (_, e) => OnSchemaFileChanged(e.FullPath);
                watcher.Deleted += (_, e) => OnSchemaFileChanged(e.FullPath);
                watcher.Renamed += (_, e) =>
                {
                    // Remove cache for old path
                    OnSchemaFileChanged(e.OldFullPath);
                    // Invalidate cache for new path if it exists
                    if (FileUtility.FileExists(e.FullPath))
                    {
                        OnSchemaFileChanged(e.FullPath);
                    }
                };

                _fileWatchers.TryAdd(schemaPath, watcher);
            }
            catch
            {
                // Silently fail if watcher cannot be created (e.g., insufficient permissions, network path)
                // Cache invalidation will still work via LastModified check during validation
            }
        }
    }

    /// <summary>
    /// Handles schema file change events by invalidating the cache and preloading the new schema.
    /// This enables hot reload: when a schema file changes, the new schema is loaded immediately
    /// and ready for the next validation call without delay.
    /// </summary>
    /// <param name="filePath">The path to the file that changed.</param>
    private void OnSchemaFileChanged(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            var normalizedPath = Path.GetFullPath(filePath);
            
            // Remove old cache entry
            _schemaCache.TryRemove(normalizedPath, out _);

            // Preload new schema for hot reload (fire and forget)
            // This ensures the new schema is ready immediately for the next validation call
            _ = Task.Run(async () =>
            {
                try
                {
                    // Wait a bit to ensure file write is complete
                    await Task.Delay(100).ConfigureAwait(false);

                    if (!FileUtility.FileExists(normalizedPath))
                    {
                        return;
                    }

                    var fileInfo = new FileInfo(normalizedPath);
                    var lastModified = fileInfo.LastWriteTimeUtc;

                    // Load and cache the new schema
                    var schemaContent = await FileUtility.ReadAllTextAsync(normalizedPath).ConfigureAwait(false);
                    var schema = await JsonSchema.FromJsonAsync(schemaContent).ConfigureAwait(false);

                    // Update cache with new schema
                    _schemaCache.TryAdd(normalizedPath, (schema, lastModified));
                }
                catch
                {
                    // Silently handle errors during preload
                    // Cache will be loaded on-demand during next validation
                }
            });
        }
        catch
        {
            // Silently handle path resolution errors
        }
    }

    /// <summary>
    /// Releases all resources used by the JsonSchemaValidator, including FileSystemWatcher instances.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_watcherLock)
        {
            if (_disposed)
            {
                return;
            }

            // Dispose all file watchers
            foreach (var watcher in _fileWatchers.Values)
            {
                try
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }
                catch
                {
                    // Silently handle disposal errors
                }
            }

            _fileWatchers.Clear();
            _disposed = true;
        }
    }
}