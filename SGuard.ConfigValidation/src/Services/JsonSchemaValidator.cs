using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using NJsonSchema;
using NJsonSchema.Validation;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Resources;
using SGuard.ConfigValidation.Results;
using SGuard.ConfigValidation.Services.Abstract;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Validates JSON content against JSON Schema using NJsonSchema.
/// </summary>
public sealed partial class JsonSchemaValidator : ISchemaValidator
{
    // Cache for schema instances (file path -> schema instance)
    // Includes file modification time for cache invalidation
    private readonly ConcurrentDictionary<string, (JsonSchema Schema, DateTime LastModified)> _schemaCache = new();

    /// <summary>
    /// Validates JSON content against a schema.
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
            return SchemaValidationResult.Failure(message ?? "Schema validation failed: JSON content is required but was null or empty. Please provide valid JSON content to validate against the schema.");
        }

        if (string.IsNullOrWhiteSpace(schemaContent))
        {
            var message = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_SchemaContentRequired));
            return SchemaValidationResult.Failure(message ?? "Schema validation failed: Schema content is required but was null or empty. Please provide valid JSON schema content to validate against.");
        }

        try
        {
            var schema = await JsonSchema.FromJsonAsync(schemaContent);
            cancellationToken.ThrowIfCancellationRequested();
            return ValidateWithSchema(jsonContent, schema);
        }
        catch (JsonException ex)
        {
            var lineInfo = ex.LineNumber > 0 ? $" at line {ex.BytePositionInLine / 1024 + 1}" : "";
            var message = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_InvalidJsonFormat));
            return SchemaValidationResult.Failure(
                string.Format(message ?? "Schema validation failed: Invalid JSON format detected{0}. JSON parsing error: {1}. Please verify the JSON syntax is valid (check for missing commas, brackets, quotes, etc.) and try again.",
                    lineInfo, ex.Message));
        }
        catch (Exception ex)
        {
            var message = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_UnexpectedErrorSchemaParsing));
            return SchemaValidationResult.Failure(
                string.Format(message ?? "Schema validation failed: Unexpected error occurred during schema parsing. Exception type: {0}. Error: {1}. This is an unexpected error. Please check the logs for more details.",
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
    public async Task<SchemaValidationResult> ValidateAgainstFileAsync(string jsonContent, string schemaPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            var message = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_JsonContentRequired));
            return SchemaValidationResult.Failure(message ?? "Schema validation failed: JSON content is required but was null or empty. Please provide valid JSON content to validate against the schema.");
        }

        if (string.IsNullOrWhiteSpace(schemaPath))
        {
            var message = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_SchemaFilePathRequired));
            return SchemaValidationResult.Failure(message ?? "Schema validation failed: Schema file path is required but was null or empty. Please provide a valid path to the JSON schema file.");
        }

        if (!SafeFileSystem.FileExists(schemaPath))
        {
            var fullPath = Path.GetFullPath(schemaPath);
            var message = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_SchemaFileNotFound));
            return SchemaValidationResult.Failure(
                string.Format(message ?? "Schema validation failed: Schema file not found. Schema path: '{0}' (resolved to: '{1}'). Please ensure the schema file exists and the path is correct. Check for typos, verify the file location, and ensure you have read permissions.",
                    schemaPath, fullPath));
        }

        try
        {
            // Get file modification time for cache invalidation
            var fileInfo = new FileInfo(schemaPath);
            var lastModified = fileInfo.LastWriteTimeUtc;

            // Check cache
            if (_schemaCache.TryGetValue(schemaPath, out var cached))
            {
                // If a file hasn't changed, use cached schema
                if (cached.LastModified == lastModified)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return ValidateWithSchema(jsonContent, cached.Schema);
                }

                // File changed, remove from cache
                _schemaCache.TryRemove(schemaPath, out _);
            }
            
            // Load schema from a file
            var schemaContent = await SafeFileSystem.SafeReadAllTextAsync(schemaPath, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var schema = await JsonSchema.FromJsonAsync(schemaContent);
            cancellationToken.ThrowIfCancellationRequested();
            
            // Cache the schema with modification time
            _schemaCache.TryAdd(schemaPath, (schema, lastModified));
            
            return ValidateWithSchema(jsonContent, schema);
        }
        catch (FileNotFoundException ex)
        {
            var fullPath = Path.GetFullPath(schemaPath);
            var message = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_SchemaFileNotFound_WithError));
            return SchemaValidationResult.Failure(
                string.Format(message ?? "Schema validation failed: Schema file not found. Schema path: '{0}' (resolved to: '{1}'). Error: {2}. Please ensure the schema file exists and the path is correct.",
                    schemaPath, fullPath, ex.Message));
        }
        catch (IOException ex)
        {
            var fullPath = Path.GetFullPath(schemaPath);
            var message = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_IOError));
            return SchemaValidationResult.Failure(
                string.Format(message ?? "Schema validation failed: I/O error occurred while reading the schema file. Schema path: '{0}' (resolved to: '{1}'). Error: {2}. Possible causes: file is locked by another process, disk is full, network path is unavailable, or insufficient permissions. Please check file access permissions and ensure the file is not in use by another application.",
                    schemaPath, fullPath, ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            var fullPath = Path.GetFullPath(schemaPath);
            var message = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_AccessDenied));
            return SchemaValidationResult.Failure(
                string.Format(message ?? "Schema validation failed: Access denied reading schema file. Schema path: '{0}' (resolved to: '{1}'). Error: {2}. The current user does not have read permissions for this file. Please check file permissions and ensure the file is readable by the current user or process.",
                    schemaPath, fullPath, ex.Message));
        }
        catch (Exception ex)
        {
            var fullPath = Path.GetFullPath(schemaPath);
            var message = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_UnexpectedErrorReadingFile));
            return SchemaValidationResult.Failure(
                string.Format(message ?? "Schema validation failed: Unexpected error occurred while reading schema file. Schema path: '{0}' (resolved to: '{1}'). Exception type: {2}. Error: {3}. This is an unexpected error. Please check the logs for more details.",
                    schemaPath, fullPath, ex.GetType().Name, ex.Message));
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

            var errorMessages = errors.Select(FormatValidationError).ToList();
            return SchemaValidationResult.Failure(errorMessages);
        }
        catch (JsonException ex)
        {
            var lineInfo = ex.LineNumber > 0 ? $" at line {ex.BytePositionInLine / 1024 + 1}" : "";
            var message = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_InvalidJsonFormat));
            return SchemaValidationResult.Failure(
                string.Format(message ?? "Schema validation failed: Invalid JSON format detected{0}. JSON parsing error: {1}. Please verify the JSON syntax is valid (check for missing commas, brackets, quotes, etc.) and try again.",
                    lineInfo, ex.Message));
        }
        catch (Exception ex)
        {
            var message = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_UnexpectedErrorValidation));
            return SchemaValidationResult.Failure(
                string.Format(message ?? "Schema validation failed: Unexpected error occurred during schema validation. Exception type: {0}. Error: {1}. This is an unexpected error. Please check the logs for more details.",
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
                var template = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_MissingRequiredProperty));
                return string.Format(template ?? "Schema validation failed: Missing required property at JSON path '{0}'. Property path: '{1}'. Error kind: {2}. Missing properties: {3}. Please add the required property to the JSON document.",
                    jsonPath, path, kind, missingProperties);
            }
            else
            {
                var template = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_MissingRequiredProperty_NoProperties));
                return string.Format(template ?? "Schema validation failed: Missing required property at JSON path '{0}'. Property path: '{1}'. Error kind: {2}. Please add the required property to the JSON document.",
                    jsonPath, path, kind);
            }
        }

        if (message.Contains("Expected type"))
        {
            var expectedType = ExtractExpectedType(message);
            var actualType = ExtractActualType(message);
            if (actualType != null)
            {
                var template = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_InvalidType));
                return string.Format(template ?? "Schema validation failed: Invalid type at JSON path '{0}'. Property path: '{1}'. Expected type: {2}. Actual type: {3}. Please ensure the value matches the expected type specified in the schema.",
                    jsonPath, path, expectedType ?? kind, actualType);
            }
            else
            {
                var template = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_InvalidType_NoActualType));
                return string.Format(template ?? "Schema validation failed: Invalid type at JSON path '{0}'. Property path: '{1}'. Expected type: {2}. Please ensure the value matches the expected type specified in the schema.",
                    jsonPath, path, expectedType ?? kind);
            }
        }

        var errorTemplate = SR.ResourceManager.GetString(nameof(SR.JsonSchemaValidator_ValidationError));
        return string.Format(errorTemplate ?? "Schema validation failed: Validation error at JSON path '{0}'. Property path: '{1}'. Error kind: {2}. Error details: {3}. Please review the schema requirements and fix the JSON document accordingly.",
            jsonPath, path, kind, message);
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
}

