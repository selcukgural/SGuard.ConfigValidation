using System.Text.Json.Serialization;

namespace SGuard.ConfigValidation.Models;

/// <summary>
/// Represents the details of a validation rule.
/// Contains a rule ID and a list of conditions that must be validated.
/// </summary>
public sealed class RuleDetail
{
    /// <summary>
    /// Gets or sets the unique identifier for this rule detail.
    /// Required field. Must not be null or empty.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of validation conditions that must be checked.
    /// Required field. Must contain at least one condition.
    /// </summary>
    [JsonPropertyName("conditions")]
    public required List<Condition> Conditions { get; init; } = [];
}