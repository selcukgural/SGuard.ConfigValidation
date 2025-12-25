using FluentAssertions;
using System.Text.Json;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Tests.Validators;

public sealed class InValidatorTests
{
    private readonly InValidator _validator = new();

    [Fact]
    public void ValidatorType_Should_Return_in()
    {
        // Act
        var result = _validator.ValidatorType;

        // Assert
        result.Should().Be("in");
    }

    [Fact]
    public void Validate_With_ValueInStringArray_Should_Pass()
    {
        // Arrange
        var jsonString = """["dev", "staging", "prod"]""";
        var jsonValue = JsonSerializer.Deserialize<JsonElement>(jsonString);
        var condition = new ValidatorCondition
        {
            Validator = "in",
            Value = jsonValue,
            Message = "Environment must be one of: dev, staging, prod"
        };

        // Act
        var result1 = _validator.Validate("dev", condition);
        var result2 = _validator.Validate("staging", condition);
        var result3 = _validator.Validate("prod", condition);

        // Assert
        result1.IsValid.Should().BeTrue();
        result1.Message.Should().BeEmpty();

        result2.IsValid.Should().BeTrue();
        result2.Message.Should().BeEmpty();

        result3.IsValid.Should().BeTrue();
        result3.Message.Should().BeEmpty();
    }

    [Fact]
    public void Validate_With_ValueNotInStringArray_Should_Fail()
    {
        // Arrange
        var jsonString = """["dev", "staging", "prod"]""";
        var jsonValue = JsonSerializer.Deserialize<JsonElement>(jsonString);
        var condition = new ValidatorCondition
        {
            Validator = "in",
            Value = jsonValue,
            Message = "Environment must be one of: dev, staging, prod"
        };

        // Act
        var result = _validator.Validate("test", condition);

        // Assert
        result.IsValid.Should().BeFalse(); // "test" is not in array
        result.Message.Should().Be("Environment must be one of: dev, staging, prod");
        result.ValidatorType.Should().Be("in");
    }

    [Fact]
    public void Validate_With_JSONArrayParsing_Should_HandleComplexArrays()
    {
        // Arrange
        var jsonString = """["http://localhost:5000", "https://api.example.com", "http://test.local"]""";
        var jsonValue = JsonSerializer.Deserialize<JsonElement>(jsonString);
        var condition = new ValidatorCondition
        {
            Validator = "in",
            Value = jsonValue,
            Message = "Base URL must be one of the allowed values"
        };

        // Act
        var result1 = _validator.Validate("https://api.example.com", condition);
        var result2 = _validator.Validate("http://invalid.com", condition);

        // Assert
        result1.IsValid.Should().BeTrue();
        result1.Message.Should().BeEmpty();

        result2.IsValid.Should().BeFalse();
        result2.Message.Should().Be("Base URL must be one of the allowed values");
        result2.ValidatorType.Should().Be("in");
    }

    [Fact]
    public void Validate_With_CaseSensitivity_Should_BeCaseInsensitive()
    {
        // Arrange
        var jsonString = """["DEV", "STAGING", "PROD"]""";
        var jsonValue = JsonSerializer.Deserialize<JsonElement>(jsonString);
        var condition = new ValidatorCondition
        {
            Validator = "in",
            Value = jsonValue,
            Message = "Environment must be one of: DEV, STAGING, PROD"
        };

        // Act
        var result1 = _validator.Validate("DEV", condition);    // exact match
        var result2 = _validator.Validate("dev", condition);    // lowercase - should match due to case-insensitive comparison
        var result3 = _validator.Validate("invalid", condition); // not in array

        // Assert
        result1.IsValid.Should().BeTrue();
        result1.Message.Should().BeEmpty();

        result2.IsValid.Should().BeTrue(); // Case-insensitive match
        result2.Message.Should().BeEmpty();

        result3.IsValid.Should().BeFalse();
        result3.Message.Should().Be("Environment must be one of: DEV, STAGING, PROD");
        result3.ValidatorType.Should().Be("in");
    }
}