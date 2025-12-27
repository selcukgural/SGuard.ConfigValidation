namespace SGuard.ConfigValidation.Results;

/// <summary>
/// Represents the validation result for a single configuration file.
/// Contains all validation results and provides optimized access to errors.
/// </summary>
/// <param name="path">The file path that was validated.</param>
/// <param name="results">The list of validation results for this file.</param>
public sealed class FileValidationResult(string path, List<ValidationResult> results)
{
    /// <summary>
    /// Gets the file path that was validated.
    /// </summary>
    public string Path { get; } = path;
    
    /// <summary>
    /// Gets all validation results for this file (both successful and failed validations).
    /// </summary>
    public List<ValidationResult> Results { get; } = results;

    /// <summary>
    /// Gets a value indicating whether all validations passed for this file.
    /// </summary>
    /// <returns><c>true</c> if all validations passed (ErrorCount == 0); otherwise, <c>false</c>.</returns>
    public bool IsValid =>
        // Early exit optimization: use ErrorCount property
        ErrorCount == 0;

    /// <summary>
    /// Gets the number of validation errors for this file.
    /// </summary>
    /// <returns>The count of validation results that are not valid.</returns>
    public int ErrorCount
    {
        get
        {
            // Count errors with early exit if possible
            var count = 0;
            foreach (var result in Results)
            {
                if (!result.IsValid)
                {
                    count++;
                }
            }
            return count;
        }
    }
    
    /// <summary>
    /// Gets the list of validation errors (failed validations only).
    /// This property uses lazy evaluation with caching for performance optimization.
    /// </summary>
    /// <returns>A list containing only the validation results that are not valid.</returns>
    public List<ValidationResult> Errors
    {
        get
        {
            // Lazy evaluation with caching
            if (field != null)
            {
                return field;
            }

            field = new List<ValidationResult>(Results.Count);
            foreach (var result in Results)
            {
                if (!result.IsValid)
                {
                    field.Add(result);
                }
            }
            return field;
        }
    }
}