using SGuard.ConfigValidation.Models;

namespace SGuard.ConfigValidation.Services.Abstract;

/// <summary>
/// Interface for validating SGuard configuration structure and integrity.
/// </summary>
public interface IConfigValidator
{
    /// <summary>
    /// Validates the configuration structure, uniqueness, and integrity.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <param name="supportedValidators">The list of supported validator types.</param>
    /// <returns>A list of validation errors. Empty list means validation passed.</returns>
    List<string> Validate(SGuardConfig config, IEnumerable<string> supportedValidators);
}

