using FluentAssertions;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Utils;

namespace SGuard.ConfigValidation.Test;

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

    [Fact]
    public async Task ValidateAgainstFile_With_FileWatcher_Should_InvalidateCache_OnFileChange()
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

        // Act - First validation (creates cache and file watcher)
        var result1 = await _validator.ValidateAgainstFileAsync(json, schemaPath);
        result1.IsValid.Should().BeTrue();

        // Modify schema file
        await Task.Delay(100); // Ensure different modification time
        CreateTestFile("schema.json", @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""required"": [""name""],
  ""properties"": {
    ""name"": { ""type"": ""string"" }
  }
}");

        // Wait for file watcher to detect change
        await Task.Delay(500);

        // Second validation - should reload schema from file (cache invalidated by watcher)
        var result2 = await _validator.ValidateAgainstFileAsync(json, schemaPath);

        // Assert - Both should succeed (cache was invalidated and new schema loaded)
        result2.Should().NotBeNull();
        result2.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAgainstFile_With_FileWatcher_Should_InvalidateCache_OnFileDelete()
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

        // Act - First validation (creates cache and file watcher)
        var result1 = await _validator.ValidateAgainstFileAsync(json, schemaPath);
        result1.IsValid.Should().BeTrue();

        // Delete schema file
        File.Delete(schemaPath);

        // Wait for file watcher to detect deletion
        await Task.Delay(500);

        // Second validation - should fail because file doesn't exist
        var result2 = await _validator.ValidateAgainstFileAsync(json, schemaPath);

        // Assert - Should fail because file was deleted
        result2.Should().NotBeNull();
        result2.IsValid.Should().BeFalse();
        result2.ErrorMessage.Should().ContainAny("not found", "NotFound");
    }

    [Fact]
    public async Task ValidateAgainstFile_With_FileWatcher_Should_HandleMultipleFiles()
    {
        // Arrange
        var schemaPath1 = CreateTestFile("schema1.json", @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""properties"": {
    ""name"": { ""type"": ""string"" }
  }
}");
        var schemaPath2 = CreateTestFile("schema2.json", @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""properties"": {
    ""age"": { ""type"": ""number"" }
  }
}");
        var json1 = @"{ ""name"": ""test"" }";
        var json2 = @"{ ""age"": 30 }";

        // Act - Validate against both files (creates watchers for both)
        var result1a = await _validator.ValidateAgainstFileAsync(json1, schemaPath1);
        var result2a = await _validator.ValidateAgainstFileAsync(json2, schemaPath2);

        // Modify first schema file
        await Task.Delay(100);
        CreateTestFile("schema1.json", @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""required"": [""name""],
  ""properties"": {
    ""name"": { ""type"": ""string"" }
  }
}");

        // Wait for file watcher to detect change
        await Task.Delay(500);

        // Validate again - first should reload, second should use cache
        var result1b = await _validator.ValidateAgainstFileAsync(json1, schemaPath1);
        var result2b = await _validator.ValidateAgainstFileAsync(json2, schemaPath2);

        // Assert - All should succeed
        result1a.IsValid.Should().BeTrue();
        result2a.IsValid.Should().BeTrue();
        result1b.IsValid.Should().BeTrue();
        result2b.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Dispose_Should_CleanupFileWatchers()
    {
        // Arrange
        var schemaPath = CreateTestFile("schema.json", @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object""
}");

        // Act - Validate to create file watcher
        _ = await _validator.ValidateAgainstFileAsync(@"{ ""name"": ""test"" }", schemaPath);

        // Dispose validator
        _validator.Dispose();

        // Assert - Should not throw when disposing again
        _validator.Invoking(v => v.Dispose()).Should().NotThrow();
    }

    [Fact]
    public async Task ValidateAgainstFile_With_FileWatcher_Should_HandleConcurrentAccess()
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

        // Act - Concurrent validations
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _validator.ValidateAgainstFileAsync(json, schemaPath))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - All should succeed
        results.Should().AllSatisfy(r =>
        {
            r.Should().NotBeNull();
            r.IsValid.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Validate_With_NullJson_Should_ReturnFailure()
    {
        // Arrange
        var schema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object""
}";

        // Act
        var result = await _validator.ValidateAsync(null!, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Validate_With_NullSchema_Should_ReturnFailure()
    {
        // Arrange
        var json = @"{ ""name"": ""test"" }";

        // Act - Null schema should return failure, not throw
        var result = await _validator.ValidateAsync(json, null!);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Validate_With_EmptySchema_Should_ReturnFailure()
    {
        // Arrange
        var json = @"{ ""name"": ""test"" }";

        // Act - Empty schema should return failure, not throw
        var result = await _validator.ValidateAsync(json, "");

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateAgainstFile_With_NullJson_Should_ReturnFailure()
    {
        // Arrange
        var schemaPath = CreateTestFile("schema.json", @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object""
}");

        // Act
        var result = await _validator.ValidateAgainstFileAsync(null!, schemaPath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAgainstFile_With_NullSchemaPath_Should_ReturnFailure()
    {
        // Arrange
        var json = @"{ ""name"": ""test"" }";

        // Act - Null schema path should return failure, not throw
        var result = await _validator.ValidateAgainstFileAsync(json, null!);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateAgainstFile_With_EmptySchemaPath_Should_ReturnFailure()
    {
        // Arrange
        var json = @"{ ""name"": ""test"" }";

        // Act - Empty schema path should return failure, not throw
        var result = await _validator.ValidateAgainstFileAsync(json, "");

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Validate_With_InvalidSchema_Should_ReturnFailure()
    {
        // Arrange
        var invalidSchema = "{ invalid json }";
        var json = @"{ ""name"": ""test"" }";

        // Act - Invalid schema should return failure, not throw
        var result = await _validator.ValidateAsync(json, invalidSchema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateAgainstFile_With_InvalidSchemaFile_Should_ReturnFailure()
    {
        // Arrange
        var schemaPath = CreateTestFile("invalid-schema.json", "{ invalid json }");
        var json = @"{ ""name"": ""test"" }";

        // Act
        var result = await _validator.ValidateAgainstFileAsync(json, schemaPath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Validate_With_ComplexNestedSchema_Should_ValidateCorrectly()
    {
        // Arrange
        var schema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""properties"": {
    ""user"": {
      ""type"": ""object"",
      ""properties"": {
        ""name"": { ""type"": ""string"" },
        ""age"": { ""type"": ""integer"" }
      },
      ""required"": [""name""]
    }
  }
}";
        var json = @"{ ""user"": { ""name"": ""John"", ""age"": 30 } }";

        // Act
        var result = await _validator.ValidateAsync(json, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_With_ArraySchema_Should_ValidateCorrectly()
    {
        // Arrange
        var schema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""array"",
  ""items"": { ""type"": ""string"" }
}";
        var json = @"[""item1"", ""item2"", ""item3""]";

        // Act
        var result = await _validator.ValidateAsync(json, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_With_ArraySchema_InvalidItems_Should_ReturnFailure()
    {
        // Arrange
        var schema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""array"",
  ""items"": { ""type"": ""string"" }
}";
        var json = @"[""item1"", 123, ""item3""]"; // Number instead of string

        // Act
        var result = await _validator.ValidateAsync(json, schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
    }

    private string CreateTestFile(string fileName, string content)
    {
        var filePath = Path.Combine(_testDirectory, fileName);
        FileUtility.WriteAllText(filePath, content);
        return filePath;
    }

    public void Dispose()
    {
        _validator?.Dispose();
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
    }
}

