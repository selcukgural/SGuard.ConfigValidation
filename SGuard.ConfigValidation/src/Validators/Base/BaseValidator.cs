using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Results;

namespace SGuard.ConfigValidation.Validators.Base;

/// <summary>
/// Base class for validators with type-safe validation support.
/// </summary>
/// <typeparam name="T">The type of value this validator can handle.</typeparam>
public abstract class BaseValidator<T> : IValidator<T>
{
    /// <summary>
    /// Gets the validator type identifier (e.g., "required", "eq", "gt").
    /// </summary>
    public abstract string ValidatorType { get; }
    
    /// <summary>
    /// Gets the supported value types for this validator.
    /// Override this property to specify which value types this validator supports.
    /// </summary>
    public virtual IReadOnlySet<Common.ValueType> SupportedValueTypes { get; } = 
        new HashSet<Common.ValueType> { Common.ValueType.String, Common.ValueType.Number, Common.ValueType.Boolean, Common.ValueType.Null, Common.ValueType.JsonElement };
    
    /// <summary>
    /// Validates a value against the given condition.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="condition">The validation condition containing validator type, expected value, and error message.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the validation passed or failed.</returns>
    public abstract ValidationResult Validate(T? value, ValidatorCondition condition);
    
    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A <see cref="ValidationResult"/> indicating successful validation.</returns>
    protected ValidationResult CreateSuccess()
        => ValidationResult.Success();
        
    /// <summary>
    /// Creates a failed validation result with detailed information.
    /// </summary>
    /// <param name="message">The error message describing why the validation failed.</param>
    /// <param name="key">The configuration key that was validated.</param>
    /// <param name="value">The value that was validated.</param>
    /// <param name="ex">Optional exception that occurred during validation.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating failed validation with detailed information.</returns>
    protected ValidationResult CreateFailure(string message, string key, object? value, Exception? ex = null)
        => ValidationResult.Failure(message, ValidatorType, key, value, ex);
    
    /// <summary>
    /// Creates a failed validation result with detailed information including the expected value.
    /// </summary>
    /// <param name="message">The base error message describing why the validation failed.</param>
    /// <param name="key">The configuration key that was validated.</param>
    /// <param name="actualValue">The actual value that was validated.</param>
    /// <param name="expectedValue">The expected value for comparison.</param>
    /// <param name="ex">Optional exception that occurred during validation.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating failed validation with detailed information.</returns>
    protected ValidationResult CreateFailure(string message, string key, object? actualValue, object? expectedValue, Exception? ex = null)
    {
        var enhancedMessage = ValidationMessageFormatter.FormatValueComparisonError(message, key, actualValue, expectedValue);
        return ValidationResult.Failure(enhancedMessage, ValidatorType, key, actualValue, ex);
    }
}