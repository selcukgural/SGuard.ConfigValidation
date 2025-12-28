using SGuard.ConfigValidation.Results;

namespace SGuard.ConfigValidation.Services.Abstract;

/// <summary>
/// Interface for validating JSON against a schema.
/// </summary>
public interface ISchemaValidator
{
    /// <summary>
    /// Validates JSON content against a schema.
    /// </summary>
    /// <param name="jsonContent">The JSON content to validate.</param>
    /// <param name="schemaContent">The JSON schema content.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous validation operation. The task result contains the validation result with any errors found.</returns>
    Task<SchemaValidationResult> ValidateAsync(string jsonContent, string schemaContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates JSON content against a schema file.
    /// </summary>
    /// <param name="jsonContent">The JSON content to validate.</param>
    /// <param name="schemaPath">The path to the JSON schema file.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous validation operation. The task result contains the validation result with any errors found.</returns>
    Task<SchemaValidationResult> ValidateAgainstFileAsync(string jsonContent, string schemaPath, CancellationToken cancellationToken = default);
}

