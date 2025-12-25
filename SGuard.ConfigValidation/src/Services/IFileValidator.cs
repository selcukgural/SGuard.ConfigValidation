using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Models;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Interface for validating configuration files against rules.
/// </summary>
public interface IFileValidator
{
    /// <summary>
    /// Validates a configuration file against the specified rules.
    /// </summary>
    /// <param name="filePath">The path to the file being validated.</param>
    /// <param name="applicableRules">The list of rules to apply to the file.</param>
    /// <param name="appSettings">The app settings dictionary to validate against.</param>
    /// <returns>A file validation result containing all validation results.</returns>
    /// <exception cref="ArgumentException">Thrown when filePath is null or empty, or when applicableRules or appSettings are null.</exception>
    FileValidationResult ValidateFile(
        string filePath,
        List<Rule> applicableRules,
        Dictionary<string, object> appSettings);
}

