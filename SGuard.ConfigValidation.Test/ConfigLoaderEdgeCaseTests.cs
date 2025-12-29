using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Exceptions;
using SGuard.ConfigValidation.Security;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Utils;

namespace SGuard.ConfigValidation.Test;

public sealed class ConfigLoaderEdgeCaseTests : IDisposable
{
    private readonly ConfigLoader _loader;
    private readonly string _testDirectory;

    public ConfigLoaderEdgeCaseTests()
    {
        var logger = NullLogger<ConfigLoader>.Instance;
        var securityOptions = Options.Create(new SecurityOptions());
        _loader = new ConfigLoader(logger, securityOptions);
        _testDirectory = DirectoryUtility.CreateTempDirectory("configloader-edge-test");
    }

    private string CreateTestFile(string fileName, string content)
    {
        var filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    [Fact]
    public async Task LoadConfigAsync_With_NullPath_Should_Throw_ArgumentNullException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _loader.LoadConfigAsync(null!));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadConfigAsync_With_EmptyPath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _loader.LoadConfigAsync(""));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadConfigAsync_With_WhitespacePath_Should_Throw_ConfigurationException()
    {
        // Act & Assert - ArgumentException gets wrapped in ConfigurationException
        var exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            _loader.LoadConfigAsync("   "));
        
        exception.Should().NotBeNull();
        exception.InnerException.Should().BeOfType<ArgumentException>();
    }

    [Fact]
    public async Task LoadConfigAsync_With_NonExistentFile_Should_Throw_ConfigurationException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            _loader.LoadConfigAsync(nonExistentPath));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadConfigAsync_With_EmptyFile_Should_Throw_ConfigurationException()
    {
        // Arrange
        var emptyFilePath = CreateTestFile("empty.json", "");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            _loader.LoadConfigAsync(emptyFilePath));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadConfigAsync_With_InvalidJson_Should_Throw_ConfigurationException()
    {
        // Arrange
        var invalidJsonPath = CreateTestFile("invalid.json", "{ invalid json }");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            _loader.LoadConfigAsync(invalidJsonPath));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAppSettingsAsync_With_NullPath_Should_Throw_ArgumentNullException()
    {
        // Act & Assert - Path.GetFullPath throws ArgumentNullException for null
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _loader.LoadAppSettingsAsync(null!));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAppSettingsAsync_With_EmptyPath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _loader.LoadAppSettingsAsync(""));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAppSettingsAsync_With_WhitespacePath_Should_Throw_ConfigurationException()
    {
        // Act & Assert - ArgumentException gets wrapped in ConfigurationException
        var exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            _loader.LoadAppSettingsAsync("   "));
        
        exception.Should().NotBeNull();
        exception.InnerException.Should().BeOfType<ArgumentException>();
    }

    [Fact]
    public async Task LoadAppSettingsAsync_With_NonExistentFile_Should_Throw_ConfigurationException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            _loader.LoadAppSettingsAsync(nonExistentPath));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAppSettingsAsync_With_EmptyFile_Should_Return_EmptyDictionary()
    {
        // Arrange
        var emptyFilePath = CreateTestFile("empty.json", "");

        // Act
        var result = await _loader.LoadAppSettingsAsync(emptyFilePath);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAppSettingsAsync_With_InvalidJson_Should_Throw_ConfigurationException()
    {
        // Arrange
        var invalidJsonPath = CreateTestFile("invalid.json", "{ invalid json }");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            _loader.LoadAppSettingsAsync(invalidJsonPath));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAppSettingsAsync_With_EmptyJsonObject_Should_Return_EmptyDictionary()
    {
        // Arrange
        var emptyObjectPath = CreateTestFile("empty-object.json", "{}");

        // Act
        var result = await _loader.LoadAppSettingsAsync(emptyObjectPath);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAppSettingsAsync_With_NestedObjects_Should_Flatten_Keys()
    {
        // Arrange
        var nestedPath = CreateTestFile("nested.json", @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft"": ""Warning""
    }
  }
}");

        // Act
        var result = await _loader.LoadAppSettingsAsync(nestedPath);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("Logging:LogLevel:Default");
        result.Should().ContainKey("Logging:LogLevel:Microsoft");
        result["Logging:LogLevel:Default"].Should().Be("Information");
        result["Logging:LogLevel:Microsoft"].Should().Be("Warning");
    }

    [Fact]
    public async Task LoadAppSettingsAsync_With_Arrays_Should_Handle_Correctly()
    {
        // Arrange
        var arrayPath = CreateTestFile("array.json", @"{
  ""AllowedHosts"": [""localhost"", ""example.com""],
  ""Numbers"": [1, 2, 3]
}");

        // Act
        var result = await _loader.LoadAppSettingsAsync(arrayPath);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("AllowedHosts");
        result.Should().ContainKey("Numbers");
    }

    [Fact]
    public async Task LoadAppSettingsAsync_With_NullValues_Should_Handle_Correctly()
    {
        // Arrange
        var nullPath = CreateTestFile("null.json", @"{
  ""NullableKey"": null,
  ""NonNullKey"": ""value""
}");

        // Act
        var result = await _loader.LoadAppSettingsAsync(nullPath);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("NullableKey");
        result.Should().ContainKey("NonNullKey");
    }

    [Fact]
    public async Task LoadConfigAsync_With_CancellationToken_Should_Respect_Cancellation()
    {
        // Arrange
        var configPath = CreateTestFile("config.json", @"{
  ""version"": ""1"",
  ""environments"": [],
  ""rules"": []
}");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - FileUtility.ReadAllTextAsync throws TaskCanceledException which gets wrapped in ConfigurationException
        var exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            _loader.LoadConfigAsync(configPath, cts.Token));
        exception.InnerException.Should().BeOfType<TaskCanceledException>();
    }

    [Fact]
    public async Task LoadAppSettingsAsync_With_CancellationToken_Should_Respect_Cancellation()
    {
        // Arrange
        var appSettingsPath = CreateTestFile("appsettings.json", "{}");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - ConfigLoader wraps TaskCanceledException in ConfigurationException
        var exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            _loader.LoadAppSettingsAsync(appSettingsPath, cts.Token));
        exception.InnerException.Should().BeOfType<TaskCanceledException>();
    }

    public void Dispose()
    {
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
    }
}

