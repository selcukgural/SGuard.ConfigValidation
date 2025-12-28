using FluentAssertions;
using SGuard.ConfigValidation.Utils;
using System.IO;

namespace SGuard.ConfigValidation.Tests;

public sealed class FileUtilityReadAllTextAsyncTests : IDisposable
{
    private readonly string _testDirectory;

    public FileUtilityReadAllTextAsyncTests()
    {
        _testDirectory = DirectoryUtility.CreateTempDirectory("fileutility-readalltextasync-test");
    }

    [Fact]
    public async Task ReadAllTextAsync_With_ValidFile_Should_ReturnContent()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var content = "Test content";
        File.WriteAllText(filePath, content);

        // Act
        var result = await FileUtility.ReadAllTextAsync(filePath);

        // Assert
        result.Should().Be(content);
    }

    [Fact]
    public async Task ReadAllTextAsync_With_NullPath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await FileUtility.ReadAllTextAsync(null!));
    }

    [Fact]
    public async Task ReadAllTextAsync_With_EmptyPath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await FileUtility.ReadAllTextAsync(""));
    }

    [Fact]
    public async Task ReadAllTextAsync_With_WhitespacePath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await FileUtility.ReadAllTextAsync("   "));
    }

    [Fact]
    public async Task ReadAllTextAsync_With_NonExistentFile_Should_Throw_FileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await FileUtility.ReadAllTextAsync(nonExistentPath));
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task ReadAllTextAsync_With_BasePath_Should_ValidatePathTraversal()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base");
        Directory.CreateDirectory(basePath);
        var filePath = Path.Combine(basePath, "test.txt");
        File.WriteAllText(filePath, "content");

        // Act
        var result = await FileUtility.ReadAllTextAsync(filePath, basePath);

        // Assert
        result.Should().Be("content");
    }

    [Fact]
    public async Task ReadAllTextAsync_With_PathTraversal_Should_Throw_UnauthorizedAccessException()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base", "base.json");
        Directory.CreateDirectory(Path.GetDirectoryName(basePath)!);
        File.WriteAllText(basePath, "{}");
        
        // Create a file outside the base directory
        var outsideDir = Path.Combine(Path.GetDirectoryName(_testDirectory)!, "outside");
        Directory.CreateDirectory(outsideDir);
        var outsideFile = Path.Combine(outsideDir, "outside.txt");
        File.WriteAllText(outsideFile, "outside");

        try
        {
            // Act & Assert - Path traversal attempt
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await FileUtility.ReadAllTextAsync(outsideFile, basePath));
        }
        finally
        {
            try { File.Delete(outsideFile); } catch { }
            try { Directory.Delete(outsideDir, true); } catch { }
        }
    }

    [Fact]
    public async Task ReadAllTextAsync_With_CancellationToken_Should_RespectCancellation()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "content");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - File.ReadAllTextAsync throws TaskCanceledException
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await FileUtility.ReadAllTextAsync(filePath, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ReadAllTextAsync_With_DirectoryNotFound_Should_Throw_FileNotFoundException()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_testDirectory, "nonexistent", "file.txt");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await FileUtility.ReadAllTextAsync(nonExistentDir));
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task ReadAllTextAsync_With_LargeFile_Should_ReadSuccessfully()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "large.txt");
        var content = new string('A', 10000);
        File.WriteAllText(filePath, content);

        // Act
        var result = await FileUtility.ReadAllTextAsync(filePath);

        // Assert
        result.Should().Be(content);
        result.Length.Should().Be(10000);
    }

    [Fact]
    public async Task ReadAllTextAsync_With_EmptyFile_Should_ReturnEmptyString()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "empty.txt");
        File.WriteAllText(filePath, "");

        // Act
        var result = await FileUtility.ReadAllTextAsync(filePath);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAllTextAsync_With_MultilineContent_Should_ReadSuccessfully()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "multiline.txt");
        var content = "Line 1\nLine 2\nLine 3";
        File.WriteAllText(filePath, content);

        // Act
        var result = await FileUtility.ReadAllTextAsync(filePath);

        // Assert
        result.Should().Be(content);
        result.Should().Contain("Line 1");
        result.Should().Contain("Line 2");
        result.Should().Contain("Line 3");
    }

    [Fact]
    public async Task ReadAllTextAsync_With_BasePathAndValidFile_Should_Work()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base");
        Directory.CreateDirectory(basePath);
        var subDir = Path.Combine(basePath, "sub");
        Directory.CreateDirectory(subDir);
        var filePath = Path.Combine(subDir, "test.txt");
        File.WriteAllText(filePath, "content");

        // Act
        var result = await FileUtility.ReadAllTextAsync(filePath, basePath);

        // Assert
        result.Should().Be("content");
    }

    public void Dispose()
    {
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
    }
}

