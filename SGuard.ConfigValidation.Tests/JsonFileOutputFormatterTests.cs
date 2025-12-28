using FluentAssertions;
using SGuard.ConfigValidation.Output;
using SGuard.ConfigValidation.Results;
using SGuard.ConfigValidation.Utils;

namespace SGuard.ConfigValidation.Tests;

public sealed class JsonFileOutputFormatterTests : IDisposable
{
    private readonly string _testDirectory;

    public JsonFileOutputFormatterTests()
    {
        _testDirectory = DirectoryUtility.CreateTempDirectory("jsonfileformatter-test");
    }

    [Fact]
    public void Constructor_With_NullFilePath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new JsonFileOutputFormatter(null!));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_With_EmptyFilePath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new JsonFileOutputFormatter(""));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_With_WhitespaceFilePath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new JsonFileOutputFormatter("   "));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public void FormatAsync_With_NullResult_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "output.json");
        var formatter = new JsonFileOutputFormatter(filePath);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            formatter.FormatAsync(null!).GetAwaiter().GetResult());
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task FormatAsync_With_SuccessResult_Should_Write_JsonFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "output.json");
        var formatter = new JsonFileOutputFormatter(filePath);
        var result = RuleEngineResult.CreateSuccess(new FileValidationResult("test.json", []));

        // Act
        await formatter.FormatAsync(result);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = File.ReadAllText(filePath);
        content.Should().NotBeNullOrWhiteSpace();
        content.Should().Contain("\"success\": true");
        content.Should().Contain("\"isValid\": true");
    }

    [Fact]
    public async Task FormatAsync_With_ErrorResult_Should_Write_JsonFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "output.json");
        var formatter = new JsonFileOutputFormatter(filePath);
        var result = RuleEngineResult.CreateError("Test error");

        // Act
        await formatter.FormatAsync(result);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = File.ReadAllText(filePath);
        content.Should().NotBeNullOrWhiteSpace();
        content.Should().Contain("\"success\": false");
        content.Should().Contain("Test error");
    }

    [Fact]
    public async Task FormatAsync_With_MultipleResults_Should_Write_All()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "output.json");
        var formatter = new JsonFileOutputFormatter(filePath);
        var results = new List<FileValidationResult>
        {
            new FileValidationResult("file1.json", []),
            new FileValidationResult("file2.json", [])
        };
        var result = RuleEngineResult.CreateSuccess(results);

        // Act
        await formatter.FormatAsync(result);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = File.ReadAllText(filePath);
        content.Should().Contain("file1.json");
        content.Should().Contain("file2.json");
    }

    [Fact]
    public async Task FormatAsync_With_NonExistentDirectory_Should_Create_Directory()
    {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "subdir");
        var filePath = Path.Combine(subDir, "output.json");
        var formatter = new JsonFileOutputFormatter(filePath);
        var result = RuleEngineResult.CreateSuccess(new FileValidationResult("test.json", []));

        // Act
        await formatter.FormatAsync(result);

        // Assert
        Directory.Exists(subDir).Should().BeTrue();
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task FormatAsync_With_ValidationErrors_Should_Include_Errors()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "output.json");
        var formatter = new JsonFileOutputFormatter(filePath);
        var validationResults = new List<ValidationResult>
        {
            ValidationResult.Failure("Error 1", "validator1", "key1", "value1"),
            ValidationResult.Failure("Error 2", "validator2", "key2", "value2")
        };
        var fileResult = new FileValidationResult("test.json", validationResults);
        var result = RuleEngineResult.CreateSuccess(fileResult);

        // Act
        await formatter.FormatAsync(result);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = File.ReadAllText(filePath);
        content.Should().Contain("Error 1");
        content.Should().Contain("Error 2");
        content.Should().Contain("key1");
        content.Should().Contain("key2");
    }

    public void Dispose()
    {
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
    }
}

