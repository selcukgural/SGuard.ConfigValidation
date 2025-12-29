using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Results;

namespace SGuard.ConfigValidation.Services.Abstract;

/// <summary>
/// Interface for validating configuration files against rules.
/// </summary>
public interface IFileValidator
{
    /// <summary>
    /// Validates a configuration file against the specified rules asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the file being validated.</param>
    /// <param name="applicableRules">The list of rules to apply to the file.</param>
    /// <param name="appSettings">The app settings dictionary to validate against.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a file validation result containing all validation results.</returns>
    /// <exception cref="System.ArgumentException">Thrown when filePath is null or empty, or when applicableRules or appSettings are null.</exception>
    Task<FileValidationResult> ValidateFileAsync(
        string filePath,
        List<Rule> applicableRules,
        Dictionary<string, object> appSettings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a configuration file against the specified rules.
    /// </summary>
    /// <param name="filePath">The path to the file being validated.</param>
    /// <param name="applicableRules">The list of rules to apply to the file.</param>
    /// <param name="appSettings">The app settings dictionary to validate against.</param>
    /// <returns>A file validation result containing all validation results.</returns>
    /// <exception cref="System.ArgumentException">Thrown when filePath is null or empty, or when applicableRules or appSettings are null.</exception>
    /// <remarks>
    /// This method is a synchronous wrapper. For better performance and cancellation support, prefer using <see cref="ValidateFileAsync"/>.
    /// </remarks>
    FileValidationResult ValidateFile(
        string filePath,
        List<Rule> applicableRules,
        Dictionary<string, object> appSettings);
}

