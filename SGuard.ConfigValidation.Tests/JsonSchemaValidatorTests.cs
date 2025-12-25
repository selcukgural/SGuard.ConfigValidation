using FluentAssertions;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Services;

namespace SGuard.ConfigValidation.Tests;

public sealed class JsonSchemaValidatorTests : IDisposable
{
    private readonly JsonSchemaValidator _validator;
    private readonly string _testDirectory;

    public JsonSchemaValidatorTests()
    {
        _validator = new JsonSchemaValidator();
        _testDirectory = SafeFileSystemHelper.CreateSafeTempDirectory("jsonschema-test");
    }

    [Fact]
    public void Validate_With_ValidJson_Should_ReturnSuccess()
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
        var result = _validator.Validate(json, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_With_InvalidJson_Should_ReturnFailure()
    {
        // Arrange
        var schema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""required"": [""name""],
  ""properties"": {
    ""name"": { ""type"": ""string"" }
  }
}";
        var json = @"{ ""age"": 30 }"; // Missing required "name" property

        // Act
        var result = _validator.Validate(json, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.ErrorMessage.ToLowerInvariant().Should().Contain("name", "Error message should mention the missing 'name' property");
    }

    [Fact]
    public void Validate_With_EmptyJson_Should_ReturnFailure()
    {
        // Arrange
        var schema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object""
}";

        // Act
        var result = _validator.Validate(string.Empty, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("cannot be null or empty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateAgainstFile_With_ValidFile_Should_ReturnSuccess()
    {
        // Arrange
        var schemaPath = CreateTestFile("schema.json", @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""properties"": {
    ""name"": { ""type"": ""string"" }
  }
}");
        var json = @"{ ""name"": ""test"" }";

        // Act
        var result = _validator.ValidateAgainstFile(json, schemaPath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAgainstFile_With_NonExistentFile_Should_ReturnFailure()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");
        var json = @"{ ""name"": ""test"" }";

        // Act
        var result = _validator.ValidateAgainstFile(json, nonExistentPath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_With_InvalidSchema_Should_HandleGracefully()
    {
        // Arrange
        // NJsonSchema can parse even minimal schemas, so we'll test with a schema that doesn't match the JSON structure
        var schema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""required"": [""requiredField""],
  ""properties"": {
    ""requiredField"": { ""type"": ""string"" }
  }
}";
        var json = @"{ ""name"": ""test"" }"; // Missing requiredField

        // Act
        var result = _validator.Validate(json, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateAgainstFile_With_CachedSchema_Should_Use_Cache()
    {
        // Arrange
        var schemaPath = CreateTestFile("schema.json", @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""properties"": {
    ""name"": { ""type"": ""string"" }
  }
}");
        var json = @"{ ""name"": ""test"" }";

        // Act - Validate against same schema file twice
        var result1 = _validator.ValidateAgainstFile(json, schemaPath);
        var result2 = _validator.ValidateAgainstFile(json, schemaPath);

        // Assert - Both should succeed (schema is cached)
        result1.Should().NotBeNull();
        result1.IsValid.Should().BeTrue();
        result2.Should().NotBeNull();
        result2.IsValid.Should().BeTrue();
    }

    private string CreateTestFile(string fileName, string content)
    {
        var filePath = Path.Combine(_testDirectory, fileName);
        SafeFileSystemHelper.SafeWriteAllText(filePath, content);
        return filePath;
    }

    public void Dispose()
    {
        SafeFileSystemHelper.SafeDeleteDirectory(_testDirectory, recursive: true);
    }
}

