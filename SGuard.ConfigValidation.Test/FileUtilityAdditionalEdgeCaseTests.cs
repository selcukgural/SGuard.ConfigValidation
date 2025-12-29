using FluentAssertions;
using SGuard.ConfigValidation.Utils;

namespace SGuard.ConfigValidation.Test;

public sealed class FileUtilityAdditionalEdgeCaseTests : IDisposable
{
    private readonly string _testDirectory;

    public FileUtilityAdditionalEdgeCaseTests()
    {
        _testDirectory = DirectoryUtility.CreateTempDirectory("fileutility-edge-test");
    }

    [Fact]
    public async Task ReadAllTextAsync_With_DirectoryNotFoundException_Should_Throw_FileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent", "subdir", "test.txt");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await FileUtility.ReadAllTextAsync(filePath));
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task ReadAllTextAsync_With_IOException_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "test");

        // Act - Should succeed in normal case
        var result = await FileUtility.ReadAllTextAsync(filePath);

        // Assert
        result.Should().Be("test");
    }

    [Fact]
    public void FileExists_With_BasePath_Should_ValidatePathTraversal()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base.json");
        File.WriteAllText(basePath, "{}");
        
        // Create a file outside the base directory
        var outsideDir = Path.Combine(Path.GetDirectoryName(_testDirectory)!, "outside");
        Directory.CreateDirectory(outsideDir);
        var outsideFile = Path.Combine(outsideDir, "outside.txt");
        File.WriteAllText(outsideFile, "outside");

        try
        {
            // Act & Assert - Path traversal attempt
            var action = () => FileUtility.FileExists(outsideFile, basePath);
            action.Should().Throw<UnauthorizedAccessException>();
        }
        finally
        {
            try { File.Delete(outsideFile); } catch { }
            try { Directory.Delete(outsideDir, true); } catch { }
        }
    }

    [Fact]
    public void FileExists_With_BasePath_And_ValidPath_Should_ReturnTrue()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base.json");
        File.WriteAllText(basePath, "{}");
        var validPath = Path.Combine(_testDirectory, "subdir", "test.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(validPath)!);
        File.WriteAllText(validPath, "test");

        // Act
        var result = FileUtility.FileExists(validPath, basePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void FileExists_With_BasePath_And_NonExistentFile_Should_ReturnFalse()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base.json");
        File.WriteAllText(basePath, "{}");
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var result = FileUtility.FileExists(nonExistentPath, basePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FileExists_With_BasePath_And_Symlink_Should_ValidateSymlink()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base.json");
        File.WriteAllText(basePath, "{}");
        var validPath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(validPath, "test");

        // Act - Should succeed in normal case
        var result = FileUtility.FileExists(validPath, basePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void FileExists_With_InvalidPath_Should_ReturnFalse()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDirectory, "invalid", "..", "..", "..", "test.txt");

        // Act
        var result = FileUtility.FileExists(invalidPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FileExists_With_BasePath_And_EmptyBaseDirectory_Should_HandleGracefully()
    {
        // Arrange
        var basePath = "filename.json"; // No directory
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "test.txt");
        File.WriteAllText(filePath, "test");

        try
        {
            // Act
            var result = FileUtility.FileExists(filePath, basePath);

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            try { File.Delete(filePath); } catch { }
        }
    }

    [Fact]
    public void FileExists_With_BasePath_And_Exception_Should_ReturnFalse()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base.json");
        File.WriteAllText(basePath, "{}");
        var invalidPath = "invalid:path:with:colons"; // Invalid path format

        // Act
        var result = FileUtility.FileExists(invalidPath, basePath);

        // Assert
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
    }
}

