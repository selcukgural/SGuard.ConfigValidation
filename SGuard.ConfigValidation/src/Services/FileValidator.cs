using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Service for validating configuration files against rules.
/// </summary>
public sealed class FileValidator : IFileValidator
{
    private readonly IValidatorFactory _validatorFactory;
    private readonly ILogger<FileValidator> _logger;

    public FileValidator(IValidatorFactory validatorFactory, ILogger<FileValidator> logger)
    {
        _validatorFactory = validatorFactory ?? throw new ArgumentNullException(nameof(validatorFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    /// <summary>
    /// Validates a configuration file against the specified rules.
    /// </summary>
    /// <param name="filePath">The path to the file being validated.</param>
    /// <param name="applicableRules">The list of rules to apply to the file.</param>
    /// <param name="appSettings">The app settings dictionary to validate against.</param>
    /// <returns>A <see cref="FileValidationResult"/> containing all validation results for the file.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="applicableRules"/> or <paramref name="appSettings"/> is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ConfigurationException">Thrown when the file cannot be loaded or parsed.</exception>
    public FileValidationResult ValidateFile(
        string filePath, 
        List<Rule> applicableRules,
        Dictionary<string, object> appSettings)
    {
        _logger.LogDebug("Starting file validation for {FilePath} with {RuleCount} rules and {SettingCount} app settings", 
            filePath, applicableRules?.Count ?? 0, appSettings?.Count ?? 0);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogError("File validation failed: File path is null or empty");
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        if (applicableRules == null)
        {
            _logger.LogError("File validation failed: Applicable rules are null");
            throw new ArgumentNullException(nameof(applicableRules), "Applicable rules cannot be null.");
        }

        if (appSettings == null)
        {
            _logger.LogError("File validation failed: AppSettings are null");
            throw new ArgumentNullException(nameof(appSettings), "AppSettings cannot be null.");
        }

        // Pre-allocate list capacity based on estimated number of validations
        // Estimate: average 2-3 validations per rule, 1-2 conditions per rule
        // Use more accurate estimation: count actual validators
        var estimatedCapacity = applicableRules.Sum(r => 
            r?.RuleDetail?.Conditions?.Sum(c => c?.Validators?.Count ?? 0) ?? 0);
        var results = new List<ValidationResult>(Math.Max(estimatedCapacity, applicableRules.Count));

        _logger.LogDebug("Processing {RuleCount} rules for file {FilePath}", applicableRules.Count, filePath);

        foreach (var rule in applicableRules)
        {
            if (rule == null)
            {
                _logger.LogWarning("Encountered null rule during validation");
                results.Add(ValidationResult.Failure("Rule cannot be null.", "system", string.Empty, null));
                continue;
            }

            _logger.LogDebug("Processing rule {RuleId}", rule.Id);

            if (rule.RuleDetail == null)
            {
                _logger.LogWarning("Rule {RuleId} has no rule detail", rule.Id);
                results.Add(ValidationResult.Failure($"Rule '{rule.Id}' has no rule detail.", "system", string.Empty, null));
                continue;
            }

            if (rule.RuleDetail.Conditions == null)
            {
                _logger.LogDebug("Rule {RuleId} has no conditions, skipping", rule.Id);
                continue;
            }

            _logger.LogDebug("Rule {RuleId} has {ConditionCount} conditions", rule.Id, rule.RuleDetail.Conditions.Count);

            foreach (var condition in rule.RuleDetail.Conditions)
            {
                if (condition == null)
                {
                    _logger.LogWarning("Encountered null condition in rule {RuleId}", rule.Id);
                    results.Add(ValidationResult.Failure("Condition cannot be null.", "system", string.Empty, null));
                    continue;
                }

                _logger.LogDebug("Validating condition for key {ConditionKey} in rule {RuleId}", condition.Key, rule.Id);
                var conditionResults = ValidateCondition(condition, appSettings);
                
                var errorCount = conditionResults.Count(r => !r.IsValid);
                if (errorCount > 0)
                {
                    _logger.LogWarning("Condition validation for key {ConditionKey} in rule {RuleId} found {ErrorCount} errors", 
                        condition.Key, rule.Id, errorCount);
                }
                else
                {
                    _logger.LogDebug("Condition validation for key {ConditionKey} in rule {RuleId} passed", 
                        condition.Key, rule.Id);
                }

                results.AddRange(conditionResults);
            }
        }

        var totalErrors = results.Count(r => !r.IsValid);
        var totalPassed = results.Count(r => r.IsValid);

        if (totalErrors > 0)
        {
            _logger.LogWarning("File validation completed for {FilePath}. Passed: {PassedCount}, Errors: {ErrorCount}", 
                filePath, totalPassed, totalErrors);
        }
        else
        {
            _logger.LogInformation("File validation completed successfully for {FilePath}. All {TotalCount} validations passed", 
                filePath, totalPassed);
        }

        return new FileValidationResult(filePath, results);
    }

    private List<ValidationResult> ValidateCondition(
        Condition condition, 
        Dictionary<string, object> appSettings)
    {
        var results = new List<ValidationResult>();
        
        if (string.IsNullOrWhiteSpace(condition.Key))
        {
            _logger.LogWarning("Condition key is null or empty");
            results.Add(ValidationResult.Failure("Condition key cannot be null or empty.", "system", string.Empty, null));
            return results;
        }

        if (condition.Validators.Count == 0)
        {
            _logger.LogWarning("Condition {ConditionKey} has no validators", condition.Key);
            results.Add(ValidationResult.Failure($"Condition '{condition.Key}' has no validators.", "system", condition.Key, null));
            return results;
        }

        // Get the value from appsettings
        var hasValue = appSettings.TryGetValue(condition.Key, out var value);
        
        if (!hasValue)
        {
            _logger.LogDebug("Key {ConditionKey} not found in app settings", condition.Key);
        }
        else
        {
            _logger.LogDebug("Found value for key {ConditionKey}: {Value}", condition.Key, value);
        }

        _logger.LogDebug("Applying {ValidatorCount} validators to condition {ConditionKey}", 
            condition.Validators.Count, condition.Key);

        foreach (var validatorCondition in condition.Validators)
        {
            if (string.IsNullOrWhiteSpace(validatorCondition.Validator))
            {
                _logger.LogWarning("Validator type is null or empty for condition {ConditionKey}", condition.Key);
                results.Add(ValidationResult.Failure("Validator type cannot be null or empty.", "system", condition.Key, value));
                continue;
            }

            _logger.LogDebug("Applying validator {ValidatorType} to key {ConditionKey}", 
                validatorCondition.Validator, condition.Key);

            try
            {
                var validator = _validatorFactory.GetValidator(validatorCondition.Validator);
                
                // Type-based dispatch: Check if value type is supported by validator
                var valueTyped = TypedValue.From(value);
                if (!validator.SupportedValueTypes.Contains(valueTyped.Type) && valueTyped.Type != Common.ValueType.Unknown)
                {
                    _logger.LogWarning("Value type {ValueType} may not be fully supported by validator {ValidatorType} for key {ConditionKey}. Supported types: {SupportedTypes}", 
                        valueTyped.Type, validatorCondition.Validator, condition.Key, string.Join(", ", validator.SupportedValueTypes));
                }
                
                var result = validator.Validate(value, validatorCondition);
                
                // Add key and validator info to result
                if (!result.IsValid)
                {
                    _logger.LogWarning("Validation failed for key {ConditionKey} using validator {ValidatorType}. Message: {Message}", 
                        condition.Key, validatorCondition.Validator, result.Message);
                    result = ValidationResult.Failure(
                        result.Message, 
                        validatorCondition.Validator, 
                        condition.Key, 
                        value);
                }
                else
                {
                    _logger.LogDebug("Validation passed for key {ConditionKey} using validator {ValidatorType}", 
                        condition.Key, validatorCondition.Validator);
                }
                
                results.Add(result);
            }
            catch (NotSupportedException ex)
            {
                _logger.LogError(ex, "Unsupported validator type {ValidatorType} for key {ConditionKey}", 
                    validatorCondition.Validator, condition.Key);
                results.Add(ValidationResult.Failure(
                    $"Unsupported validator type '{validatorCondition.Validator}': {ex.Message}",
                    "system",
                    condition.Key,
                    value,
                    ex));
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument during validation of key {ConditionKey} with validator {ValidatorType}", 
                    condition.Key, validatorCondition.Validator);
                results.Add(ValidationResult.Failure(
                    $"Invalid validation argument: {ex.Message}",
                    validatorCondition.Validator,
                    condition.Key,
                    value,
                    ex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during validation of key {ConditionKey} with validator {ValidatorType}", 
                    condition.Key, validatorCondition.Validator);
                results.Add(ValidationResult.Failure(
                    $"Error during validation: {ex.Message}",
                    validatorCondition.Validator,
                    condition.Key,
                    value,
                    ex));
            }
        }

        return results;
    }
}