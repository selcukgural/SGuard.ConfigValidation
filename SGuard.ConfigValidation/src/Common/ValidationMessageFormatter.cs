using SGuard.ConfigValidation.Resources;

namespace SGuard.ConfigValidation.Common;

/// <summary>
/// Helper class for formatting validation error messages.
/// Provides consistent and readable message formatting for validation errors.
/// </summary>
internal static class ValidationMessageFormatter
{
    /// <summary>
    /// Formats an error message for unsupported validator type errors.
    /// </summary>
    /// <param name="validatorType">The unsupported validator type.</param>
    /// <param name="key">The configuration key that was validated.</param>
    /// <param name="value">The value that was validated.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A formatted error message.</returns>
    internal static string FormatUnsupportedValidatorError(string validatorType, string key, object? value, Exception exception)
    {
        var valueStr = value == null ? "null" : $"'{value}'";
        var message = SR.ResourceManager.GetString(nameof(SR.ValidationMessageFormatter_UnsupportedValidator));
        return string.Format(message ?? "Validation error: Unsupported validator type '{0}'. Configuration key: '{1}'. Value: {2}. Error: {3}. Please use a supported validator type. Check the configuration for valid validator types.",
            validatorType, key, valueStr, exception.Message);
    }

    /// <summary>
    /// Formats an error message for invalid validation argument errors.
    /// </summary>
    /// <param name="key">The configuration key that was validated.</param>
    /// <param name="validatorType">The validator type that caused the error.</param>
    /// <param name="value">The value that was validated.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A formatted error message.</returns>
    internal static string FormatInvalidArgumentError(string key, string validatorType, object? value, Exception exception)
    {
        var valueStr = value == null ? "null" : $"'{value}'";
        var message = SR.ResourceManager.GetString(nameof(SR.ValidationMessageFormatter_InvalidArgument));
        return string.Format(message ?? "Validation error: Invalid validation argument. Configuration key: '{0}'. Validator type: '{1}'. Value: {2}. Error: {3}. Please check the validator configuration and ensure all required properties are provided.",
            key, validatorType, valueStr, exception.Message);
    }

    /// <summary>
    /// Formats an error message for unexpected validation errors.
    /// </summary>
    /// <param name="key">The configuration key that was validated.</param>
    /// <param name="validatorType">The validator type that caused the error.</param>
    /// <param name="value">The value that was validated.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A formatted error message.</returns>
    internal static string FormatUnexpectedValidationError(string key, string validatorType, object? value, Exception exception)
    {
        var valueStr = value == null ? "null" : $"'{value}'";
        var message = SR.ResourceManager.GetString(nameof(SR.ValidationMessageFormatter_UnexpectedError));
        return string.Format(message ?? "Validation error: Unexpected error occurred during validation. Configuration key: '{0}'. Validator type: '{1}'. Value: {2}. Exception type: {3}. Error: {4}. This is an unexpected error. Please check the logs for more details.",
            key, validatorType, valueStr, exception.GetType().Name, exception.Message);
    }

    /// <summary>
    /// Formats an error message for environment validation errors.
    /// </summary>
    /// <param name="environmentId">The environment ID.</param>
    /// <param name="errorType">The type of error (e.g., "invalid path", "failed to load").</param>
    /// <param name="message">The error message.</param>
    /// <returns>A formatted error message.</returns>
    internal static string FormatEnvironmentError(string environmentId, string errorType, string message)
    {
        var template = SR.ResourceManager.GetString(nameof(SR.ValidationMessageFormatter_EnvironmentError));
        return string.Format(template ?? "Environment '{0}' {1}: {2}", environmentId, errorType, message);
    }

    /// <summary>
    /// Formats an error message for value comparison errors (actual vs expected).
    /// </summary>
    /// <param name="baseMessage">The base error message.</param>
    /// <param name="key">The configuration key that was validated.</param>
    /// <param name="actualValue">The actual value that was validated.</param>
    /// <param name="expectedValue">The expected value for comparison.</param>
    /// <returns>A formatted error message.</returns>
    internal static string FormatValueComparisonError(string baseMessage, string key, object? actualValue, object? expectedValue)
    {
        var actualStr = actualValue == null ? "null" : $"'{actualValue}'";
        var expectedStr = expectedValue == null ? "null" : $"'{expectedValue}'";
        var comparisonTemplate = SR.ResourceManager.GetString(nameof(SR.ValidationMessageFormatter_ValueComparison));
        var comparisonPart = string.Format(comparisonTemplate ?? "Actual value: {0}. Expected value: {1}.", actualStr, expectedStr);
        return $"{baseMessage} {comparisonPart}";
    }

    /// <summary>
    /// Formats an error message for file not found errors during environment validation.
    /// </summary>
    /// <param name="environmentId">The environment ID.</param>
    /// <param name="filePath">The file path that was not found.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A formatted error message.</returns>
    internal static string FormatFileNotFoundError(string environmentId, string? filePath, Exception exception)
    {
        var message = SR.ResourceManager.GetString(nameof(SR.ValidationMessageFormatter_FileNotFound));
        return string.Format(message ?? "File not found for environment '{0}': {1}", environmentId, exception.Message);
    }

    /// <summary>
    /// Formats an error message for configuration errors during environment validation.
    /// </summary>
    /// <param name="environmentId">The environment ID.</param>
    /// <param name="message">The error message.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A formatted error message.</returns>
    internal static string FormatConfigurationError(string environmentId, string message, Exception exception)
    {
        var template = SR.ResourceManager.GetString(nameof(SR.ValidationMessageFormatter_ConfigurationError));
        return string.Format(template ?? "Configuration error for environment '{0}': {1}", environmentId, exception.Message);
    }

    /// <summary>
    /// Formats an error message for failed to load or validate environment errors.
    /// </summary>
    /// <param name="environmentId">The environment ID.</param>
    /// <param name="message">The error message.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A formatted error message.</returns>
    internal static string FormatFailedToLoadEnvironmentError(string environmentId, string message, Exception exception)
    {
        var template = SR.ResourceManager.GetString(nameof(SR.ValidationMessageFormatter_FailedToLoadEnvironment));
        return string.Format(template ?? "Failed to load or validate environment '{0}': {1}", environmentId, exception.Message);
    }

    /// <summary>
    /// Formats an error message for validation failures with context information.
    /// </summary>
    /// <param name="baseMessage">The base error message.</param>
    /// <param name="environmentId">Optional environment ID.</param>
    /// <param name="contextDescription">Optional context description.</param>
    /// <param name="additionalInfo">Additional information to append.</param>
    /// <param name="suggestion">Optional suggestion message.</param>
    /// <returns>A formatted error message.</returns>
    internal static string FormatValidationFailureWithContext(
        string baseMessage,
        string? environmentId = null,
        string? contextDescription = null,
        string? additionalInfo = null,
        string? suggestion = null)
    {
        var environmentIdTemplate = SR.ResourceManager.GetString(nameof(SR.ValidationMessageFormatter_Context_EnvironmentId));
        var contextDescTemplate = SR.ResourceManager.GetString(nameof(SR.ValidationMessageFormatter_Context_ContextDesc));
        
        var contextInfo = !string.IsNullOrWhiteSpace(environmentId)
            ? string.Format(environmentIdTemplate ?? "Environment ID: '{0}'. ", environmentId)
            : string.Empty;
        var contextDesc = !string.IsNullOrWhiteSpace(contextDescription)
            ? string.Format(contextDescTemplate ?? "Context: {0}. ", contextDescription)
            : string.Empty;
        var additional = !string.IsNullOrWhiteSpace(additionalInfo)
            ? $"{additionalInfo} "
            : string.Empty;
        var suggestionText = !string.IsNullOrWhiteSpace(suggestion)
            ? suggestion
            : string.Empty;

        return $"{baseMessage} {contextInfo}{contextDesc}{additional}{suggestionText}";
    }
}

