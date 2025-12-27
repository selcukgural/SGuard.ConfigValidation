using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Results;

namespace SGuard.ConfigValidation.Output;

/// <summary>
/// Interface for formatting validation results for output.
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// Formats and outputs the validation results.
    /// </summary>
    /// <param name="result">The rule engine result to format.</param>
    /// <returns>A value task representing the asynchronous operation. Returns a completed task for synchronous operations.</returns>
    ValueTask FormatAsync(RuleEngineResult result);
}

