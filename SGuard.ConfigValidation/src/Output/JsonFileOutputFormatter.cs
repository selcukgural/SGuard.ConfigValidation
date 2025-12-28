using System.Text;
using System.Text.Json;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Results;
using static SGuard.ConfigValidation.Common.Throw;

namespace SGuard.ConfigValidation.Output;

/// <summary>
/// JSON file output formatter that writes validation results to a file in JSON format.
/// Uses an indented JSON format for better readability.
/// </summary>
public sealed class JsonFileOutputFormatter : IOutputFormatter
{
    private readonly string _filePath;

    /// <summary>
    /// Initializes a new instance of the JsonFileOutputFormatter class.
    /// </summary>
    /// <param name="filePath">The path to the output file where validation results will be written as JSON.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
    public JsonFileOutputFormatter(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw ArgumentException(nameof(Resources.SR.ArgumentException_FilePathNullOrEmpty), nameof(filePath));
        }

        _filePath = filePath;
    }

    /// <summary>
    /// Formats and writes the validation results to a file as JSON.
    /// </summary>
    /// <param name="result">The rule engine result to format and write.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation. Always returns a completed task as this operation is synchronous.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    /// <exception cref="System.IO.IOException">Thrown when an I/O error occurs while writing to the file.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when access to the file is denied.</exception>
    /// <example>
    /// <code>
    /// var formatter = new JsonFileOutputFormatter("validation-results.json");
    /// await formatter.FormatAsync(ruleEngineResult);
    /// </code>
    /// </example>
    public ValueTask FormatAsync(RuleEngineResult result)
    {
        System.ArgumentNullException.ThrowIfNull(result);

        // Optimized: Direct array initialization instead of Select().ToArray()
        object[] results;
        if (result.SingleResult != null)
        {
            results = [FormatFileResult(result.SingleResult)];
        }
        else
        {
            var validationResults = result.ValidationResults;
            results = new object[validationResults.Count];
            for (var i = 0; i < validationResults.Count; i++)
            {
                results[i] = FormatFileResult(validationResults[i]);
            }
        }

        var output = new
        {
            Success = result.IsSuccess,
            result.ErrorMessage,
            result.HasValidationErrors,
            Results = results
        };

        var json = JsonSerializer.Serialize(output, JsonOptions.Output);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, json, Encoding.UTF8);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Formats a file validation result into an anonymous object for JSON serialization.
    /// </summary>
    /// <param name="fileResult">The file validation result to format.</param>
    /// <returns>An anonymous object containing the formatted file validation result with path, validation status, results, and errors.</returns>
    private static object FormatFileResult(FileValidationResult fileResult)
    {
        var results = fileResult.Results;
        var resultsArray = new object[results.Count];
        
        for (var i = 0; i < results.Count; i++)
        {
            var r = results[i];
            resultsArray[i] = new
            {
                r.IsValid,
                r.Message,
                r.ValidatorType,
                r.Key,
                Value = r.Value?.ToString()
            };
        }

        var errors = fileResult.Errors;
        var errorsArray = new object[errors.Count];
        
        for (var i = 0; i < errors.Count; i++)
        {
            var e = errors[i];
            errorsArray[i] = new
            {
                e.Message,
                e.ValidatorType,
                e.Key,
                Value = e.Value?.ToString()
            };
        }

        return new
        {
            fileResult.Path,
            fileResult.IsValid,
            fileResult.ErrorCount,
            Results = resultsArray,
            Errors = errorsArray
        };
    }
}

