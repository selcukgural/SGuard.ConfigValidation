using SGuard.ConfigValidation.Results;

namespace SGuard.ConfigValidation.Services.Abstract;

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
    /// <returns>A task that represents the asynchronous operation. The task result contains a rule engine result with the validation results.</returns>
    Task<RuleEngineResult> ValidateEnvironmentAsync(string configPath, string environmentId);

    /// <summary>
    /// Validates all environments from a configuration file.
    /// </summary>
    /// <param name="configPath">The path to the configuration file.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a rule engine result with validation results for all environments.</returns>
    Task<RuleEngineResult> ValidateAllEnvironmentsAsync(string configPath);

    /// <summary>
    /// Validates a specific environment from a JSON configuration string.
    /// </summary>
    /// <param name="configJson">The JSON configuration string.</param>
    /// <param name="environmentId">The ID of the environment to validate.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a rule engine result with the validation results.</returns>
    Task<RuleEngineResult> ValidateEnvironmentFromJsonAsync(string configJson, string environmentId);

    /// <summary>
    /// Validates all environments from a JSON configuration string.
    /// </summary>
    /// <param name="configJson">The JSON configuration string.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a rule engine result with validation results for all environments.</returns>
    Task<RuleEngineResult> ValidateAllEnvironmentsFromJsonAsync(string configJson);
}

