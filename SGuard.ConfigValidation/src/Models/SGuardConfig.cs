using System.Text.Json.Serialization;

namespace SGuard.ConfigValidation.Models;

/// <summary>
/// Represents the root configuration object for SGuard configuration validation.
/// Contains version information, environment definitions, and validation rules.
/// </summary>
public sealed class SGuardConfig
{
    /// <summary>
    /// Gets or sets the configuration version.
    /// Required field. Must not be null or empty.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of environment definitions (e.g., Development, Staging, Production).
    /// Required field. Must contain at least one environment.
    /// </summary>
    [JsonPropertyName("environments")]
    public List<Environment> Environments { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of validation rules to apply to configuration files.
    /// Optional field. Can be empty if no validation rules are needed.
    /// </summary>
    [JsonPropertyName("rules")]
    public List<Rule> Rules { get; set; } = [];
}