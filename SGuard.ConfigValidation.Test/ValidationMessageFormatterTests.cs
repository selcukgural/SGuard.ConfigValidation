using FluentAssertions;
using SGuard.ConfigValidation.Common;

namespace SGuard.ConfigValidation.Test;

public sealed class ValidationMessageFormatterTests
{
    [Fact]
    public void FormatUnsupportedValidatorError_Should_Format_Message()
    {
        // Arrange
        var validatorType = "unsupported_validator";
        var key = "test.key";
        var value = "test-value";
        var exception = new Exception("Test exception");

        // Act
        var result = ValidationMessageFormatter.FormatUnsupportedValidatorError(validatorType, key, value, exception);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(validatorType);
        result.Should().Contain(key);
        result.Should().Contain(value);
        result.Should().Contain(exception.Message);
    }

    [Fact]
    public void FormatUnsupportedValidatorError_With_NullValue_Should_Format_Message()
    {
        // Arrange
        var validatorType = "unsupported_validator";
        var key = "test.key";
        object? value = null;
        var exception = new Exception("Test exception");

        // Act
        var result = ValidationMessageFormatter.FormatUnsupportedValidatorError(validatorType, key, value, exception);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(validatorType);
        result.Should().Contain(key);
        result.Should().Contain("null");
        result.Should().Contain(exception.Message);
    }

    [Fact]
    public void FormatInvalidArgumentError_Should_Format_Message()
    {
        // Arrange
        var key = "test.key";
        var validatorType = "min_len";
        var value = "test-value";
        var exception = new Exception("Test exception");

        // Act
        var result = ValidationMessageFormatter.FormatInvalidArgumentError(key, validatorType, value, exception);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(key);
        result.Should().Contain(validatorType);
        result.Should().Contain(value);
        result.Should().Contain(exception.Message);
    }

    [Fact]
    public void FormatInvalidArgumentError_With_NullValue_Should_Format_Message()
    {
        // Arrange
        var key = "test.key";
        var validatorType = "min_len";
        object? value = null;
        var exception = new Exception("Test exception");

        // Act
        var result = ValidationMessageFormatter.FormatInvalidArgumentError(key, validatorType, value, exception);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(key);
        result.Should().Contain(validatorType);
        result.Should().Contain("null");
        result.Should().Contain(exception.Message);
    }

    [Fact]
    public void FormatUnexpectedValidationError_Should_Format_Message()
    {
        // Arrange
        var key = "test.key";
        var validatorType = "required";
        var value = "test-value";
        var exception = new InvalidOperationException("Unexpected error");

        // Act
        var result = ValidationMessageFormatter.FormatUnexpectedValidationError(key, validatorType, value, exception);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(key);
        result.Should().Contain(validatorType);
        result.Should().Contain(value);
        result.Should().Contain(exception.GetType().Name);
        result.Should().Contain(exception.Message);
    }

    [Fact]
    public void FormatUnexpectedValidationError_With_NullValue_Should_Format_Message()
    {
        // Arrange
        var key = "test.key";
        var validatorType = "required";
        object? value = null;
        var exception = new InvalidOperationException("Unexpected error");

        // Act
        var result = ValidationMessageFormatter.FormatUnexpectedValidationError(key, validatorType, value, exception);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(key);
        result.Should().Contain(validatorType);
        result.Should().Contain("null");
        result.Should().Contain(exception.GetType().Name);
    }

    [Fact]
    public void FormatEnvironmentError_Should_Format_Message()
    {
        // Arrange
        var environmentId = "production";
        var errorType = "invalid path";
        var message = "File not found";

        // Act
        var result = ValidationMessageFormatter.FormatEnvironmentError(environmentId, errorType, message);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(environmentId);
        result.Should().Contain(errorType);
        result.Should().Contain(message);
    }

    [Fact]
    public void FormatValueComparisonError_With_NonNullValues_Should_Format_Message()
    {
        // Arrange
        var baseMessage = "Validation failed";
        var key = "test.key";
        var actualValue = "actual";
        var expectedValue = "expected";

        // Act
        var result = ValidationMessageFormatter.FormatValueComparisonError(baseMessage, key, actualValue, expectedValue);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(baseMessage);
        result.Should().Contain(actualValue);
        result.Should().Contain(expectedValue);
    }

    [Fact]
    public void FormatValueComparisonError_With_NullActualValue_Should_Format_Message()
    {
        // Arrange
        var baseMessage = "Validation failed";
        var key = "test.key";
        object? actualValue = null;
        var expectedValue = "expected";

        // Act
        var result = ValidationMessageFormatter.FormatValueComparisonError(baseMessage, key, actualValue, expectedValue);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(baseMessage);
        result.Should().Contain("null");
        result.Should().Contain(expectedValue);
    }

    [Fact]
    public void FormatValueComparisonError_With_NullExpectedValue_Should_Format_Message()
    {
        // Arrange
        var baseMessage = "Validation failed";
        var key = "test.key";
        var actualValue = "actual";
        object? expectedValue = null;

        // Act
        var result = ValidationMessageFormatter.FormatValueComparisonError(baseMessage, key, actualValue, expectedValue);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(baseMessage);
        result.Should().Contain(actualValue);
        result.Should().Contain("null");
    }

    [Fact]
    public void FormatValueComparisonError_With_BothNullValues_Should_Format_Message()
    {
        // Arrange
        var baseMessage = "Validation failed";
        var key = "test.key";
        object? actualValue = null;
        object? expectedValue = null;

        // Act
        var result = ValidationMessageFormatter.FormatValueComparisonError(baseMessage, key, actualValue, expectedValue);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(baseMessage);
        result.Should().Contain("null");
    }

    [Fact]
    public void FormatFileNotFoundError_Should_Format_Message()
    {
        // Arrange
        var environmentId = "production";
        var filePath = "/path/to/file.json";
        var exception = new FileNotFoundException("File not found", filePath);

        // Act
        var result = ValidationMessageFormatter.FormatFileNotFoundError(environmentId, filePath, exception);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(environmentId);
        result.Should().Contain(exception.Message);
    }

    [Fact]
    public void FormatFileNotFoundError_With_NullFilePath_Should_Format_Message()
    {
        // Arrange
        var environmentId = "production";
        string? filePath = null;
        var exception = new FileNotFoundException("File not found");

        // Act
        var result = ValidationMessageFormatter.FormatFileNotFoundError(environmentId, filePath, exception);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(environmentId);
        result.Should().Contain(exception.Message);
    }

    [Fact]
    public void FormatConfigurationError_Should_Format_Message()
    {
        // Arrange
        var environmentId = "production";
        var message = "Invalid configuration";
        var exception = new Exception("Config error");

        // Act
        var result = ValidationMessageFormatter.FormatConfigurationError(environmentId, message, exception);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(environmentId);
        result.Should().Contain(exception.Message);
    }

    [Fact]
    public void FormatFailedToLoadEnvironmentError_Should_Format_Message()
    {
        // Arrange
        var environmentId = "production";
        var message = "Failed to load";
        var exception = new Exception("Load error");

        // Act
        var result = ValidationMessageFormatter.FormatFailedToLoadEnvironmentError(environmentId, message, exception);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(environmentId);
        result.Should().Contain(exception.Message);
    }

    [Fact]
    public void FormatValidationFailureWithContext_With_AllParameters_Should_Format_Message()
    {
        // Arrange
        var baseMessage = "Validation failed";
        var environmentId = "production";
        var contextDescription = "Config validation";
        var additionalInfo = "Additional info";
        var suggestion = "Fix the config";

        // Act
        var result = ValidationMessageFormatter.FormatValidationFailureWithContext(
            baseMessage, environmentId, contextDescription, additionalInfo, suggestion);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(baseMessage);
        result.Should().Contain(environmentId);
        result.Should().Contain(contextDescription);
        result.Should().Contain(additionalInfo);
        result.Should().Contain(suggestion);
    }

    [Fact]
    public void FormatValidationFailureWithContext_With_OnlyBaseMessage_Should_Format_Message()
    {
        // Arrange
        var baseMessage = "Validation failed";

        // Act
        var result = ValidationMessageFormatter.FormatValidationFailureWithContext(baseMessage);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(baseMessage);
    }

    [Fact]
    public void FormatValidationFailureWithContext_With_EnvironmentId_Should_Format_Message()
    {
        // Arrange
        var baseMessage = "Validation failed";
        var environmentId = "production";

        // Act
        var result = ValidationMessageFormatter.FormatValidationFailureWithContext(baseMessage, environmentId);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(baseMessage);
        result.Should().Contain(environmentId);
    }

    [Fact]
    public void FormatValidationFailureWithContext_With_ContextDescription_Should_Format_Message()
    {
        // Arrange
        var baseMessage = "Validation failed";
        var contextDescription = "Config validation";

        // Act
        var result = ValidationMessageFormatter.FormatValidationFailureWithContext(
            baseMessage, contextDescription: contextDescription);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(baseMessage);
        result.Should().Contain(contextDescription);
    }

    [Fact]
    public void FormatValidationFailureWithContext_With_AdditionalInfo_Should_Format_Message()
    {
        // Arrange
        var baseMessage = "Validation failed";
        var additionalInfo = "Additional info";

        // Act
        var result = ValidationMessageFormatter.FormatValidationFailureWithContext(
            baseMessage, additionalInfo: additionalInfo);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(baseMessage);
        result.Should().Contain(additionalInfo);
    }

    [Fact]
    public void FormatValidationFailureWithContext_With_Suggestion_Should_Format_Message()
    {
        // Arrange
        var baseMessage = "Validation failed";
        var suggestion = "Fix the config";

        // Act
        var result = ValidationMessageFormatter.FormatValidationFailureWithContext(
            baseMessage, suggestion: suggestion);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(baseMessage);
        result.Should().Contain(suggestion);
    }

    [Fact]
    public void FormatValidationFailureWithContext_With_NullOptionalParameters_Should_Format_Message()
    {
        // Arrange
        var baseMessage = "Validation failed";

        // Act
        var result = ValidationMessageFormatter.FormatValidationFailureWithContext(
            baseMessage, null, null, null, null);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(baseMessage);
    }

    [Fact]
    public void FormatValidationFailureWithContext_With_EmptyOptionalParameters_Should_Format_Message()
    {
        // Arrange
        var baseMessage = "Validation failed";

        // Act
        var result = ValidationMessageFormatter.FormatValidationFailureWithContext(
            baseMessage, "", "", "", "");

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(baseMessage);
    }
}

