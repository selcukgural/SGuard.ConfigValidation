using SGuard.ConfigValidation.Common;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Interface for validating JSON against a schema.
/// </summary>
public interface ISchemaValidator
{
    /// <summary>
    /// Validates JSON content against a schema file.
    /// </summary>
    /// <param name="jsonContent">The JSON content to validate.</param>
    /// <param name="schemaPath">The path to the JSON schema file.</param>
    /// <returns>Validation result with any errors found.</returns>
    SchemaValidationResult ValidateAgainstFile(string jsonContent, string schemaPath);
}

