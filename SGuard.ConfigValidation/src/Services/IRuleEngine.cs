using SGuard.ConfigValidation.Common;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Interface for the rule engine that validates configurations.
/// </summary>
public interface IRuleEngine
{
    /// <summary>
    /// Validates a specific environment from a configuration file.
    /// </summary>
    /// <param name="configPath">The path to the configuration file.</param>
    /// <param name="environmentId">The ID of the environment to validate.</param>
    /// <returns>A rule engine result containing the validation results.</returns>
    RuleEngineResult ValidateEnvironment(string configPath, string environmentId);

    /// <summary>
    /// Validates all environments from a configuration file.
    /// </summary>
    /// <param name="configPath">The path to the configuration file.</param>
    /// <returns>A rule engine result containing validation results for all environments.</returns>
    RuleEngineResult ValidateAllEnvironments(string configPath);

    /// <summary>
    /// Validates a specific environment from a JSON configuration string.
    /// </summary>
    /// <param name="configJson">The JSON configuration string.</param>
    /// <param name="environmentId">The ID of the environment to validate.</param>
    /// <returns>A rule engine result containing the validation results.</returns>
    RuleEngineResult ValidateEnvironmentFromJson(string configJson, string environmentId);

    /// <summary>
    /// Validates all environments from a JSON configuration string.
    /// </summary>
    /// <param name="configJson">The JSON configuration string.</param>
    /// <returns>A rule engine result containing validation results for all environments.</returns>
    RuleEngineResult ValidateAllEnvironmentsFromJson(string configJson);

    /// <summary>
    /// Gets the list of supported validator types.
    /// </summary>
    /// <returns>An enumerable of supported validator type names.</returns>
    IEnumerable<string> GetSupportedValidators();
}

