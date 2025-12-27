using Microsoft.Extensions.Logging;
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
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="format"/> is not one of the supported formats: "json", "text", "console", null, or empty string.</exception>
    /// <example>
    /// <code>
    /// var formatter = OutputFormatterFactory.Create("json", loggerFactory);
    /// await formatter.FormatAsync(result);
    /// </code>
    /// </example>
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

    /// <summary>
    /// Creates an output formatter based on the specified format and output file path.
    /// If a file path is provided, the formatter will write to the file instead of the console.
    /// </summary>
    /// <param name="format">The output format. Supported values: "json" for JSON output, "text" or "console" (or null/empty) for text output. Case-insensitive.</param>
    /// <param name="loggerFactory">The logger factory for creating loggers for output formatters. Only used for console output.</param>
    /// <param name="filePath">The path to the output file. If provided, the formatter will write to this file instead of the console. If null or empty, writes to console.</param>
    /// <returns>An <see cref="IOutputFormatter"/> instance for the specified format and target.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="format"/> is not one of the supported formats: "json", "text", "console", null, or empty string.</exception>
    /// <example>
    /// <code>
    /// // Write JSON to file
    /// var jsonFormatter = OutputFormatterFactory.Create("json", loggerFactory, "results.json");
    /// await jsonFormatter.FormatAsync(result);
    /// 
    /// // Write text to file
    /// var textFormatter = OutputFormatterFactory.Create("text", loggerFactory, "results.txt");
    /// await textFormatter.FormatAsync(result);
    /// 
    /// // Write to console (filePath is null)
    /// var consoleFormatter = OutputFormatterFactory.Create("json", loggerFactory, null);
    /// await consoleFormatter.FormatAsync(result);
    /// </code>
    /// </example>
    public static IOutputFormatter Create(string format, ILoggerFactory loggerFactory, string? filePath)
    {
        System.ArgumentException.ThrowIfNullOrWhiteSpace(format);
        System.ArgumentNullException.ThrowIfNull(loggerFactory);
        
        // If file path is provided, return file formatters
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            var normalizedFormat = format.ToLowerInvariant();
            if (normalizedFormat == "json")
            {
                return new JsonFileOutputFormatter(filePath);
            }
            
            if (normalizedFormat == "text" || normalizedFormat == "console" || string.IsNullOrEmpty(normalizedFormat))
            {
                return new FileOutputFormatter(filePath);
            }
            
            throw ArgumentException(nameof(SR.ArgumentException_UnknownOutputFormat), nameof(format), format);
        }
        
        // If no file path, use console formatters (existing behavior)
        return Create(format, loggerFactory);
    }
}

