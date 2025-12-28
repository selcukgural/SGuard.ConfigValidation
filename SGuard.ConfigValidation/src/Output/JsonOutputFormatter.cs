using System.Text.Json;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Results;

namespace SGuard.ConfigValidation.Output;

/// <summary>
/// JSON output formatter that outputs validation results as JSON.
/// Uses an indented JSON format for better readability.
/// </summary>
public sealed class JsonOutputFormatter : IOutputFormatter
{
    /// <summary>
    /// Formats and outputs the validation results as JSON to the console.
    /// </summary>
    /// <param name="result">The rule engine result to format and output.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation. Always returns a completed task as this operation is synchronous.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    public ValueTask FormatAsync(RuleEngineResult result)
    {
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
        Console.WriteLine(json);
        
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

