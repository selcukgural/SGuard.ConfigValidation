using FluentAssertions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Tests.Validators;

public sealed class LessThanValidatorTests
{
    private readonly LessThanValidator _validator = new();

    [Fact]
    public void ValidatorType_Should_Return_lt()
    {
        // Act
        var result = _validator.ValidatorType;

        // Assert
        result.Should().Be("lt");
    }

    [Theory]
    [InlineData(3, 5, true)]
    [InlineData(0, 10, true)]
    [InlineData(-5, -1, true)]
    [InlineData(-1, 0, true)]
    [InlineData(42, 42, false)] // equal should fail
    [InlineData(5, 3, false)]
    [InlineData(10, 0, false)]
    [InlineData(-1, -5, false)]
    [InlineData(0, -1, false)]
    public void Validate_With_IntegerValues_Should_ReturnCorrectResult(int value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lt",
            Value = conditionValue,
            Message = "Value must be less than threshold"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Be("Value must be less than threshold");
            result.ValidatorType.Should().Be("lt");
        }
        else
        {
            result.Message.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData(3.2, 5.5, true)]
    [InlineData(0.0, 10.1, true)]
    [InlineData(-5.2, -1.5, true)]
    [InlineData(0.0, 0.1, true)]
    [InlineData(5.0, 5.0, false)] // equal should fail
    [InlineData(5.5, 3.2, false)]
    [InlineData(10.1, 0.0, false)]
    [InlineData(-1.5, -5.2, false)]
    [InlineData(0.1, 0.0, false)]
    public void Validate_With_DoubleValues_Should_ReturnCorrectResult(double value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lt",
            Value = conditionValue,
            Message = "Value must be less than threshold"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(3, 5.0, true)] // int vs double
    [InlineData(3.2, 5, true)] // double vs int
    public void Validate_With_MixedNumericTypes_Should_ReturnCorrectResult(object value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lt",
            Value = conditionValue,
            Message = "Value must be less than threshold"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void Validate_With_DecimalValues_Should_ReturnCorrectResult()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lt",
            Value = 10.5,
            Message = "Value must be less than threshold"
        };

        // Act & Assert
        var result1 = _validator.Validate(5.5m, condition);
        result1.IsValid.Should().BeTrue();
        result1.Message.Should().BeEmpty();

        var result2 = _validator.Validate(42, condition);
        result2.IsValid.Should().BeFalse();
        result2.Message.Should().Be("Value must be less than threshold");
    }

    [Fact]
    public void Validate_With_NullValue_Should_Pass()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lt",
            Value = 5,
            Message = "Value must be less than threshold"
        };

        // Act
        var result = _validator.Validate(null, condition);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Message.Should().BeEmpty();
    }

    [Fact]
    public void Validate_With_NonComparableValue_Should_Fail_WithProperError()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lt",
            Value = new object(), // non-comparable object
            Message = "Value must be less than threshold"
        };

        // Act
        var result = _validator.Validate(10, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Value for 'lt' validator must be comparable");
        result.ValidatorType.Should().Be("lt");
    }

    [Fact]
    public void Validate_With_NonComparableConditionValue_Should_Fail_WithProperError()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lt",
            Value = "test",
            Message = "Value must be less than threshold"
        };

        // Act
        var result = _validator.Validate(5, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Value for 'lt' validator must be comparable");
        result.ValidatorType.Should().Be("lt");
    }

    [Fact]
    public void Validate_With_Failure_Should_Include_CustomErrorMessage()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lt",
            Value = 5,
            Message = "Custom less than error message"
        };

        // Act
        var result = _validator.Validate(10, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Be("Custom less than error message");
        result.ValidatorType.Should().Be("lt");
        result.Value.Should().Be(10);
    }

    [Fact]
    public void Validate_With_Success_Should_Have_EmptyMessage()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lt",
            Value = 10,
            Message = "This should not appear"
        };

        // Act
        var result = _validator.Validate(5, condition);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Message.Should().BeEmpty();
        result.ValidatorType.Should().BeEmpty();
    }

    [Theory]
    [InlineData('a', 'b', true)] // char comparison
    [InlineData('b', 'a', false)]
    public void Validate_With_CharValues_Should_ReturnCorrectResult(char value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lt",
            Value = conditionValue,
            Message = "Value must be less than threshold"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void Validate_With_DateTimeValues_Should_ReturnCorrectResult()
    {
        // Arrange
        var now = DateTime.Now;
        var yesterday = now.AddDays(-1);
        var tomorrow = now.AddDays(1);
        
        var condition1 = new ValidatorCondition
        {
            Validator = "lt",
            Value = tomorrow,
            Message = "Value must be less than threshold"
        };
        
        var condition2 = new ValidatorCondition
        {
            Validator = "lt",
            Value = yesterday,
            Message = "Value must be less than threshold"
        };

        // Act
        var result1 = _validator.Validate(now, condition1);
        var result2 = _validator.Validate(now, condition2);

        // Assert
        result1.IsValid.Should().BeTrue();
        result1.Message.Should().BeEmpty();
        
        result2.IsValid.Should().BeFalse();
        result2.Message.Should().Be("Value must be less than threshold");
        result2.ValidatorType.Should().Be("lt");
    }

    [Fact]
    public void Validate_With_ComparableError_Should_NotCrash()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lt",
            Value = "string value",
            Message = "Value must be less than threshold"
        };

        // Act
        var result = _validator.Validate(42, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Value for 'lt' validator must be comparable");
        result.ValidatorType.Should().Be("lt");
    }

    [Theory]
    [InlineData(5, "not_a_number")] // int vs string
    [InlineData(5.5, "not_a_number")] // double vs string
    public void Validate_With_MixedTypeComparison_Should_Fail_WithProperError(object value, object conditionValue)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lt",
            Value = conditionValue,
            Message = "Value must be less than threshold"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Value for 'lt' validator must be comparable");
        result.ValidatorType.Should().Be("lt");
    }
}