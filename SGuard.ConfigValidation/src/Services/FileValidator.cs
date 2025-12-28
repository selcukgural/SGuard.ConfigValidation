using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Resources;
using SGuard.ConfigValidation.Results;
using SGuard.ConfigValidation.Services.Abstract;
using SGuard.ConfigValidation.Validators;
using static SGuard.ConfigValidation.Common.Throw;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Service for validating configuration files against rules.
/// </summary>
public sealed class FileValidator : IFileValidator
{
    private readonly IValidatorFactory _validatorFactory;
    private readonly ILogger<FileValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the FileValidator class.
    /// </summary>
    /// <param name="validatorFactory">The validator factory used to create validators for validation rules.</param>
    /// <param name="logger">Logger instance for logging validation operations.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="validatorFactory"/> or <paramref name="logger"/> is null.</exception>
    public FileValidator(IValidatorFactory validatorFactory, ILogger<FileValidator> logger)
    {
        System.ArgumentNullException.ThrowIfNull(validatorFactory);
        System.ArgumentNullException.ThrowIfNull(logger);
        _validatorFactory = validatorFactory;
        _logger = logger;
    }

    /// <summary>
    /// Validates a configuration file against the specified rules.
    /// </summary>
    /// <param name="filePath">The path to the file being validated.</param>
    /// <param name="applicableRules">The list of rules to apply to the file.</param>
    /// <param name="appSettings">The app settings dictionary to validate against.</param>
    /// <returns>A <see cref="FileValidationResult"/> containing all validation results for the file.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="applicableRules"/> or <paramref name="appSettings"/> is null.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="SGuard.ConfigValidation.Exceptions.ConfigurationException">Thrown when the file cannot be loaded or parsed.</exception>
    /// <example>
    /// <code>
    /// using Microsoft.Extensions.Logging;
    /// using Microsoft.Extensions.Logging.Abstractions;
    /// using SGuard.ConfigValidation.Models;
    /// using SGuard.ConfigValidation.Services;
    /// using SGuard.ConfigValidation.Validators;
    /// 
    /// var validatorFactory = new ValidatorFactory(NullLogger&lt;ValidatorFactory&gt;.Instance);
    /// var fileValidator = new FileValidator(validatorFactory, NullLogger&lt;FileValidator&gt;.Instance);
    /// 
    /// var rules = new List&lt;Rule&gt;
    /// {
    ///     new Rule
    ///     {
    ///         Id = "connection-string-rule",
    ///         Environments = new List&lt;string&gt; { "prod" },
    ///         RuleDetail = new RuleDetail
    ///         {
    ///             Id = "connection-string-detail",
    ///             Conditions = new List&lt;Condition&gt;
    ///             {
    ///                 new Condition
    ///                 {
    ///                     Key = "ConnectionStrings:DefaultConnection",
    ///                     Validators = new List&lt;ValidatorCondition&gt;
    ///                     {
    ///                         new ValidatorCondition
    ///                         {
    ///                             Validator = "required",
    ///                             Message = "Connection string is required"
    ///                         }
    ///                     }
    ///                 }
    ///             }
    ///         }
    ///     }
    /// };
    /// 
    /// var appSettings = new Dictionary&lt;string, object&gt;
    /// {
    ///     { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=MyDb" }
    /// };
    /// 
    /// var result = fileValidator.ValidateFile("appsettings.Production.json", rules, appSettings);
    /// 
    /// if (result.IsValid)
    /// {
    ///     Console.WriteLine("File validation passed!");
    /// }
    /// else
    /// {
    ///     foreach (var validationResult in result.Results)
    ///     {
    ///         foreach (var error in validationResult.Errors)
    ///         {
    ///             Console.WriteLine($"  - {error.Key}: {error.Message}");
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public FileValidationResult ValidateFile(string filePath, List<Rule> applicableRules, Dictionary<string, object>? appSettings)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogError("File validation failed: File path is null or empty");
            throw ArgumentException(nameof(SR.ArgumentException_FilePathRequired), nameof(filePath));
        }

        System.ArgumentNullException.ThrowIfNull(applicableRules);

        if (applicableRules.Count == 0)
        {
            _logger.LogError("File validation failed: Applicable rules list is empty. File path: {FilePath}", filePath);
            throw ArgumentException(nameof(SR.ArgumentNullException_ApplicableRules), nameof(applicableRules), filePath);
        }

        System.ArgumentNullException.ThrowIfNull(appSettings);

        // Optimized: Single loop for both validation and capacity estimation
        // Start with reasonable initial capacity, let List grow dynamically if needed
        // This avoids the overhead of pre-calculating capacity with a separate loop
        var results = new List<ValidationResult>();

        foreach (var rule in applicableRules)
        {
            if (rule == null)
            {
                _logger.LogWarning("Encountered null rule during validation of file {FilePath}", filePath);

                results.Add(ValidationResult.Failure(
                                $"Validation error: Rule cannot be null. " + $"File: '{filePath}'. " +
                                "Please ensure all rules in the configuration are properly defined.", "system", string.Empty, null));

                continue;
            }

            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                _logger.LogWarning("Encountered rule with null or empty ID during validation of file {FilePath}", filePath);

                results.Add(ValidationResult.Failure(
                                $"Validation error: Rule ID cannot be null or empty. " + $"File: '{filePath}'. " +
                                "Please ensure all rules have a valid ID.", "system", string.Empty, null));

                continue;
            }

            if (string.IsNullOrWhiteSpace(rule.RuleDetail.Id))
            {
                _logger.LogWarning("Rule {RuleId} has no rule detail ID. File: {FilePath}", rule.Id, filePath);

                results.Add(ValidationResult.Failure(
                                $"Validation error: Rule '{rule.Id}' has no rule detail ID. " + $"File: '{filePath}'. " +
                                "The rule detail must have an 'id' property.", "system", string.Empty, null));

                continue;
            }

            if (rule.RuleDetail.Conditions.Count == 0)
            {
                continue;
            }

            foreach (var condition in rule.RuleDetail.Conditions)
            {
                var conditionResults = ValidateCondition(condition, appSettings);

                // Count errors without enumeration (use single loop)
                var errorCount = 0;

                foreach (var cr in conditionResults)
                {
                    if (!cr.IsValid)
                    {
                        errorCount++;
                    }
                }

                if (errorCount > 0)
                {
                    _logger.LogWarning("Condition validation for key {ConditionKey} in rule {RuleId} found {ErrorCount} errors", condition.Key,
                                       rule.Id, errorCount);
                }

                results.AddRange(conditionResults);
            }
        }

        // Create result object to use ErrorCount property (avoids multiple enumeration)
        var result = new FileValidationResult(filePath, results);
        var totalErrors = result.ErrorCount;
        var totalPassed = result.Results.Count - totalErrors;

        if (totalErrors > 0)
        {
            _logger.LogWarning("File validation completed for {FilePath}. Passed: {PassedCount}, Errors: {ErrorCount}", filePath, totalPassed,
                               totalErrors);
        }
        else
        {
            _logger.LogInformation("File validation completed successfully for {FilePath}. All {TotalCount} validations passed", filePath,
                                   totalPassed);
        }

        return result;
    }

    private List<ValidationResult> ValidateCondition(Condition condition, Dictionary<string, object> appSettings)
    {
        var results = new List<ValidationResult>();

        if (string.IsNullOrWhiteSpace(condition.Key))
        {
            _logger.LogWarning("Condition key is null or empty during validation");

            results.Add(ValidationResult.Failure(
                            "Validation error: Condition key is required but was null or empty. " +
                            "Each condition must have a non-empty 'key' property specifying the configuration key to validate.", "system",
                            string.Empty, null));
            return results;
        }

        if (condition.Validators.Count == 0)
        {
            _logger.LogWarning("Condition {ConditionKey} has no validators", condition.Key);

            results.Add(ValidationResult.Failure(
                            $"Validation error: Condition for key '{condition.Key}' has no validators. " +
                            "Each condition must have at least one validator in the 'condition' array to perform validation.", "system",
                            condition.Key, null));
            return results;
        }

        // Get the value from app settings
        appSettings.TryGetValue(condition.Key, out var value);

        foreach (var validatorCondition in condition.Validators)
        {
            if (string.IsNullOrWhiteSpace(validatorCondition.Validator))
            {
                _logger.LogWarning("Validator type is null or empty for condition {ConditionKey}", condition.Key);

                results.Add(ValidationResult.Failure(
                                $"Validation error: Validator type is required but was null or empty. " + $"Configuration key: '{condition.Key}'. " +
                                $"Value found: {(value == null ? "null" : $"'{value}'")}. " +
                                "Each validator must have a non-empty 'validator' property specifying the validator type.", "system", condition.Key,
                                value));
                continue;
            }

            try
            {
                var validator = _validatorFactory.GetValidator(validatorCondition.Validator);

                // Type-based dispatch: Check if the validator supports a value type
                var valueTyped = TypedValue.From(value);

                if (!validator.SupportedValueTypes.Contains(valueTyped.Type) && valueTyped.Type != Common.ValueType.Unknown)
                {
                    _logger.LogWarning(
                        "Value type {ValueType} may not be fully supported by validator {ValidatorType} for key {ConditionKey}. Supported types: {SupportedTypes}",
                        valueTyped.Type, validatorCondition.Validator, condition.Key, string.Join(", ", validator.SupportedValueTypes));
                }

                var result = validator.Validate(value, validatorCondition);

                // Add key and validator info to the result
                if (!result.IsValid)
                {
                    _logger.LogWarning("Validation failed for key {ConditionKey} using validator {ValidatorType}. Message: {Message}", condition.Key,
                                       validatorCondition.Validator, result.Message);
                    result = ValidationResult.Failure(result.Message, validatorCondition.Validator, condition.Key, value);
                }

                results.Add(result);
            }
            catch (NotSupportedException ex)
            {
                _logger.LogError(ex, "Unsupported validator type {ValidatorType} for key {ConditionKey}. Value: {Value}",
                                 validatorCondition.Validator, condition.Key, value);

                results.Add(ValidationResult.Failure(
                                ValidationMessageFormatter.FormatUnsupportedValidatorError(validatorCondition.Validator, condition.Key, value, ex),
                                "system", condition.Key, value, ex));
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument during validation of key {ConditionKey} with validator {ValidatorType}. Value: {Value}",
                                 condition.Key, validatorCondition.Validator, value);

                results.Add(ValidationResult.Failure(
                                ValidationMessageFormatter.FormatInvalidArgumentError(condition.Key, validatorCondition.Validator, value, ex),
                                validatorCondition.Validator, condition.Key, value, ex));
            }
            catch (Exception ex)
            {
                // Re-throw critical exceptions immediately - they indicate severe system problems
                if (Throw.IsCriticalException(ex))
                {
                    throw;
                }

                _logger.LogError(
                    ex,
                    "Unexpected error during validation of key {ConditionKey} with validator {ValidatorType}. Exception type: {ExceptionType}, Value: {Value}",
                    condition.Key, validatorCondition.Validator, ex.GetType().Name, value);

                results.Add(ValidationResult.Failure(
                                ValidationMessageFormatter.FormatUnexpectedValidationError(condition.Key, validatorCondition.Validator, value, ex),
                                validatorCondition.Validator, condition.Key, value, ex));
            }
        }

        return results;
    }
}