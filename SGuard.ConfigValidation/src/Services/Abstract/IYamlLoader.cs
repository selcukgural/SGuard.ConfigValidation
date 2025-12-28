using SGuard.ConfigValidation.Models;

namespace SGuard.ConfigValidation.Services.Abstract;

/// <summary>
/// Interface for loading YAML configuration files.
/// </summary>
public interface IYamlLoader
{
    /// <summary>
    /// Loads the SGuard configuration from a YAML file asynchronously.
    /// </summary>
    /// <param name="yamlPath">The path to the YAML configuration file.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the loaded SGuard configuration.</returns>
    /// <exception cref="System.ArgumentException">Thrown when yamlPath is null or empty.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when the YAML file does not exist.</exception>
    /// <exception cref="SGuard.ConfigValidation.Exceptions.ConfigurationException">Thrown when the YAML file is invalid or cannot be deserialized.</exception>
    Task<SGuardConfig> LoadConfigAsync(string yamlPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads app settings from a YAML file and flattens them into a dictionary asynchronously.
    /// </summary>
    /// <param name="yamlPath">The path to the YAML app settings file.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary with flattened app settings with colon-separated keys.</returns>
    /// <exception cref="System.ArgumentException">Thrown when yamlPath is null or empty.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when the YAML file does not exist.</exception>
    /// <exception cref="SGuard.ConfigValidation.Exceptions.ConfigurationException">Thrown when the YAML file is invalid or cannot be deserialized.</exception>
    Task<Dictionary<string, object>> LoadAppSettingsAsync(string yamlPath, CancellationToken cancellationToken = default);
}

