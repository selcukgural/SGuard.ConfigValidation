using FluentAssertions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Tests.Validators;

public sealed class RequiredValidatorTests
{
    private readonly RequiredValidator _validator = new();

    [Fact]
    public void ValidatorType_Should_Return_Correct_Type()
    {
        // Act
        var result = _validator.ValidatorType;

        // Assert
        result.Should().Be("required");
    }

    [Fact]
    public void Validate_With_NullValue_Should_Fail()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "required",
            Message = "Value is required"
        };

        // Act
        var result = _validator.Validate(null, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Be("Value is required");
    }

    [Fact]
    public void Validate_With_Value_Should_Pass()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "required",
            Message = "Value is required"
        };

        // Act
        var result = _validator.Validate("hello", condition);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Message.Should().BeEmpty();
    }
}