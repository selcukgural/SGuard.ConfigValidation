using FluentAssertions;
using SGuard.ConfigValidation.Utilities;

namespace SGuard.ConfigValidation.Test;

public sealed class FileUtilityAdditionalTests : IDisposable
{
    private readonly string _testDirectory;

    public FileUtilityAdditionalTests()
    {
        _testDirectory = DirectoryUtility.CreateTempDirectory("fileutility-additional-test");
    }

    [Fact]
    public void ReadAllTextAsync_With_NullFilePath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(() =>
            FileUtility.ReadAllTextAsync(null!));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public void ReadAllTextAsync_With_EmptyFilePath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(() =>
            FileUtility.ReadAllTextAsync(""));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public void ReadAllTextAsync_With_NonExistentFile_Should_Throw_FileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act & Assert
        var exception = Assert.ThrowsAsync<FileNotFoundException>(() =>
            FileUtility.ReadAllTextAsync(nonExistentPath));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task ReadAllTextAsync_With_ValidFile_Should_Return_Content()
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
    public async Task ReadAllTextAsync_With_CancellationToken_Should_Respect_Cancellation()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "Test");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            FileUtility.ReadAllTextAsync(filePath, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ReadAllTextAsync_With_BasePath_Should_ValidatePathTraversal()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base.json");
        File.WriteAllText(basePath, "{}");
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "Test");
        var outsidePath = Path.Combine(Path.GetDirectoryName(_testDirectory)!, "outside.txt");
        File.WriteAllText(outsidePath, "Outside");

        try
        {
            // Act & Assert - Should throw UnauthorizedAccessException for path traversal
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                FileUtility.ReadAllTextAsync(outsidePath, basePath: basePath));
        }
        finally
        {
            try { File.Delete(outsidePath); } catch { }
        }
    }

    [Fact]
    public async Task ReadAllTextAsync_With_ValidBasePath_Should_ReadFile()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base.json");
        File.WriteAllText(basePath, "{}");
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var content = "Test content";
        File.WriteAllText(filePath, content);

        // Act
        var result = await FileUtility.ReadAllTextAsync(filePath, basePath: basePath);

        // Assert
        result.Should().Be(content);
    }

    [Fact]
    public async Task ReadAllTextAsync_With_DirectoryNotFoundException_Should_Throw_FileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent", "test.txt");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            FileUtility.ReadAllTextAsync(filePath));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public void FileExists_With_NullFilePath_Should_ReturnFalse()
    {
        // Act
        var result = FileUtility.FileExists(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FileExists_With_EmptyFilePath_Should_ReturnFalse()
    {
        // Act
        var result = FileUtility.FileExists("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FileExists_With_ExistingFile_Should_ReturnTrue()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "Test");

        // Act
        var result = FileUtility.FileExists(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void FileExists_With_NonExistentFile_Should_ReturnFalse()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var result = FileUtility.FileExists(nonExistentPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanReadFile_With_NullFilePath_Should_ReturnFalse()
    {
        // Act
        var result = FileUtility.CanReadFile(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanReadFile_With_EmptyFilePath_Should_ReturnFalse()
    {
        // Act
        var result = FileUtility.CanReadFile("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanReadFile_With_NonExistentFile_Should_ReturnFalse()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var result = FileUtility.CanReadFile(nonExistentPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanReadFile_With_ExistingFile_Should_ReturnTrue()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "Test");

        // Act
        var result = FileUtility.CanReadFile(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanWriteFile_With_NullFilePath_Should_ReturnFalse()
    {
        // Act
        var result = FileUtility.CanWriteFile(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanWriteFile_With_EmptyFilePath_Should_ReturnFalse()
    {
        // Act
        var result = FileUtility.CanWriteFile("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanWriteFile_With_NonExistentFile_Should_CheckDirectory()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "newfile.txt");

        // Act
        var result = FileUtility.CanWriteFile(filePath);

        // Assert
        result.Should().BeTrue(); // Directory is writable
    }

    [Fact]
    public void CanWriteFile_With_ExistingFile_Should_CheckFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "Test");

        // Act
        var result = FileUtility.CanWriteFile(filePath);

        // Assert
        result.Should().BeTrue();
    }

    public void Dispose()
    {
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
    }
}

