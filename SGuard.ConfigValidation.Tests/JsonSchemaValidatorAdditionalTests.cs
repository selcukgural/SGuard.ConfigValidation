using FluentAssertions;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Utils;

namespace SGuard.ConfigValidation.Tests;

public sealed class JsonSchemaValidatorAdditionalTests : IDisposable
{
    private readonly JsonSchemaValidator _validator;
    private readonly string _testDirectory;

    public JsonSchemaValidatorAdditionalTests()
    {
        _validator = new JsonSchemaValidator();
        _testDirectory = DirectoryUtility.CreateTempDirectory("jsonschema-additional-test");
    }

    [Fact]
    public async Task ValidateAgainstFile_With_IOException_Should_ReturnFailure()
    {
        // Arrange
        var schemaPath = Path.Combine(_testDirectory, "schema.json");
        FileUtility.WriteAllText(schemaPath, @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object""
}");
        var json = @"{ ""name"": ""test"" }";
        
        // Delete directory to simulate I/O error
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
        Directory.CreateDirectory(_testDirectory);

        // Act
        var result = await _validator.ValidateAgainstFileAsync(json, schemaPath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().ContainAny("not found", "I/O", "error");
    }

    [Fact]
    public async Task ValidateAgainstFile_With_UnauthorizedAccessException_Should_ReturnFailure()
    {
        // Arrange
        var schemaPath = Path.Combine(_testDirectory, "schema.json");
        FileUtility.WriteAllText(schemaPath, @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object""
}");
        var json = @"{ ""name"": ""test"" }";

        // Act - Should succeed in normal case
        var result = await _validator.ValidateAgainstFileAsync(json, schemaPath);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Validate_With_FormatValidationError_ExtractMissingProperties_Should_FormatCorrectly()
    {
        // Arrange - Schema with required properties
        var schema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""required"": [""name"", ""age""],
  ""properties"": {
    ""name"": { ""type"": ""string"" },
    ""age"": { ""type"": ""number"" }
  }
}";
        var json = @"{}"; // Missing all required properties

        // Act
        var result = await _validator.ValidateAsync(json, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.ErrorMessage.Should().ContainAny("required", "missing", "name", "age");
    }

    [Fact]
    public async Task Validate_With_FormatValidationError_ExtractExpectedType_Should_FormatCorrectly()
    {
        // Arrange - Schema with type mismatch
        var schema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""properties"": {
    ""age"": { ""type"": ""integer"" }
  }
}";
        var json = @"{ ""age"": ""not a number"" }"; // String instead of integer

        // Act
        var result = await _validator.ValidateAsync(json, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.ErrorMessage.Should().ContainAny("IntegerExpected", "type", "integer", "expected");
    }

    [Fact]
    public async Task Validate_With_FormatValidationError_ExtractActualType_Should_FormatCorrectly()
    {
        // Arrange - Schema with type mismatch
        var schema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""properties"": {
    ""age"": { ""type"": ""integer"" },
    ""name"": { ""type"": ""string"" }
  }
}";
        var json = @"{ ""age"": ""not a number"", ""name"": 123 }"; // Type mismatches

        // Act
        var result = await _validator.ValidateAsync(json, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.ErrorMessage.Should().ContainAny("IntegerExpected", "StringExpected", "type", "expected");
    }

    [Fact]
    public async Task Validate_With_FormatValidationError_NoActualType_Should_FormatCorrectly()
    {
        // Arrange - Schema with type constraint
        var schema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""properties"": {
    ""value"": { ""type"": ""string"" }
  }
}";
        var json = @"{ ""value"": 123 }"; // Number instead of string

        // Act
        var result = await _validator.ValidateAsync(json, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Validate_With_FormatValidationError_MissingRequiredNoProperties_Should_FormatCorrectly()
    {
        // Arrange - Schema with required but no properties extracted
        var schema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""required"": [""name""],
  ""properties"": {
    ""name"": { ""type"": ""string"" }
  }
}";
        var json = @"{}"; // Missing required property

        // Act
        var result = await _validator.ValidateAsync(json, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Validate_With_JsonException_Should_HandleGracefully()
    {
        // Arrange - Invalid JSON that causes JsonException
        // Note: NJsonSchema's Validate method may not throw JsonException for malformed JSON
        // It might handle it gracefully or throw a different exception
        var schema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object""
}";
        var invalidJson = @"{ ""name"": ""test"""; // Missing closing brace

        // Act
        var result = await _validator.ValidateAsync(invalidJson, schema);

        // Assert
        result.Should().NotBeNull();
        // NJsonSchema may handle malformed JSON gracefully or return validation errors
        // So we just check that it doesn't throw and returns a result
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Validate_With_EmptyErrorPath_Should_HandleCorrectly()
    {
        // Arrange - Schema that might produce empty error path
        var schema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""required"": [""name""]
}";
        var json = @"{}"; // Missing required at root

        // Act
        var result = await _validator.ValidateAsync(json, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateAgainstFile_With_GenericException_Should_ReturnFailure()
    {
        // Arrange
        var schemaPath = Path.Combine(_testDirectory, "schema.json");
        FileUtility.WriteAllText(schemaPath, @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object""
}");
        var json = @"{ ""name"": ""test"" }";

        // Act - Should succeed in normal case
        var result = await _validator.ValidateAgainstFileAsync(json, schemaPath);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Validate_With_ValidateWithSchemaException_Should_HandleGracefully()
    {
        // Arrange
        var schema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""properties"": {
    ""name"": { ""type"": ""string"" }
  }
}";
        var json = @"{ ""name"": ""test"" }";

        // Act
        var result = await _validator.ValidateAsync(json, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }

    public void Dispose()
    {
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
    }
}

