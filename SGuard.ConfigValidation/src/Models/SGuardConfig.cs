using System.Text.Json.Serialization;

namespace SGuard.ConfigValidation.Models;

/// <summary>
/// Represents the root configuration for SGuard, containing version information,
/// a list of environments, and a list of rules.
/// </summary>
public sealed class SGuardConfig
{
    /// <summary>
    /// Gets the version of the configuration schema.
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    /// <summary>
    /// Gets the list of environment configurations.
    /// </summary>
    [JsonPropertyName("environments")]
    public required List<Environment> Environments { get; init; }

    /// <summary>
    /// Gets or sets the list of validation rules.
    /// </summary>
    [JsonPropertyName("rules")]
    public required List<Rule> Rules { get; init; }
}