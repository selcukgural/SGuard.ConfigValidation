using System.Collections.Concurrent;
using System.Text.Json;
using NJsonSchema;
using NJsonSchema.Validation;
using SGuard.ConfigValidation.Common;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Validates JSON content against JSON Schema using NJsonSchema.
/// </summary>
public sealed class JsonSchemaValidator : ISchemaValidator
{
    // Cache for schema instances (file path -> schema instance)
    // Includes file modification time for cache invalidation
    private readonly ConcurrentDictionary<string, (JsonSchema Schema, DateTime LastModified)> _schemaCache = new();

    /// <summary>
    /// Validates JSON content against a schema.
    /// </summary>
    /// <param name="jsonContent">The JSON content to validate.</param>
    /// <param name="schemaContent">The JSON schema content.</param>
    /// <returns>A <see cref="SchemaValidationResult"/> containing validation errors if any.</returns>
    /// <exception cref="JsonException">Thrown when the JSON or schema format is invalid.</exception>
    public SchemaValidationResult Validate(string jsonContent, string schemaContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return SchemaValidationResult.Failure("JSON content cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(schemaContent))
        {
            return SchemaValidationResult.Failure("Schema content cannot be null or empty.");
        }

        try
        {
            var schema = JsonSchema.FromJsonAsync(schemaContent).GetAwaiter().GetResult();
            return ValidateWithSchema(jsonContent, schema);
        }
        catch (JsonException ex)
        {
            return SchemaValidationResult.Failure($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            return SchemaValidationResult.Failure($"Schema validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates JSON content against a schema file.
    /// Uses caching to improve performance when the same schema file is validated multiple times.
    /// Cache is invalidated when the schema file is modified.
    /// </summary>
    /// <param name="jsonContent">The JSON content to validate.</param>
    /// <param name="schemaPath">The path to the JSON schema file.</param>
    /// <returns>A <see cref="SchemaValidationResult"/> containing validation errors if any.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the schema file does not exist.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while reading the schema file.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access to the schema file is denied.</exception>
    /// <exception cref="JsonException">Thrown when the JSON or schema format is invalid.</exception>
    public SchemaValidationResult ValidateAgainstFile(string jsonContent, string schemaPath)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return SchemaValidationResult.Failure("JSON content cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(schemaPath))
        {
            return SchemaValidationResult.Failure("Schema file path cannot be null or empty.");
        }

        if (!SafeFileSystemHelper.SafeFileExists(schemaPath))
        {
            return SchemaValidationResult.Failure($"Schema file not found: {schemaPath}");
        }

        try
        {
            // Get file modification time for cache invalidation
            var fileInfo = new FileInfo(schemaPath);
            var lastModified = fileInfo.LastWriteTimeUtc;

            // Check cache
            if (_schemaCache.TryGetValue(schemaPath, out var cached))
            {
                // If file hasn't changed, use cached schema
                if (cached.LastModified == lastModified)
                {
                    return ValidateWithSchema(jsonContent, cached.Schema);
                }
                else
                {
                    // File changed, remove from cache
                    _schemaCache.TryRemove(schemaPath, out _);
                }
            }

            // Load schema from file
            var schemaContent = SafeFileSystemHelper.SafeReadAllText(schemaPath);
            var schema = JsonSchema.FromJsonAsync(schemaContent).GetAwaiter().GetResult();
            
            // Cache the schema with modification time
            _schemaCache.TryAdd(schemaPath, (schema, lastModified));
            
            return ValidateWithSchema(jsonContent, schema);
        }
        catch (FileNotFoundException ex)
        {
            return SchemaValidationResult.Failure($"Schema file not found: {ex.Message}");
        }
        catch (IOException ex)
        {
            return SchemaValidationResult.Failure($"I/O error reading schema file '{schemaPath}': {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return SchemaValidationResult.Failure($"Access denied reading schema file '{schemaPath}': {ex.Message}");
        }
        catch (Exception ex)
        {
            return SchemaValidationResult.Failure($"Error reading schema file '{schemaPath}': {ex.Message}");
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

            if (errors.Count == 0)
            {
                return SchemaValidationResult.Success();
            }

            var errorMessages = errors.Select(e => FormatValidationError(e)).ToList();
            return SchemaValidationResult.Failure(errorMessages);
        }
        catch (JsonException ex)
        {
            return SchemaValidationResult.Failure($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            return SchemaValidationResult.Failure($"Schema validation failed: {ex.Message}");
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

        // Extract a more user-friendly message
        if (message.Contains("Required properties"))
        {
            return $"Missing required property at '{path}': {error.Kind}";
        }

        if (message.Contains("Expected type"))
        {
            return $"Invalid type at '{path}': Expected {error.Kind}";
        }

        return $"Validation error at '{path}': {message}";
    }
}

