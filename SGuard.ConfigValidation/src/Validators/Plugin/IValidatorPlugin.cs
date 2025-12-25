namespace SGuard.ConfigValidation.Validators.Plugin;

/// <summary>
/// Interface for validator plugins that can be discovered and loaded dynamically.
/// </summary>
public interface IValidatorPlugin
{
    /// <summary>
    /// Gets the validator type name (e.g., "required", "min_len").
    /// </summary>
    string ValidatorType { get; }

    /// <summary>
    /// Gets the validator instance.
    /// </summary>
    IValidator<object> Validator { get; }
}

