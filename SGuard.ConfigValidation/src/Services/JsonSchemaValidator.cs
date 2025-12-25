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
            return SchemaValidationResult.Failure(
                "Schema validation failed: JSON content is required but was null or empty. " +
                "Please provide valid JSON content to validate against the schema.");
        }

        if (string.IsNullOrWhiteSpace(schemaContent))
        {
            return SchemaValidationResult.Failure(
                "Schema validation failed: Schema content is required but was null or empty. " +
                "Please provide valid JSON schema content to validate against.");
        }

        try
        {
            var schema = JsonSchema.FromJsonAsync(schemaContent).GetAwaiter().GetResult();
            return ValidateWithSchema(jsonContent, schema);
        }
        catch (JsonException ex)
        {
            var lineInfo = ex.LineNumber > 0 ? $" at line {ex.BytePositionInLine / 1024 + 1}" : "";
            return SchemaValidationResult.Failure(
                $"Schema validation failed: Invalid JSON format detected{lineInfo}. " +
                $"JSON parsing error: {ex.Message}. " +
                "Please verify the JSON syntax is valid (check for missing commas, brackets, quotes, etc.) and try again.");
        }
        catch (Exception ex)
        {
            return SchemaValidationResult.Failure(
                $"Schema validation failed: Unexpected error occurred during schema parsing. " +
                $"Exception type: {ex.GetType().Name}. " +
                $"Error: {ex.Message}. " +
                "This is an unexpected error. Please check the logs for more details.");
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
            return SchemaValidationResult.Failure(
                "Schema validation failed: JSON content is required but was null or empty. " +
                "Please provide valid JSON content to validate against the schema.");
        }

        if (string.IsNullOrWhiteSpace(schemaPath))
        {
            return SchemaValidationResult.Failure(
                "Schema validation failed: Schema file path is required but was null or empty. " +
                "Please provide a valid path to the JSON schema file.");
        }

        if (!SafeFileSystem.FileExists(schemaPath))
        {
            var fullPath = Path.GetFullPath(schemaPath);
            return SchemaValidationResult.Failure(
                $"Schema validation failed: Schema file not found. " +
                $"Schema path: '{schemaPath}' (resolved to: '{fullPath}'). " +
                "Please ensure the schema file exists and the path is correct. " +
                "Check for typos, verify the file location, and ensure you have read permissions.");
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

                // File changed, remove from cache
                _schemaCache.TryRemove(schemaPath, out _);
            }

            // Load schema from file
            var schemaContent = SafeFileSystem.SafeReadAllText(schemaPath);
            var schema = JsonSchema.FromJsonAsync(schemaContent).GetAwaiter().GetResult();
            
            // Cache the schema with modification time
            _schemaCache.TryAdd(schemaPath, (schema, lastModified));
            
            return ValidateWithSchema(jsonContent, schema);
        }
        catch (FileNotFoundException ex)
        {
            var fullPath = Path.GetFullPath(schemaPath);
            return SchemaValidationResult.Failure(
                $"Schema validation failed: Schema file not found. " +
                $"Schema path: '{schemaPath}' (resolved to: '{fullPath}'). " +
                $"Error: {ex.Message}. " +
                "Please ensure the schema file exists and the path is correct.");
        }
        catch (IOException ex)
        {
            var fullPath = Path.GetFullPath(schemaPath);
            return SchemaValidationResult.Failure(
                $"Schema validation failed: I/O error occurred while reading the schema file. " +
                $"Schema path: '{schemaPath}' (resolved to: '{fullPath}'). " +
                $"Error: {ex.Message}. " +
                "Possible causes: file is locked by another process, disk is full, network path is unavailable, or insufficient permissions. " +
                "Please check file access permissions and ensure the file is not in use by another application.");
        }
        catch (UnauthorizedAccessException ex)
        {
            var fullPath = Path.GetFullPath(schemaPath);
            return SchemaValidationResult.Failure(
                $"Schema validation failed: Access denied reading schema file. " +
                $"Schema path: '{schemaPath}' (resolved to: '{fullPath}'). " +
                $"Error: {ex.Message}. " +
                "The current user does not have read permissions for this file. " +
                "Please check file permissions and ensure the file is readable by the current user or process.");
        }
        catch (Exception ex)
        {
            var fullPath = Path.GetFullPath(schemaPath);
            return SchemaValidationResult.Failure(
                $"Schema validation failed: Unexpected error occurred while reading schema file. " +
                $"Schema path: '{schemaPath}' (resolved to: '{fullPath}'). " +
                $"Exception type: {ex.GetType().Name}. " +
                $"Error: {ex.Message}. " +
                "This is an unexpected error. Please check the logs for more details.");
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
            var lineInfo = ex.LineNumber > 0 ? $" at line {ex.BytePositionInLine / 1024 + 1}" : "";
            return SchemaValidationResult.Failure(
                $"Schema validation failed: Invalid JSON format detected{lineInfo}. " +
                $"JSON parsing error: {ex.Message}. " +
                "Please verify the JSON syntax is valid (check for missing commas, brackets, quotes, etc.) and try again.");
        }
        catch (Exception ex)
        {
            return SchemaValidationResult.Failure(
                $"Schema validation failed: Unexpected error occurred during schema validation. " +
                $"Exception type: {ex.GetType().Name}. " +
                $"Error: {ex.Message}. " +
                "This is an unexpected error. Please check the logs for more details.");
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
            return $"Schema validation failed: Missing required property at JSON path '{jsonPath}'. " +
                   $"Property path: '{path}'. " +
                   $"Error kind: {kind}. " +
                   (missingProperties != null ? $"Missing properties: {missingProperties}. " : "") +
                   "Please add the required property to the JSON document.";
        }

        if (message.Contains("Expected type"))
        {
            var expectedType = ExtractExpectedType(message);
            var actualType = ExtractActualType(message);
            return $"Schema validation failed: Invalid type at JSON path '{jsonPath}'. " +
                   $"Property path: '{path}'. " +
                   $"Expected type: {expectedType ?? kind}. " +
                   (actualType != null ? $"Actual type: {actualType}. " : "") +
                   "Please ensure the value matches the expected type specified in the schema.";
        }

        return $"Schema validation failed: Validation error at JSON path '{jsonPath}'. " +
               $"Property path: '{path}'. " +
               $"Error kind: {kind}. " +
               $"Error details: {message}. " +
               "Please review the schema requirements and fix the JSON document accordingly.";
    }
    
    private static string? ExtractMissingProperties(string message)
    {
        // Try to extract missing property names from the error message
        // This is a simple extraction - may need refinement based on actual error format
        var match = System.Text.RegularExpressions.Regex.Match(message, @"\[(.*?)\]");
        return match.Success ? match.Groups[1].Value : null;
    }
    
    private static string? ExtractExpectedType(string message)
    {
        // Try to extract expected type from the error message
        var match = System.Text.RegularExpressions.Regex.Match(message, @"Expected\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }
    
    private static string? ExtractActualType(string message)
    {
        // Try to extract actual type from the error message
        var match = System.Text.RegularExpressions.Regex.Match(message, @"Actual\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }
}

