using System.Text.Json.Serialization;
using SGuard.ConfigValidation.Common;

namespace SGuard.ConfigValidation.Models;

/// <summary>
/// Represents a validator condition that specifies how to validate a configuration value.
/// Contains the validator type, expected value (if required), and error message.
/// </summary>
public sealed class ValidatorCondition
{
    /// <summary>
    /// Gets or sets the validator type name (e.g., "required", "eq", "gt", "min_len").
    /// Required field. Must not be null or empty. Must be one of the supported validator types.
    /// </summary>
    [JsonPropertyName("validator")]
    public required string Validator { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected value for the validator (if required).
    /// Optional field for some validators (e.g., "required"), but required for others (e.g., "eq", "gt", "min_len", "max_len", "in").
    /// Required validators: min_len, max_len, eq, ne, gt, gte, lt, lte, in.
    /// Optional validators: required.
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; init; }

    /// <summary>
    /// Gets or sets the error message to display when validation fails.
    /// Required field. Must not be null or empty.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; } = string.Empty;

    /// <summary>
    /// Returns the value as a type-safe wrapper for type-safe validation operations.
    /// </summary>
    /// <returns>A <see cref="TypedValue"/> instance wrapping the value with type information.</returns>
    public TypedValue GetTypedValue() => TypedValue.From(Value);
}