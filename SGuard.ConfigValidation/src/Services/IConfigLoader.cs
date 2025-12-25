using SGuard.ConfigValidation.Models;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Interface for loading configuration files and app settings.
/// </summary>
public interface IConfigLoader
{
    /// <summary>
    /// Loads the SGuard configuration from the specified file path.
    /// </summary>
    /// <param name="configPath">The path to the configuration file.</param>
    /// <returns>The loaded SGuard configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when configPath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the configuration file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the configuration file is invalid or cannot be deserialized.</exception>
    SGuardConfig LoadConfig(string configPath);

    /// <summary>
    /// Loads app settings from the specified file path and flattens them into a dictionary.
    /// </summary>
    /// <param name="appSettingsPath">The path to the app settings file.</param>
    /// <returns>A dictionary containing flattened app settings with colon-separated keys.</returns>
    /// <exception cref="ArgumentException">Thrown when appSettingsPath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the app settings file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the app settings file is invalid or cannot be deserialized.</exception>
    Dictionary<string, object> LoadAppSettings(string appSettingsPath);
}

