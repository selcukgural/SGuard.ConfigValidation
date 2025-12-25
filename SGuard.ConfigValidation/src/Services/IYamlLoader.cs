using SGuard.ConfigValidation.Models;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Interface for loading YAML configuration files.
/// </summary>
public interface IYamlLoader
{
    /// <summary>
    /// Loads the SGuard configuration from a YAML file.
    /// </summary>
    /// <param name="yamlPath">The path to the YAML configuration file.</param>
    /// <returns>The loaded SGuard configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when yamlPath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the YAML file does not exist.</exception>
    /// <exception cref="ConfigurationException">Thrown when the YAML file is invalid or cannot be deserialized.</exception>
    SGuardConfig LoadConfig(string yamlPath);

    /// <summary>
    /// Loads app settings from a YAML file and flattens them into a dictionary.
    /// </summary>
    /// <param name="yamlPath">The path to the YAML app settings file.</param>
    /// <returns>A dictionary containing flattened app settings with colon-separated keys.</returns>
    /// <exception cref="ArgumentException">Thrown when yamlPath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the YAML file does not exist.</exception>
    /// <exception cref="ConfigurationException">Thrown when the YAML file is invalid or cannot be deserialized.</exception>
    Dictionary<string, object> LoadAppSettings(string yamlPath);
}

