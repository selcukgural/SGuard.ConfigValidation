using FluentAssertions;
using System.Text.Json;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Test.Validators;

public sealed class MinLengthValidatorTests
{
    private readonly MinLengthValidator _validator = new();

    [Fact]
    public void ValidatorType_Should_Return_min_len()
    {
        // Act
        var result = _validator.ValidatorType;

        // Assert
        result.Should().Be("min_len");
    }

    [Theory]
    [InlineData("hello", 3, true)]  // 5 >= 3
    [InlineData("hello", 5, true)]   // 5 >= 5
    [InlineData("hello", 6, false)] // 5 < 6
    [InlineData("hello", 10, false)] // 5 < 10
    [InlineData("hello", 1, true)]  // 5 >= 1
    [InlineData("hello", 4, true)]  // 5 >= 4
    [InlineData("hello", 0, true)]  // 5 >= 0
    public void Validate_With_StringLength_Should_ReturnCorrectResult(string value, int minLength, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "min_len",
            Value = minLength,
            Message = "String is too short"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Contain("String is too short");
            result.Message.Should().Contain("Actual value:");
            result.Message.Should().Contain("minimum length");
            result.ValidatorType.Should().Be("min_len");
        }
        else
        {
            result.Message.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData("", 0, true)]
    [InlineData("", 1, false)]
    [InlineData("", 100, false)]
    public void Validate_With_EmptyString_Should_ReturnCorrectResult(string value, int minLength, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "min_len",
            Value = minLength,
            Message = "String is too short"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(null, false)]  // null -> empty string -> length 0 < 5
    [InlineData(0, false)]     // "0" -> length 1 < 5
    [InlineData(1, false)]     // "1" -> length 1 < 5
    [InlineData(100, false)]   // "100" -> length 3 < 5
    [InlineData(12345, true)] // "12345" -> length 5 >= 5
    public void Validate_With_NullValue_Should_Pass(object? value, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "min_len",
            Value = 5,
            Message = "String is too short"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Contain("String is too short");
            result.Message.Should().Contain("Actual value:");
            result.Message.Should().Contain("minimum length");
        }
        else
        {
            result.Message.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData(42, 3, false)] // string "42" length 2 < min 3
    [InlineData(42, 5, false)] // string "42" length 2 < min 5
    [InlineData(42, 2, true)]  // string "42" length 2 >= min 2
    [InlineData(42, 1, true)]  // string "42" length 2 >= min 1
    public void Validate_With_NonStringValues_Should_ConvertToString(object value, int minLength, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "min_len",
            Value = minLength,
            Message = "String is too short"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Contain("String is too short");
            result.Message.Should().Contain("Actual value:");
            result.Message.Should().Contain("minimum length");
            result.ValidatorType.Should().Be("min_len");
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
            Validator = "min_len",
            Value = jsonValue,
            Message = "String must meet minimum length"
        };

        // Act
        var result = _validator.Validate("long string", condition);

        // Assert
        result.IsValid.Should().BeTrue(); // 11 characters >= 10
        result.Message.Should().BeEmpty();
    }

    [Fact]
    public void Validate_With_StringToIntConversion_Should_ConvertCorrectly()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "min_len",
            Value = "10",
            Message = "String must meet minimum length"
        };

        // Act
        var result = _validator.Validate("test string", condition);

        // Assert
        result.IsValid.Should().BeTrue(); // 10 characters >= 10
        result.Message.Should().BeEmpty();
    }

    [Fact]
    public void Validate_With_InvalidStringToIntConversion_Should_ReturnValidationFailure()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "min_len",
            Value = "not_a_number",
            Message = "String is too short"
        };

        // Act
        var result = _validator.Validate("test", condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("String is too short");
        result.Message.Should().Contain("invalid condition value type");
        result.Message.Should().Contain("min_len");
        result.ValidatorType.Should().Be("min_len");
    }

    [Fact]
    public void Validate_With_UnsupportedValueType_Should_ReturnValidationFailure()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "min_len",
            Value = new object(), // unsupported type
            Message = "String is too short"
        };

        // Act
        var result = _validator.Validate("test", condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("String is too short");
        result.Message.Should().Contain("invalid condition value type");
        result.Message.Should().Contain("min_len");
        result.ValidatorType.Should().Be("min_len");
    }

    [Fact]
    public void Validate_With_Failure_Should_Include_CustomErrorMessage()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "min_len",
            Value = 10,
            Message = "Custom min length error message"
        };

        // Act
        var result = _validator.Validate("hi", condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Custom min length error message");
        result.Message.Should().Contain("Actual value:");
        result.Message.Should().Contain("minimum length");
        result.ValidatorType.Should().Be("min_len");
        result.Value.Should().Be("hi");
    }

    [Fact]
    public void Validate_With_Success_Should_Have_EmptyMessage()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "min_len",
            Value = 5,
            Message = "This should not appear"
        };

        // Act
        var result = _validator.Validate("exact", condition);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Message.Should().BeEmpty();
        result.ValidatorType.Should().BeEmpty();
    }

    [Theory]
    [InlineData("very long string that exceeds the maximum allowed length", 20, true)]  // 47 >= 20
    [InlineData("exact length", 12, true)]  // 12 >= 12
    [InlineData("short", 10, false)]  // 5 < 10
    public void Validate_With_ComplexStringScenarios_Should_WorkCorrectly(string value, int minLength, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "min_len",
            Value = minLength,
            Message = "String is too short"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Contain("String is too short");
            result.Message.Should().Contain("Actual value:");
            result.Message.Should().Contain("minimum length");
            result.ValidatorType.Should().Be("min_len");
        }
        else
        {
            result.Message.Should().BeEmpty();
        }
    }

    [Fact]
    public void Validate_With_JsonElementStringValue_Should_ConvertCorrectly()
    {
        // Arrange
        var jsonValue = JsonSerializer.Deserialize<JsonElement>("\"10\"");
        var condition = new ValidatorCondition
        {
            Validator = "min_len",
            Value = jsonValue,
            Message = "String must meet minimum length"
        };

        // Act
        var result = _validator.Validate("long string", condition);

        // Assert
        result.IsValid.Should().BeTrue(); // 11 characters >= 10
        result.Message.Should().BeEmpty();
    }
}