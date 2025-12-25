using FluentAssertions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Tests.Validators;

public sealed class NotEqualValidatorTests
{
    private readonly NotEqualValidator _validator = new();

    [Fact]
    public void ValidatorType_Should_Return_ne()
    {
        // Act
        var result = _validator.ValidatorType;

        // Assert
        result.Should().Be("ne");
    }

    [Theory]
    [InlineData("hello", "world", true)]
    [InlineData("a", "b", true)]
    [InlineData("", "not empty", true)]
    [InlineData("test", "test", false)] // same strings should fail
    [InlineData("Hello", "hello", true)] // case sensitive - different
    public void Validate_With_StringValues_Should_ReturnCorrectResult(string value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "ne",
            Value = conditionValue,
            Message = "Values must not be equal"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Be("Values must not be equal");
            result.ValidatorType.Should().Be("ne");
        }
        else
        {
            result.Message.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData(42, 43, true)]
    [InlineData(0, 1, true)]
    [InlineData(-1, 1, true)]
    [InlineData(42, 42, false)] // same numbers should fail
    [InlineData(0, 0, false)]
    [InlineData(-1, -1, false)]
    public void Validate_With_IntegerValues_Should_ReturnCorrectResult(int value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "ne",
            Value = conditionValue,
            Message = "Values must not be equal"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Be("Values must not be equal");
            result.ValidatorType.Should().Be("ne");
        }
    }

    [Theory]
    [InlineData(42.5, 42.6, true)]
    [InlineData(0.0, 0.1, true)]
    [InlineData(-1.5, -1.6, true)]
    [InlineData(42.5, 42.5, false)] // same doubles should fail
    [InlineData(0.0, 0.0, false)]
    [InlineData(-1.5, -1.5, false)]
    public void Validate_With_DoubleValues_Should_ReturnCorrectResult(double value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "ne",
            Value = conditionValue,
            Message = "Values must not be equal"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, false)] // same booleans should fail
    [InlineData(false, false, false)]
    public void Validate_With_BooleanValues_Should_ReturnCorrectResult(bool value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "ne",
            Value = conditionValue,
            Message = "Boolean values must not be equal"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Be("Boolean values must not be equal");
        }
    }

    [Theory]
    [InlineData(null, "test", true)]
    [InlineData("test", null, true)]
    public void Validate_With_NullValue_Should_Pass_WhenOtherIsNotNull(object? value, object? conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "ne",
            Value = conditionValue,
            Message = "Values must not be equal"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Be("Values must not be equal");
        }
    }

    [Fact]
    public void Validate_With_BothNull_Should_Pass()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "ne",
            Value = null,
            Message = "Values must not be equal"
        };

        // Act
        var result = _validator.Validate(null, condition);

        // Assert
        result.IsValid.Should().BeTrue(); // null != null is false, so not equal should pass
        result.Message.Should().BeEmpty();
        result.ValidatorType.Should().BeEmpty();
    }

    [Theory]
    [InlineData(42, "42", true)]
    [InlineData("42", 42, true)]
    [InlineData(42.0, 42, true)] // double vs int - different types
    public void Validate_With_DifferentTypes_Should_Pass(object value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "ne",
            Value = conditionValue,
            Message = "Values must not be equal"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Be("Values must not be equal");
        }
    }

    [Fact]
    public void Validate_With_Failure_Should_Include_CustomErrorMessage()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "ne",
            Value = "same value",
            Message = "Custom error message for inequality"
        };

        // Act
        var result = _validator.Validate("same value", condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Be("Custom error message for inequality");
        result.ValidatorType.Should().Be("ne");
        result.Value.Should().Be("same value");
    }

    [Fact]
    public void Validate_With_Success_Should_Have_EmptyMessage()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "ne",
            Value = "different value",
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