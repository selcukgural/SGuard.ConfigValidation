using System.Text.Json.Serialization;

namespace SGuard.ConfigValidation.Models;

/// <summary>
/// Represents a validation rule that applies to specific environments.
/// A rule contains an ID, a list of environment IDs where it applies, and the rule details (conditions).
/// </summary>
public sealed class Rule
{
    /// <summary>
    /// Gets the unique identifier for this rule.
    /// Required field. Must not be null or empty. Must be unique across all rules.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Gets the list of environment IDs where this rule applies.
    /// Required field. Must contain at least one environment ID. All environment IDs must exist in the environments list.
    /// </summary>
    [JsonPropertyName("environments")]
    public required List<string> Environments { get; init; } = [];

    /// <summary>
    /// Gets the rule details containing the validation conditions.
    /// Required field. Must not be null.
    /// </summary>
    [JsonPropertyName("rule")]
    public required RuleDetail RuleDetail { get; init; }
}