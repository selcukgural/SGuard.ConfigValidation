using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Resources;
using SGuard.ConfigValidation.Results;

namespace SGuard.ConfigValidation.Output;

/// <summary>
/// Console output formatter that displays validation results with formatted text.
/// Provides human-readable output with visual indicators for validation status.
/// </summary>
public sealed class ConsoleOutputFormatter : IOutputFormatter
{
    private readonly ILogger<ConsoleOutputFormatter> _logger;

    /// <summary>
    /// Initializes a new instance of the ConsoleOutputFormatter class.
    /// </summary>
    /// <param name="logger">The logger instance for outputting validation results.</param>
    public ConsoleOutputFormatter(ILogger<ConsoleOutputFormatter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        
        _logger = logger;
    }

    /// <summary>
    /// Formats and outputs the validation results to the console with formatted text.
    /// </summary>
    /// <param name="result">The rule engine result to format and output.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation. Always returns a completed task as this operation is synchronous.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    public ValueTask FormatAsync(RuleEngineResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.IsSuccess)
        {
            _logger.LogError(SR.ConsoleOutput_Error, result.ErrorMessage);
            if (result.Exception != null)
            {
                _logger.LogError(SR.ConsoleOutput_ErrorDetails, result.Exception.Message);
            }
            return ValueTask.CompletedTask;
        }

        if (result.SingleResult != null)
        {
            FormatFileResult(result.SingleResult);
        }
        else if (result.ValidationResults.Count != 0)
        {
            _logger.LogInformation(SR.ConsoleOutput_ValidatingEnvironments);
            _logger.LogInformation(string.Empty); // Add spacing
            
            foreach (var fileResult in result.ValidationResults)
            {
                FormatFileResult(fileResult);
                _logger.LogInformation(string.Empty); // Add spacing between environments
            }
        }

        var totalErrors = result.ValidationResults.Sum(r => r.ErrorCount) + 
                         (result.SingleResult?.ErrorCount ?? 0);

        if (totalErrors == 0)
        {
            _logger.LogInformation(SR.ConsoleOutput_AllValidationsPassed);
        }
        else
        {
            _logger.LogError(SR.ConsoleOutput_ValidationErrorsFound, totalErrors);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Formats and outputs a single file validation result to the console.
    /// </summary>
    /// <param name="fileResult">The file validation result to format and output.</param>
    private void FormatFileResult(FileValidationResult fileResult)
    {
        var environmentName = Path.GetFileNameWithoutExtension(fileResult.Path);
        
        _logger.LogInformation(SR.ConsoleOutput_Environment, environmentName);
        _logger.LogInformation(SR.ConsoleOutput_File, fileResult.Path);
        
        if (fileResult.IsValid)
        {
            _logger.LogInformation(SR.ConsoleOutput_StatusPass);
        }
        else
        {
            _logger.LogError(SR.ConsoleOutput_StatusFail);
        }
        
        if (!fileResult.IsValid)
        {
            _logger.LogError(SR.ConsoleOutput_Errors, fileResult.ErrorCount);
            
            foreach (var validationResult in fileResult.Errors)
            {
                _logger.LogError(SR.ConsoleOutput_Key, validationResult.Key);
                _logger.LogError(SR.ConsoleOutput_ValidatorType, validationResult.ValidatorType, validationResult.Message);
                
                if (validationResult.Value != null)
                {
                    _logger.LogError(SR.ConsoleOutput_CurrentValue, validationResult.Value);
                }
            }
        }
        else
        {
            _logger.LogInformation(SR.ConsoleOutput_ValidatedRules, fileResult.Results.Count);
        }
    }
}
