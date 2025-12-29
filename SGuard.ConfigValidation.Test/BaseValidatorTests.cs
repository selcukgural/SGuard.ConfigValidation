using FluentAssertions;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Results;
using SGuard.ConfigValidation.Validators;
using SGuard.ConfigValidation.Validators.Base;
using ValueType = SGuard.ConfigValidation.Common.ValueType;

namespace SGuard.ConfigValidation.Test;

public sealed class BaseValidatorTests
{
    [Fact]
    public void BaseValidator_CreateSuccess_Should_Return_SuccessResult()
    {
        // Arrange - Use a concrete validator implementation
        var validator = new RequiredValidator();

        // Act - Use reflection to access protected method
        var method = typeof(BaseValidator<object>).GetMethod("CreateSuccess",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (ValidationResult)method!.Invoke(validator, null)!;

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void BaseValidator_CreateFailure_Should_Return_FailureResult()
    {
        // Arrange
        var validator = new RequiredValidator();
        var message = "Test error";
        var key = "Test:Key";
        var value = "test-value";

        // Act - Use reflection to access protected method
        var method = typeof(BaseValidator<object>).GetMethod("CreateFailure",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(string), typeof(string), typeof(object), typeof(Exception) },
            null);
        var result = (ValidationResult)method!.Invoke(validator, new object[] { message, key, value, null! })!;

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain(message);
    }

    [Fact]
    public void BaseValidator_CreateFailure_With_Exception_Should_Return_FailureResult()
    {
        // Arrange
        var validator = new RequiredValidator();
        var message = "Test error";
        var key = "Test:Key";
        var value = "test-value";
        var exception = new Exception("Inner error");

        // Act - Use reflection to access protected method
        var method = typeof(BaseValidator<object>).GetMethod("CreateFailure",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(string), typeof(string), typeof(object), typeof(Exception) },
            null);
        var result = (ValidationResult)method!.Invoke(validator, new object[] { message, key, value, exception })!;

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain(message);
    }

    [Fact]
    public void BaseValidator_CreateFailure_With_ExpectedValue_Should_Return_FailureResult()
    {
        // Arrange
        var validator = new RequiredValidator();
        var message = "Test error";
        var key = "Test:Key";
        var actualValue = "actual";
        var expectedValue = "expected";

        // Act - Use reflection to access protected method
        var method = typeof(BaseValidator<object>).GetMethod("CreateFailure",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(string), typeof(string), typeof(object), typeof(object), typeof(Exception) },
            null);
        var result = (ValidationResult)method!.Invoke(validator, new object[] { message, key, actualValue, expectedValue, null! })!;

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain(message);
        result.Message.Should().Contain("Actual value");
        result.Message.Should().Contain("Expected value");
    }

    [Fact]
    public void BaseValidator_SupportedValueTypes_Should_Return_DefaultTypes()
    {
        // Arrange
        var validator = new RequiredValidator();

        // Act
        var supportedTypes = validator.SupportedValueTypes;

        // Assert
        supportedTypes.Should().NotBeNull();
        supportedTypes.Should().Contain(ValueType.String);
        supportedTypes.Should().Contain(ValueType.Number);
        supportedTypes.Should().Contain(ValueType.Boolean);
        supportedTypes.Should().Contain(ValueType.Null);
        supportedTypes.Should().Contain(ValueType.JsonElement);
    }

    [Fact]
    public void BaseValidator_ValidatorType_Should_Return_Type()
    {
        // Arrange
        var validator = new RequiredValidator();

        // Act
        var validatorType = validator.ValidatorType;

        // Assert
        validatorType.Should().NotBeNullOrWhiteSpace();
        validatorType.Should().Be("required");
    }
}

