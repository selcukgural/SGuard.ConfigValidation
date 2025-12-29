using FluentAssertions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Test.Validators;

public sealed class LessThanOrEqualValidatorTests
{
    private readonly LessThanOrEqualValidator _validator = new();

    [Fact]
    public void ValidatorType_Should_Return_lte()
    {
        // Act
        var result = _validator.ValidatorType;

        // Assert
        result.Should().Be("lte");
    }

    [Theory]
    [InlineData(3, 5, true)]
    [InlineData(0, 10, true)]
    [InlineData(-5, -1, true)]
    [InlineData(-1, 0, true)]
    [InlineData(42, 42, true)] // equal should pass
    [InlineData(5, 3, false)]
    [InlineData(10, 0, false)]
    [InlineData(-1, -5, false)]
    [InlineData(0, -1, false)]
    public void Validate_With_IntegerValues_Should_ReturnCorrectResult(int value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lte",
            Value = conditionValue,
            Message = "Value must be less than or equal to threshold"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Contain("Value must be less than or equal to threshold");
            result.Message.Should().Contain("Actual value:");
            result.Message.Should().Contain("Expected value:");
            result.ValidatorType.Should().Be("lte");
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
    [InlineData(5.0, 5.0, true)] // equal should pass
    [InlineData(5.5, 3.2, false)]
    [InlineData(10.1, 0.0, false)]
    [InlineData(-1.5, -5.2, false)]
    [InlineData(0.1, 0.0, false)]
    public void Validate_With_DoubleValues_Should_ReturnCorrectResult(double value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lte",
            Value = conditionValue,
            Message = "Value must be less than or equal to threshold"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(3, 5.0, true)] // int vs double
    [InlineData(3.2, 5, true)] // double vs int
    [InlineData(5, 5.0, true)] // int vs double, equal
    [InlineData(5.0, 5, true)] // double vs int, equal
    public void Validate_With_MixedNumericTypes_Should_ReturnCorrectResult(object value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lte",
            Value = conditionValue,
            Message = "Value must be less than or equal to threshold"
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
            Validator = "lte",
            Value = 10.5,
            Message = "Value must be less than or equal to threshold"
        };

        // Act & Assert
        var result1 = _validator.Validate(5.5m, condition);
        result1.IsValid.Should().BeTrue();
        result1.Message.Should().BeEmpty();

        var result2 = _validator.Validate(10.5m, condition); // equal
        result2.IsValid.Should().BeTrue();
        result2.Message.Should().BeEmpty();
    }

    [Fact]
    public void Validate_With_NullValue_Should_Pass()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lte",
            Value = 5,
            Message = "Value must be less than or equal to threshold"
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
            Validator = "lte",
            Value = new object(), // non-comparable object
            Message = "Value must be less than or equal to threshold"
        };

        // Act
        var result = _validator.Validate(10, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Value for 'lte' validator must be comparable");
        result.ValidatorType.Should().Be("lte");
    }

    [Fact]
    public void Validate_With_NonComparableConditionValue_Should_Fail_WithProperError()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lte",
            Value = "test",
            Message = "Value must be less than or equal to threshold"
        };

        // Act
        var result = _validator.Validate(5, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Value for 'lte' validator must be comparable");
        result.ValidatorType.Should().Be("lte");
    }

    [Fact]
    public void Validate_With_Failure_Should_Include_CustomErrorMessage()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lte",
            Value = 5,
            Message = "Custom less than or equal error message"
        };

        // Act
        var result = _validator.Validate(10, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Custom less than or equal error message");
        result.Message.Should().Contain("Actual value:");
        result.Message.Should().Contain("Expected value:");
        result.ValidatorType.Should().Be("lte");
        result.Value.Should().Be(10);
    }

    [Fact]
    public void Validate_With_Success_Should_Have_EmptyMessage()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lte",
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
    [InlineData('a', 'a', true)] // equal
    [InlineData('b', 'a', false)]
    public void Validate_With_CharValues_Should_ReturnCorrectResult(char value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lte",
            Value = conditionValue,
            Message = "Value must be less than or equal to threshold"
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
            Validator = "lte",
            Value = tomorrow,
            Message = "Value must be less than or equal to threshold"
        };
        
        var condition2 = new ValidatorCondition
        {
            Validator = "lte",
            Value = yesterday,
            Message = "Value must be less than or equal to threshold"
        };
        
        var condition3 = new ValidatorCondition
        {
            Validator = "lte",
            Value = now,
            Message = "Value must be less than or equal to threshold"
        };

        // Act
        var result1 = _validator.Validate(now, condition1);
        var result2 = _validator.Validate(now, condition2);
        var result3 = _validator.Validate(now, condition3); // equal

        // Assert
        result1.IsValid.Should().BeTrue();
        result1.Message.Should().BeEmpty();
        
        result2.IsValid.Should().BeFalse();
        result2.Message.Should().Contain("Value must be less than or equal to threshold");
        result2.Message.Should().Contain("Actual value:");
        result2.Message.Should().Contain("Expected value:");
        result2.ValidatorType.Should().Be("lte");
        
        result3.IsValid.Should().BeTrue(); // equal should pass
        result3.Message.Should().BeEmpty();
    }

    [Fact]
    public void Validate_With_ComparableError_Should_NotCrash()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lte",
            Value = "string value",
            Message = "Value must be less than or equal to threshold"
        };

        // Act
        var result = _validator.Validate(42, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Value for 'lte' validator must be comparable");
        result.ValidatorType.Should().Be("lte");
    }

    [Theory]
    [InlineData(5, "not_a_number")] // int vs string
    [InlineData(5.5, "not_a_number")] // double vs string
    public void Validate_With_MixedTypeComparison_Should_Fail_WithProperError(object value, object conditionValue)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lte",
            Value = conditionValue,
            Message = "Value must be less than or equal to threshold"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Value for 'lte' validator must be comparable");
        result.ValidatorType.Should().Be("lte");
    }

    [Fact]
    public void Validate_With_NullConditionValue_Should_Fail_WithProperError()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lte",
            Value = null,
            Message = "Value must be less than or equal to threshold"
        };

        // Act
        var result = _validator.Validate(10, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Value for 'lte' validator must be comparable");
        result.Message.Should().Contain("Expected value type: null");
        result.ValidatorType.Should().Be("lte");
    }

    [Fact]
    public void Validate_With_StringNumericComparison_Should_Work()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "lte",
            Value = "10",
            Message = "Value must be less than or equal to threshold"
        };

        // Act
        var result1 = _validator.Validate(5, condition);
        var result2 = _validator.Validate(10, condition);
        var result3 = _validator.Validate(15, condition);
        var result4 = _validator.Validate("5", condition);
        var result5 = _validator.Validate("10", condition);
        var result6 = _validator.Validate("15", condition);

        // Assert
        result1.IsValid.Should().BeTrue();
        result1.Message.Should().BeEmpty();

        result2.IsValid.Should().BeTrue(); // equal should pass
        result2.Message.Should().BeEmpty();

        result3.IsValid.Should().BeFalse();
        result3.Message.Should().Contain("Value must be less than or equal to threshold");

        result4.IsValid.Should().BeTrue();
        result4.Message.Should().BeEmpty();

        result5.IsValid.Should().BeTrue(); // equal should pass
        result5.Message.Should().BeEmpty();

        result6.IsValid.Should().BeFalse();
        result6.Message.Should().Contain("Value must be less than or equal to threshold");
    }
}