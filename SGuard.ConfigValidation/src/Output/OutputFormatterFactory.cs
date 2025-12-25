namespace SGuard.ConfigValidation.Output;

/// <summary>
/// Factory for creating output formatters.
/// </summary>
public static class OutputFormatterFactory
{
    /// <summary>
    /// Creates an output formatter based on the specified format.
    /// </summary>
    /// <param name="format">The output format. Supported values: "json" for JSON output, "text" or "console" (or null/empty) for console output. Case-insensitive.</param>
    /// <returns>An <see cref="IOutputFormatter"/> instance for the specified format.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="format"/> is not one of the supported formats: "json", "text", "console", null, or empty string.</exception>
    public static IOutputFormatter Create(string format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        
        return format.ToLowerInvariant() switch
        {
            "json" => new JsonOutputFormatter(),
            "text" or "console" or null or "" => new ConsoleOutputFormatter(),
            _ => throw new ArgumentException($"Unknown output format: {format}. Supported formats: json, text, console", nameof(format))
        };
    }
}

