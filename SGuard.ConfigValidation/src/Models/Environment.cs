using System.Text.Json.Serialization;

namespace SGuard.ConfigValidation.Models;

/// <summary>
/// Represents a configuration environment (e.g., Development, Staging, Production).
/// Note: This class is in the SGuard.ConfigValidation.Models namespace and is distinct from System.Environment.
/// </summary>
public sealed class Environment
{
    /// <summary>
    /// Gets the unique identifier for the environment.
    /// Required field. Must not be null or empty. Must be unique across all environments.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display name of the environment.
    /// Required field. Must not be null or empty.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the file path to the appsettings file for this environment.
    /// Required field. Must not be null or empty.
    /// Note: This property represents a file path string and is distinct from System.IO.Path.
    /// </summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; } 

    /// <summary>
    /// Gets or sets the optional description of the environment.
    /// Optional field. Can be null or empty.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}