namespace SGuard.ConfigValidation.Results;

/// <summary>
/// Represents the result of a single validation operation.
/// Contains information about whether the validation passed or failed, along with details about the validation.
/// </summary>
/// <param name="isValid">Indicates whether the validation passed.</param>
/// <param name="message">The validation message (error message if validation failed, empty if successful).</param>
/// <param name="exception">Optional exception that occurred during validation.</param>
public sealed class ValidationResult(bool isValid, string message, Exception? exception = null)
{
    /// <summary>
    /// Gets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid { get; } = isValid;
    
    /// <summary>
    /// Gets the validation message. For failed validations, this contains the error message.
    /// For successful validations, this is typically empty.
    /// </summary>
    public string Message { get; } = message;
    
    /// <summary>
    /// Gets the exception that occurred during validation, if any.
    /// </summary>
    public Exception? Exception { get; init; } = exception;
    
    /// <summary>
    /// Gets the type of validator that produced this result (e.g., "required", "eq", "gt").
    /// </summary>
    public string ValidatorType { get; private init; } = string.Empty;
    
    /// <summary>
    /// Gets the configuration key that was validated.
    /// </summary>
    public string Key { get; private init; } = string.Empty;
    
    /// <summary>
    /// Gets the value that was validated.
    /// </summary>
    public object? Value { get; private init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A new <see cref="ValidationResult"/> instance indicating successful validation.</returns>
    public static ValidationResult Success() => new(true, string.Empty);

    /// <summary>
    /// Creates a failed validation result with detailed information.
    /// </summary>
    /// <param name="message">The error message describing why the validation failed.</param>
    /// <param name="validatorType">The type of validator that produced this result.</param>
    /// <param name="key">The configuration key that was validated.</param>
    /// <param name="value">The value that was validated.</param>
    /// <param name="exception">Optional exception that occurred during validation.</param>
    /// <returns>A new <see cref="ValidationResult"/> instance indicating failed validation with detailed information.</returns>
    public static ValidationResult Failure(string message, string validatorType, string key, object? value, Exception? exception = null) =>
        new(false, message, exception)
        {
            ValidatorType = validatorType,
            Key = key,
            Value = value
        };
}