using FluentAssertions;
using System.Text.Json;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Test.Validators;

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
        result.Message.Should().Contain("Environment must be one of: dev, staging, prod");
        result.Message.Should().Contain("Actual value:");
        result.Message.Should().Contain("one of:");
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
        result2.Message.Should().Contain("Base URL must be one of the allowed values");
        result2.Message.Should().Contain("Actual value:");
        result2.Message.Should().Contain("one of:");
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
        result3.Message.Should().Contain("Environment must be one of: DEV, STAGING, PROD");
        result3.Message.Should().Contain("Actual value:");
        result3.Message.Should().Contain("one of:");
        result3.ValidatorType.Should().Be("in");
    }

    [Fact]
    public void Validate_With_NullValue_Should_Fail()
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
        var result = _validator.Validate(null, condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Environment must be one of: dev, staging, prod");
        result.Message.Should().Contain("one of:");
    }

    [Fact]
    public void Validate_With_EmptyArray_Should_Fail()
    {
        // Arrange
        var jsonString = """[]""";
        var jsonValue = JsonSerializer.Deserialize<JsonElement>(jsonString);
        var condition = new ValidatorCondition
        {
            Validator = "in",
            Value = jsonValue,
            Message = "Value must be in array"
        };

        // Act
        var result = _validator.Validate("test", condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Value must be in array");
        result.Message.Should().Contain("one of:");
    }

    [Fact]
    public void Validate_With_NonArrayConditionValue_Should_Fail_WithProperError()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "in",
            Value = "not an array",
            Message = "Value must be in array"
        };

        // Act
        var result = _validator.Validate("test", condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Validation failed: Value for 'in' validator must be an array");
        result.Message.Should().Contain("Actual value type:");
        result.Message.Should().Contain("Please provide an array of allowed values");
        result.ValidatorType.Should().Be("in");
    }

    [Fact]
    public void Validate_With_NullConditionValue_Should_Fail_WithProperError()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "in",
            Value = null,
            Message = "Value must be in array"
        };

        // Act
        var result = _validator.Validate("test", condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Validation failed: Value for 'in' validator must be an array");
        result.Message.Should().Contain("Actual value type: null");
        result.Message.Should().Contain("Please provide an array of allowed values");
        result.ValidatorType.Should().Be("in");
    }

    [Fact]
    public void Validate_With_JsonElementArrayContainingNumbers_Should_ConvertToString()
    {
        // Arrange
        var jsonString = """[1, 2, 3]""";
        var jsonValue = JsonSerializer.Deserialize<JsonElement>(jsonString);
        var condition = new ValidatorCondition
        {
            Validator = "in",
            Value = jsonValue,
            Message = "Value must be in array"
        };

        // Act
        var result1 = _validator.Validate("1", condition);
        var result2 = _validator.Validate("2", condition);
        var result3 = _validator.Validate("4", condition);

        // Assert
        result1.IsValid.Should().BeTrue();
        result1.Message.Should().BeEmpty();

        result2.IsValid.Should().BeTrue();
        result2.Message.Should().BeEmpty();

        result3.IsValid.Should().BeFalse();
        result3.Message.Should().Contain("Value must be in array");
    }

    [Fact]
    public void Validate_With_JsonElementArrayContainingBooleans_Should_ConvertToString()
    {
        // Arrange
        var jsonString = """[true, false]""";
        var jsonValue = JsonSerializer.Deserialize<JsonElement>(jsonString);
        var condition = new ValidatorCondition
        {
            Validator = "in",
            Value = jsonValue,
            Message = "Value must be in array"
        };

        // Act
        var result1 = _validator.Validate("True", condition);
        var result2 = _validator.Validate("False", condition);
        var result3 = _validator.Validate("invalid", condition);

        // Assert
        result1.IsValid.Should().BeTrue();
        result1.Message.Should().BeEmpty();

        result2.IsValid.Should().BeTrue();
        result2.Message.Should().BeEmpty();

        result3.IsValid.Should().BeFalse();
        result3.Message.Should().Contain("Value must be in array");
    }

    [Fact]
    public void Validate_With_JsonElementArrayContainingMixedTypes_Should_ConvertToString()
    {
        // Arrange
        var jsonString = """["string", 42, true, null]""";
        var jsonValue = JsonSerializer.Deserialize<JsonElement>(jsonString);
        var condition = new ValidatorCondition
        {
            Validator = "in",
            Value = jsonValue,
            Message = "Value must be in array"
        };

        // Act
        var result1 = _validator.Validate("string", condition);
        var result2 = _validator.Validate("42", condition);
        var result3 = _validator.Validate("True", condition);
        var result4 = _validator.Validate("", condition); // null becomes empty string

        // Assert
        result1.IsValid.Should().BeTrue();
        result1.Message.Should().BeEmpty();

        result2.IsValid.Should().BeTrue();
        result2.Message.Should().BeEmpty();

        result3.IsValid.Should().BeTrue();
        result3.Message.Should().BeEmpty();

        result4.IsValid.Should().BeTrue(); // null JsonElement.ToString() returns empty string
        result4.Message.Should().BeEmpty();
    }

    [Fact]
    public void Validate_With_StringArrayConditionValue_Should_Work()
    {
        // Arrange
        var condition = new ValidatorCondition
        {
            Validator = "in",
            Value = new[] { "dev", "staging", "prod" },
            Message = "Environment must be one of: dev, staging, prod"
        };

        // Act
        var result1 = _validator.Validate("dev", condition);
        var result2 = _validator.Validate("staging", condition);
        var result3 = _validator.Validate("invalid", condition);

        // Assert
        result1.IsValid.Should().BeTrue();
        result1.Message.Should().BeEmpty();

        result2.IsValid.Should().BeTrue();
        result2.Message.Should().BeEmpty();

        result3.IsValid.Should().BeFalse();
        result3.Message.Should().Contain("Environment must be one of: dev, staging, prod");
        result3.Message.Should().Contain("one of:");
    }

    [Fact]
    public void Validate_With_JsonElementObject_Should_Fail_WithProperError()
    {
        // Arrange
        var jsonString = """{"key": "value"}""";
        var jsonValue = JsonSerializer.Deserialize<JsonElement>(jsonString);
        var condition = new ValidatorCondition
        {
            Validator = "in",
            Value = jsonValue,
            Message = "Value must be in array"
        };

        // Act
        var result = _validator.Validate("test", condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Validation failed: Value for 'in' validator must be an array");
        result.Message.Should().Contain("Actual value type:");
        result.Message.Should().Contain("Please provide an array of allowed values");
        result.ValidatorType.Should().Be("in");
    }

    [Fact]
    public void Validate_With_JsonElementString_Should_Fail_WithProperError()
    {
        // Arrange
        var jsonString = "\"test\"";
        var jsonValue = JsonSerializer.Deserialize<JsonElement>(jsonString);
        var condition = new ValidatorCondition
        {
            Validator = "in",
            Value = jsonValue,
            Message = "Value must be in array"
        };

        // Act
        var result = _validator.Validate("test", condition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Validation failed: Value for 'in' validator must be an array");
        result.Message.Should().Contain("Actual value type:");
        result.Message.Should().Contain("Please provide an array of allowed values");
        result.ValidatorType.Should().Be("in");
    }
}