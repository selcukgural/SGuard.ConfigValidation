using FluentAssertions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Tests.Validators;

public sealed class GreaterThanOrEqualValidatorTests
{
    private readonly GreaterThanOrEqualValidator _validator = new();

    [Fact]
    public void ValidatorType_Should_Return_gte()
    {
        // Act
        var result = _validator.ValidatorType;

        // Assert
        result.Should().Be("gte");
    }

    [Theory]
    [InlineData(5, 3, true)]
    [InlineData(10, 0, true)]
    [InlineData(-1, -5, true)]
    [InlineData(0, -1, true)]
    [InlineData(42, 42, true)] // equal should pass
    [InlineData(3, 5, false)]
    [InlineData(0, 10, false)]
    [InlineData(-5, -1, false)]
    [InlineData(-1, 0, false)]
    public void Validate_With_IntegerValues_Should_ReturnCorrectResult(int value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "gte",
            Value = conditionValue,
            Message = "Value must be greater than or equal to threshold"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Message.Should().Be("Value must be greater than or equal to threshold");
            result.ValidatorType.Should().Be("gte");
        }
        else
        {
            result.Message.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData(5.5, 3.2, true)]
    [InlineData(10.1, 0.0, true)]
    [InlineData(-1.5, -5.2, true)]
    [InlineData(0.1, 0.0, true)]
    [InlineData(5.0, 5.0, true)] // equal should pass
    [InlineData(3.2, 5.5, false)]
    [InlineData(0.0, 10.1, false)]
    [InlineData(-5.2, -1.5, false)]
    [InlineData(0.0, 0.1, false)]
    public void Validate_With_DoubleValues_Should_ReturnCorrectResult(double value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "gte",
            Value = conditionValue,
            Message = "Value must be greater than or equal to threshold"
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
            Validator = "gte",
            Value = 5.5,
            Message = "Value must be greater than or equal to threshold"
        };

        // Act & Assert
        var result1 = _validator.Validate(10.5m, condition);
        result1.IsValid.Should().BeTrue();
        result1.Message.Should().BeEmpty();

        var result2 = _validator.Validate(5.5m, condition); // equal
        result2.IsValid.Should().BeTrue();
        result2.Message.Should().BeEmpty();
    }

    [Fact]
    public void Validate_With_NullValue_Should_Pass()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "gte",
            Value = 5,
            Message = "Value must be greater than or equal to threshold"
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
            Validator = "gte",
            Value = new object(), // non-comparable object
            Message = "Value must be greater than or equal to threshold"
        };

        // Act
        var result = _validator.Validate(10, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Value for 'gte' validator must be comparable");
        result.ValidatorType.Should().Be("gte");
    }

    [Fact]
    public void Validate_With_NonComparableConditionValue_Should_Fail_WithProperError()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "gte",
            Value = "test",
            Message = "Value must be greater than or equal to threshold"
        };

        // Act
        var result = _validator.Validate(5, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Value for 'gte' validator must be comparable");
        result.ValidatorType.Should().Be("gte");
    }

    [Fact]
    public void Validate_With_Failure_Should_Include_CustomErrorMessage()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "gte",
            Value = 10,
            Message = "Custom greater than or equal error message"
        };

        // Act
        var result = _validator.Validate(5, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Be("Custom greater than or equal error message");
        result.ValidatorType.Should().Be("gte");
        result.Value.Should().Be(5);
    }

    [Fact]
    public void Validate_With_Success_Should_Have_EmptyMessage()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "gte",
            Value = 5,
            Message = "This should not appear"
        };

        // Act
        var result = _validator.Validate(10, condition);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Message.Should().BeEmpty();
        result.ValidatorType.Should().BeEmpty();
    }

    [Theory]
    [InlineData('b', 'a', true)] // char comparison
    [InlineData('a', 'a', true)] // equal
    [InlineData('a', 'b', false)]
    public void Validate_With_CharValues_Should_ReturnCorrectResult(char value, object conditionValue, bool expectedValid)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "gte",
            Value = conditionValue,
            Message = "Value must be greater than or equal to threshold"
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
            Validator = "gte",
            Value = yesterday,
            Message = "Value must be greater than or equal to threshold"
        };
        
        var condition2 = new ValidatorCondition
        {
            Validator = "gte",
            Value = tomorrow,
            Message = "Value must be greater than or equal to threshold"
        };
        
        var condition3 = new ValidatorCondition
        {
            Validator = "gte",
            Value = now,
            Message = "Value must be greater than or equal to threshold"
        };

        // Act
        var result1 = _validator.Validate(now, condition1);
        var result2 = _validator.Validate(now, condition2);
        var result3 = _validator.Validate(now, condition3); // equal

        // Assert
        result1.IsValid.Should().BeTrue();
        result1.Message.Should().BeEmpty();
        
        result2.IsValid.Should().BeFalse();
        result2.Message.Should().Be("Value must be greater than or equal to threshold");
        result2.ValidatorType.Should().Be("gte");
        
        result3.IsValid.Should().BeTrue(); // equal should pass
        result3.Message.Should().BeEmpty();
    }

    [Fact]
    public void Validate_With_ComparableError_Should_NotCrash()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "gte",
            Value = "string value",
            Message = "Value must be greater than or equal to threshold"
        };

        // Act
        var result = _validator.Validate(42, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("must be comparable");
        result.ValidatorType.Should().Be("gte");
    }

    [Theory]
    [InlineData(5, "not_a_number")] // int vs string
    [InlineData(5.5, "not_a_number")] // double vs string
    public void Validate_With_MixedTypeComparison_Should_Fail_WithProperError(object value, object conditionValue)
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "gte",
            Value = conditionValue,
            Message = "Value must be greater than or equal to threshold"
        };

        // Act
        var result = _validator.Validate(value, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Value for 'gte' validator must be comparable");
        result.ValidatorType.Should().Be("gte");
    }
}