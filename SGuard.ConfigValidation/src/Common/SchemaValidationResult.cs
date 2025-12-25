namespace SGuard.ConfigValidation.Common;

/// <summary>
/// Represents the result of schema validation.
/// </summary>
public sealed class SchemaValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid { get; private init; }

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public List<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a formatted error message combining all errors.
    /// </summary>
    public string ErrorMessage => Errors.Count == 0 
        ? string.Empty 
        : string.Join(Environment.NewLine, Errors);

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static SchemaValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    public static SchemaValidationResult Failure(IEnumerable<string> errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    public static SchemaValidationResult Failure(string error) => new()
    {
        IsValid = false,
        Errors = [error]
    };
}

