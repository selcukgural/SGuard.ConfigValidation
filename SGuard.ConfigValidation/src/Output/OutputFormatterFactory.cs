using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Resources;
using static SGuard.ConfigValidation.Common.Throw;

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
    /// <param name="loggerFactory">The logger factory for creating loggers for output formatters.</param>
    /// <returns>An <see cref="IOutputFormatter"/> instance for the specified format.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="format"/> is not one of the supported formats: "json", "text", "console", null, or empty string.</exception>
    public static IOutputFormatter Create(string format, ILoggerFactory loggerFactory)
    {
        System.ArgumentException.ThrowIfNullOrWhiteSpace(format);
        System.ArgumentNullException.ThrowIfNull(loggerFactory);
        
        var normalizedFormat = format.ToLowerInvariant();
        if (normalizedFormat == "json")
        {
            return new JsonOutputFormatter();
        }
        
        if (normalizedFormat == "text" || normalizedFormat == "console" || string.IsNullOrEmpty(normalizedFormat))
        {
            return new ConsoleOutputFormatter(loggerFactory.CreateLogger<ConsoleOutputFormatter>());
        }
        
        throw ArgumentException(nameof(SR.ArgumentException_UnknownOutputFormat), nameof(format), format);
    }
}

