using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Results;

namespace SGuard.ConfigValidation.Validators;

/// <summary>
/// Generic validator interface for type-safe validation.
/// </summary>
/// <typeparam name="T">The type of value this validator can handle.</typeparam>
public interface IValidator<in T>
{
    /// <summary>
    /// Gets the validator type identifier (e.g., "required", "eq", "gt").
    /// </summary>
    string ValidatorType { get; }
    
    /// <summary>
    /// Gets the supported value types for this validator.
    /// </summary>
    IReadOnlySet<Common.ValueType> SupportedValueTypes { get; }
    
    /// <summary>
    /// Validates a value against the given condition.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="condition">The validation condition.</param>
    /// <returns>The validation result.</returns>
    ValidationResult Validate(T? value, ValidatorCondition condition);
}