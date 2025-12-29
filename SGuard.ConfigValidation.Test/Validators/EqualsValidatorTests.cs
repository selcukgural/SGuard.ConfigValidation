using FluentAssertions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Test.Validators;

public sealed class EqualsValidatorTests
{
    private readonly EqualsValidator _validator = new();

    [Fact]
    public void ValidatorType_Should_Return_eq()
    {
        // Act
        var result = _validator.ValidatorType;

        // Assert
        result.Should().Be("eq");
    }

    [Theory]
    [InlineData("hello", "hello", true)]
    [InlineData("", "", true)]
    [InlineData("test string", "test string", true)]
    [InlineData("Hello", "hello", false)] // case sensitive
    public void Validate_With_StringValues_Should_ReturnCorrectResult(string value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "eq",
            Value = conditionValue,
            Message = "Values must be equal"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Contain("Values must be equal");
            result.Message.Should().Contain("Actual value:");
            result.Message.Should().Contain("Expected value:");
            result.ValidatorType.Should().Be("eq");
        }
        else
        {
            result.Message.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData(42, 42, true)]
    [InlineData(0, 0, true)]
    [InlineData(-1, -1, true)]
    [InlineData(42, 43, false)]
    [InlineData(0, 1, false)]
    [InlineData(-1, 1, false)]
    public void Validate_With_IntegerValues_Should_ReturnCorrectResult(int value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "eq",
            Value = conditionValue,
            Message = "Values must be equal"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Contain("Values must be equal");
            result.Message.Should().Contain("Actual value:");
            result.Message.Should().Contain("Expected value:");
            result.ValidatorType.Should().Be("eq");
        }
    }

    [Theory]
    [InlineData(42.5, 42.5, true)]
    [InlineData(0.0, 0.0, true)]
    [InlineData(-1.5, -1.5, true)]
    [InlineData(42.5, 42.6, false)]
    [InlineData(0.1, 0.0, false)]
    [InlineData(-1.5, -1.6, false)]
    public void Validate_With_DoubleValues_Should_ReturnCorrectResult(double value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "eq",
            Value = conditionValue,
            Message = "Values must be equal"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void Validate_With_BooleanValues_Should_ReturnCorrectResult(bool value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "eq",
            Value = conditionValue,
            Message = "Boolean values must be equal"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Contain("Boolean values must be equal");
            result.Message.Should().Contain("Actual value:");
            result.Message.Should().Contain("Expected value:");
        }
    }

    [Theory]
    [InlineData(null, "test", false)]
    [InlineData("test", null, false)]
    public void Validate_With_NullValue_Should_Fail_WhenOtherIsNotNull(object? value, object? conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "eq",
            Value = conditionValue,
            Message = "Values must be equal"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Contain("Values must be equal");
            result.Message.Should().Contain("Actual value:");
            result.Message.Should().Contain("Expected value:");
        }
    }

    [Fact]
    public void Validate_With_BothNull_Should_Fail()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "eq",
            Value = null,
            Message = "Values must be equal"
        };

        // Act
        var result = _validator.Validate(null, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Values must be equal");
        result.Message.Should().Contain("Actual value:");
        result.Message.Should().Contain("Expected value:");
        result.ValidatorType.Should().Be("eq");
    }

    [Theory]
    [InlineData(42, "42", false)]
    [InlineData("42", 42, false)]
    [InlineData(42.0, 42, false)] // double vs int
    public void Validate_With_DifferentTypes_Should_Fail(object value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "eq",
            Value = conditionValue,
            Message = "Values must be equal"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Contain("Values must be equal");
            result.Message.Should().Contain("Actual value:");
            result.Message.Should().Contain("Expected value:");
        }
    }

    [Fact]
    public void Validate_With_Failure_Should_Include_CustomErrorMessage()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "eq",
            Value = "expected",
            Message = "Custom error message for equality"
        };

        // Act
        var result = _validator.Validate("actual", condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Custom error message for equality");
        result.Message.Should().Contain("Actual value:");
        result.Message.Should().Contain("Expected value:");
        result.ValidatorType.Should().Be("eq");
        result.Value.Should().Be("actual");
    }

    [Fact]
    public void Validate_With_Success_Should_Have_EmptyMessage()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "eq",
            Value = "same value",
            Message = "This should not appear"
        };

        // Act
        var result = _validator.Validate("same value", condition);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Message.Should().BeEmpty();
        result.ValidatorType.Should().BeEmpty();
    }
}