using FluentAssertions;
using System.Text.Json;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Tests.Validators;

public sealed class MaxLengthValidatorTests
{
    private readonly MaxLengthValidator _validator = new();

    [Fact]
    public void ValidatorType_Should_Return_max_len()
    {
        // Act
        var result = _validator.ValidatorType;

        // Assert
        result.Should().Be("max_len");
    }

    [Theory]
    [InlineData("hello", 5, true)]
    [InlineData("hello", 10, true)]
    [InlineData("hello", 100, true)]
    [InlineData("hello", 1, false)]
    [InlineData("hello", 4, false)]
    [InlineData("hello", 0, false)]
    public void Validate_With_StringLength_Should_ReturnCorrectResult(string value, int maxLength, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "max_len",
            Value = maxLength,
            Message = "String exceeds maximum length"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Contain("String exceeds maximum length");
            result.Message.Should().Contain("Actual value:");
            result.Message.Should().Contain("maximum length");
            result.ValidatorType.Should().Be("max_len");
        }
        else
        {
            result.Message.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData("", 0, true)]
    [InlineData("", 1, true)]
    [InlineData("", 100, true)]
    public void Validate_With_EmptyString_Should_Pass(string value, int maxLength, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "max_len",
            Value = maxLength,
            Message = "String exceeds maximum length"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        result.Message.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(100, true)]
    public void Validate_With_NullValue_Should_Pass(object? value, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "max_len",
            Value = 5,
            Message = "String exceeds maximum length"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        result.Message.Should().BeEmpty();
    }

    [Theory]
    [InlineData(42, 3, true)] // string "42" length 2 <= max 3
    [InlineData(42, 5, true)] // string "42" length 2 <= max 5
    [InlineData(42, 1, false)] // string "42" length 2 > max 1
    public void Validate_With_NonStringValues_Should_ConvertToString(object value, int maxLength, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "max_len",
            Value = maxLength,
            Message = "Value exceeds maximum length"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Contain("Value exceeds maximum length");
            result.Message.Should().Contain("Actual value:");
            result.Message.Should().Contain("Expected value:");
            result.ValidatorType.Should().Be("max_len");
        }
        else
        {
            result.Message.Should().BeEmpty();
        }
    }

    [Fact]
    public void Validate_With_JsonElementIntValue_Should_ConvertCorrectly()
    {
        // Arrange
        var jsonValue = JsonSerializer.Deserialize<JsonElement>("10");
        var condition = new ValidatorCondition
        {
            Validator = "max_len",
            Value = jsonValue,
            Message = "String exceeds maximum length"
        };

        // Act
        var result = _validator.Validate("long string", condition);

        // Assert
        result.IsValid.Should().BeFalse(); // 11 chars > 10
        result.Message.Should().Contain("String exceeds maximum length");
        result.Message.Should().Contain("Actual value:");
        result.Message.Should().Contain("maximum length");
        result.ValidatorType.Should().Be("max_len");
    }

    [Fact]
    public void Validate_With_StringToIntConversion_Should_ConvertCorrectly()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "max_len",
            Value = "10",
            Message = "String exceeds maximum length"
        };

        // Act
        var result = _validator.Validate("test string", condition);

        // Assert
        result.IsValid.Should().BeFalse(); // 11 chars > 10
        result.Message.Should().Contain("String exceeds maximum length");
        result.Message.Should().Contain("Actual value:");
        result.Message.Should().Contain("maximum length");
    }

    [Fact]
    public void Validate_With_InvalidStringToIntConversion_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "max_len",
            Value = "not_a_number",
            Message = "String exceeds maximum length"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _validator.Validate("test", condition));
        exception.Message.Should().Contain("Cannot convert value 'not_a_number' to int for max_len validator");
    }

    [Fact]
    public void Validate_With_UnsupportedValueType_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "max_len",
            Value = new object(), // unsupported type
            Message = "String exceeds maximum length"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _validator.Validate("test", condition));
        exception.Message.Should().Contain("Cannot convert value");
        exception.Message.Should().Contain("for max_len validator");
    }

    [Fact]
    public void Validate_With_Failure_Should_Include_CustomErrorMessage()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "max_len",
            Value = 3,
            Message = "Custom max length error message"
        };

        // Act
        var result = _validator.Validate("long string", condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Custom max length error message");
        result.Message.Should().Contain("Actual value:");
        result.Message.Should().Contain("Expected value:");
        result.ValidatorType.Should().Be("max_len");
        result.Value.Should().Be("long string");
    }

    [Fact]
    public void Validate_With_Success_Should_Have_EmptyMessage()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "max_len",
            Value = 50,
            Message = "This should not appear"
        };

        // Act
        var result = _validator.Validate("short", condition);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Message.Should().BeEmpty();
        result.ValidatorType.Should().BeEmpty();
    }

    [Theory]
    [InlineData("very long string that exceeds the maximum allowed length", 20, false)]
    [InlineData("exact length", 12, true)]
    [InlineData("shorter", 10, true)]
    public void Validate_With_ComplexStringScenarios_Should_WorkCorrectly(string value, int maxLength, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "max_len",
            Value = maxLength,
            Message = "String exceeds maximum length"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Contain("String exceeds maximum length");
            result.Message.Should().Contain("Actual value:");
            result.Message.Should().Contain("maximum length");
            result.ValidatorType.Should().Be("max_len");
        }
        else
        {
            result.Message.Should().BeEmpty();
        }
    }


}