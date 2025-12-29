namespace SGuard.ConfigValidation.Results;

/// <summary>
/// Represents the result of a rule engine validation operation.
/// Contains either validation results for one or more files or an error message if the operation failed.
/// </summary>
public sealed class RuleEngineResult
{
    /// <summary>
    /// Gets a value indicating whether the rule engine operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; private init; }
    
    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string ErrorMessage { get; private init; } = string.Empty;
    
    /// <summary>
    /// Gets the exception that occurred during the operation, if any.
    /// </summary>
    public Exception? Exception { get; private init; }
    
    /// <summary>
    /// Gets the list of file validation results when validating multiple environments.
    /// This is populated when <see cref="FileValidationResult"/> is called.
    /// </summary>
    public List<FileValidationResult> ValidationResults { get; private init; } = [];
    
    /// <summary>
    /// Gets the single file validation result when validating a single environment.
    /// This is populated when <see cref="FileValidationResult"/> is called.
    /// </summary>
    public FileValidationResult? SingleResult { get; private init; }

    /// <summary>
    /// Gets the list of critical exceptions that occurred during parallel validation.
    /// These exceptions are captured but do not prevent partial results from being returned.
    /// This allows users to see which environments succeeded even when some failed with critical exceptions.
    /// </summary>
    public IReadOnlyList<Exception> CriticalExceptions { get; private init; } = [];

    /// <summary>
    /// Creates a successful result with multiple file validation results.
    /// </summary>
    /// <param name="results">The list of file validation results.</param>
    /// <returns>A new <see cref="RuleEngineResult"/> instance indicating success with multiple results.</returns>
    public static RuleEngineResult CreateSuccess(List<FileValidationResult> results)
        => new() { IsSuccess = true, ValidationResults = results };

    /// <summary>
    /// Creates a successful result with multiple file validation results and optional critical exceptions.
    /// </summary>
    /// <param name="results">The list of file validation results.</param>
    /// <param name="criticalExceptions">Optional list of critical exceptions that occurred during parallel validation.</param>
    /// <returns>A new <see cref="RuleEngineResult"/> instance indicating success with multiple results and any critical exceptions.</returns>
    public static RuleEngineResult CreateSuccess(List<FileValidationResult> results, IReadOnlyList<Exception>? criticalExceptions)
        => new() { IsSuccess = true, ValidationResults = results, CriticalExceptions = criticalExceptions ?? [] };

    /// <summary>
    /// Creates a successful result with a single file validation result.
    /// </summary>
    /// <param name="result">The single file validation result.</param>
    /// <returns>A new <see cref="RuleEngineResult"/> instance indicating success with a single result.</returns>
    public static RuleEngineResult CreateSuccess(FileValidationResult result)
        => new() { IsSuccess = true, SingleResult = result };

    /// <summary>
    /// Creates an error result with an error message.
    /// </summary>
    /// <param name="message">The error message describing why the operation failed.</param>
    /// <param name="ex">Optional exception that occurred during the operation.</param>
    /// <returns>A new <see cref="RuleEngineResult"/> instance indicating failure.</returns>
    public static RuleEngineResult CreateError(string message, Exception? ex = null)
        => new() { IsSuccess = false, ErrorMessage = message, Exception = ex };

    /// <summary>
    /// Gets a value indicating whether any validation errors were found in the results.
    /// </summary>
    /// <returns><c>true</c> if any file validation result contains errors; otherwise, <c>false</c>.</returns>
    public bool HasValidationErrors => 
        ValidationResults.Any(r => !r.IsValid) || 
        SingleResult is { IsValid: false };
}