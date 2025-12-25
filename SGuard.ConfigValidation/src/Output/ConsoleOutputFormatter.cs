using SGuard.ConfigValidation.Common;

namespace SGuard.ConfigValidation.Output;

/// <summary>
/// Console output formatter that displays validation results with emojis and formatted text.
/// Provides human-readable output with visual indicators for validation status.
/// </summary>
public sealed class ConsoleOutputFormatter : IOutputFormatter
{
    /// <summary>
    /// Formats and outputs the validation results to the console with emojis and formatted text.
    /// </summary>
    /// <param name="result">The rule engine result to format and output.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation. Always returns a completed task as this operation is synchronous.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    public ValueTask FormatAsync(RuleEngineResult result)
    {
        if (!result.IsSuccess)
        {
            Console.WriteLine($"❌ Error: {result.ErrorMessage}");
            if (result.Exception != null)
            {
                Console.WriteLine($"   Details: {result.Exception.Message}");
            }
            return ValueTask.CompletedTask;
        }

        if (result.SingleResult != null)
        {
            FormatFileResult(result.SingleResult);
        }
        else if (result.ValidationResults.Count != 0)
        {
            Console.WriteLine("🔍 Validating Environments:\n");
            
            foreach (var fileResult in result.ValidationResults)
            {
                FormatFileResult(fileResult);
                Console.WriteLine(); // Add spacing between environments
            }
        }

        var totalErrors = result.ValidationResults.Sum(r => r.ErrorCount) + 
                         (result.SingleResult?.ErrorCount ?? 0);

        Console.WriteLine(totalErrors == 0 ? "✅ All validations passed successfully!" : $"❌ {totalErrors} validation error(s) found.");

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Formats and outputs a single file validation result to the console.
    /// </summary>
    /// <param name="fileResult">The file validation result to format and output.</param>
    private static void FormatFileResult(FileValidationResult fileResult)
    {
        Console.WriteLine($"📁 Environment: {Path.GetFileNameWithoutExtension(fileResult.Path)}");
        Console.WriteLine($"   File: {fileResult.Path}");
        Console.WriteLine($"   Status: {(fileResult.IsValid ? "✅ PASS" : "❌ FAIL")}");
        
        if (!fileResult.IsValid)
        {
            Console.WriteLine($"   Errors ({fileResult.ErrorCount}):");
            
            foreach (var validationResult in fileResult.Errors)
            {
                Console.WriteLine($"     🔑 {validationResult.Key}");
                Console.WriteLine($"        ✖ {validationResult.ValidatorType}: {validationResult.Message}");
                if (validationResult.Value != null)
                {
                    Console.WriteLine($"        💡 Current value: {validationResult.Value}");
                }
            }
        }
        else
        {
            Console.WriteLine($"   Validated {fileResult.Results.Count} rule(s)");
        }
    }
}

