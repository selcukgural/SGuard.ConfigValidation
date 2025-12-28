using System.Text;
using SGuard.ConfigValidation.Resources;
using SGuard.ConfigValidation.Results;
using static SGuard.ConfigValidation.Common.Throw;

namespace SGuard.ConfigValidation.Output;

/// <summary>
/// File output formatter that writes validation results to a file in text format.
/// Provides human-readable output with visual indicators for validation status.
/// </summary>
public sealed class FileOutputFormatter : IOutputFormatter
{
    private readonly string _filePath;

    /// <summary>
    /// Initializes a new instance of the FileOutputFormatter class.
    /// </summary>
    /// <param name="filePath">The path to the output file where validation results will be written.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
    public FileOutputFormatter(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw ArgumentException(nameof(SR.ArgumentException_FilePathNullOrEmpty), nameof(filePath));
        }

        _filePath = filePath;
    }

    /// <summary>
    /// Formats and writes the validation results to a file with formatted text.
    /// </summary>
    /// <param name="result">The rule engine result to format and write.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation. Always returns a completed task as this operation is synchronous.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    /// <exception cref="System.IO.IOException">Thrown when an I/O error occurs while writing to the file.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when access to the file is denied.</exception>
    /// <example>
    /// <code>
    /// var formatter = new FileOutputFormatter("validation-results.txt");
    /// await formatter.FormatAsync(ruleEngineResult);
    /// </code>
    /// </example>
    public ValueTask FormatAsync(RuleEngineResult result)
    {
        System.ArgumentNullException.ThrowIfNull(result);

        var output = new StringBuilder();

        if (!result.IsSuccess)
        {
            output.AppendLine(string.Format(SR.ConsoleOutput_Error, result.ErrorMessage));
            if (result.Exception != null)
            {
                output.AppendLine(string.Format(SR.ConsoleOutput_ErrorDetails, result.Exception.Message));
            }
        }
        else
        {
            if (result.SingleResult != null)
            {
                FormatFileResult(result.SingleResult, output);
            }
            else if (result.ValidationResults.Count != 0)
            {
                output.AppendLine(SR.ConsoleOutput_ValidatingEnvironments);
                output.AppendLine();

                foreach (var fileResult in result.ValidationResults)
                {
                    FormatFileResult(fileResult, output);
                    output.AppendLine();
                }
            }

            var totalErrors = result.ValidationResults.Sum(r => r.ErrorCount) +
                             (result.SingleResult?.ErrorCount ?? 0);

            output.AppendLine(totalErrors == 0
                                  ? SR.ConsoleOutput_AllValidationsPassed
                                  : string.Format(SR.ConsoleOutput_ValidationErrorsFound, totalErrors));
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, output.ToString(), Encoding.UTF8);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Formats a single file validation result and appends it to the output string builder.
    /// </summary>
    /// <param name="fileResult">The file validation result to format.</param>
    /// <param name="output">The string builder to append the formatted result to.</param>
    private static void FormatFileResult(FileValidationResult fileResult, StringBuilder output)
    {
        var environmentName = Path.GetFileNameWithoutExtension(fileResult.Path);
        
        output.AppendLine(string.Format(SR.ConsoleOutput_Environment, environmentName));
        output.AppendLine(string.Format(SR.ConsoleOutput_File, fileResult.Path));

        output.AppendLine(fileResult.IsValid ? SR.ConsoleOutput_StatusPass : SR.ConsoleOutput_StatusFail);

        if (!fileResult.IsValid)
        {
            output.AppendLine(string.Format(SR.ConsoleOutput_Errors, fileResult.ErrorCount));

            foreach (var validationResult in fileResult.Errors)
            {
                output.AppendLine(string.Format(SR.ConsoleOutput_Key, validationResult.Key));
                output.AppendLine(string.Format(SR.ConsoleOutput_ValidatorType, validationResult.ValidatorType, validationResult.Message));
                if (validationResult.Value != null)
                {
                    output.AppendLine(string.Format(SR.ConsoleOutput_CurrentValue, validationResult.Value));
                }
            }
        }
        else
        {
            output.AppendLine(string.Format(SR.ConsoleOutput_ValidatedRules, fileResult.Results.Count));
        }
    }
}

