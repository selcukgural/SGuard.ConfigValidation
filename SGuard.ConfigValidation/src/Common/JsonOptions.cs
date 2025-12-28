using System.Text.Json;
using SGuard.ConfigValidation.Security;

namespace SGuard.ConfigValidation.Common;

/// <summary>
/// Shared JSON serializer/deserializer options for performance optimization.
/// These options are immutable and can be safely reused across the application.
/// </summary>
public static class JsonOptions
{
    /// <summary>
    /// Options for deserializing configuration files.
    /// WriteIndented is false for better performance (not needed for deserialization).
    /// MaxDepth is set to prevent stack overflow attacks through deeply nested JSON.
    /// </summary>
    public static readonly JsonSerializerOptions Deserialization = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false, // Performance: no indentation needed for deserialization
        MaxDepth = SecurityConstants.MaxJsonDepth, // Prevent stack overflow attacks
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles // Prevent circular reference DoS
    };

    /// <summary>
    /// Options for serializing JSON for user output (formatted, readable).
    /// WriteIndented is true for better readability in console/file output.
    /// </summary>
    public static readonly JsonSerializerOptions Output = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Options for internal serialization (no indentation for performance).
    /// Used when JSON is used internally and not displayed to users.
    /// </summary>
    public static readonly JsonSerializerOptions Internal = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false, // Performance: no indentation for internal use
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Options for parsing JSON documents (reading only, no serialization).
    /// Optimized for performance with reasonable limits.
    /// </summary>
    public static readonly JsonDocumentOptions Document = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
        MaxDepth = 128 // Reasonable limit for nested JSON
    };
}

