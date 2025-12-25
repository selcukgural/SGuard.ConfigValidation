namespace SGuard.ConfigValidation.Validators;

/// <summary>
/// Factory interface for creating validator instances.
/// </summary>
public interface IValidatorFactory
{
    /// <summary>
    /// Gets a validator instance for the specified validator type.
    /// </summary>
    /// <param name="validatorType">The validator type name (e.g., "required", "eq", "gt"). Case-insensitive.</param>
    /// <returns>An <see cref="IValidator{T}"/> instance for the specified validator type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validatorType"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the validator type is not supported.</exception>
    IValidator<object> GetValidator(string validatorType);
    
    /// <summary>
    /// Gets the list of supported validator types.
    /// </summary>
    /// <returns>An enumerable of supported validator type names (e.g., "required", "eq", "gt").</returns>
    IEnumerable<string> GetSupportedValidators();
}