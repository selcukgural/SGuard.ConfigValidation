using System.Text.Json.Serialization;

namespace SGuard.ConfigValidation.Models;

/// <summary>
/// Represents a validation condition for a specific configuration key.
/// A condition contains a key (configuration path) and a list of validators to apply to that key.
/// </summary>
public sealed class Condition
{
    /// <summary>
    /// Gets the configuration key (path) to validate.
    /// Required field. Must not be null or empty.
    /// Keys can be nested paths separated by colons (e.g., "Logging:LogLevel:Default").
    /// </summary>
    [JsonPropertyName("key")]
    public required string Key { get; init; } 

    /// <summary>
    /// Gets the list of validators to apply to this key.
    /// Required field. Must contain at least one validator.
    /// Multiple validators can be applied to the same key (e.g., "required" and "min_len").
    /// </summary>
    [JsonPropertyName("condition")]
    public required List<ValidatorCondition> Validators { get; init; }
}