using FluentAssertions;
using SGuard.ConfigValidation.Common;

namespace SGuard.ConfigValidation.Tests;

public sealed class SafeFileSystemTests : IDisposable
{
    private readonly string _testDirectory;

    public SafeFileSystemTests()
    {
        _testDirectory = SafeFileSystem.CreateSafeTempDirectory("safefilesystem-test");
    }

    [Fact]
    public void CreateSafeTempDirectory_Should_Create_Directory()
    {
        // Act
        var directory = SafeFileSystem.CreateSafeTempDirectory("test-prefix");

        // Assert
        directory.Should().NotBeNullOrWhiteSpace();
        Directory.Exists(directory).Should().BeTrue();
        directory.Should().Contain("test-prefix");
    }

    [Fact]
    public void CreateSafeTempDirectory_WithoutPrefix_Should_Create_Directory()
    {
        // Act
        var directory = SafeFileSystem.CreateSafeTempDirectory();

        // Assert
        directory.Should().NotBeNullOrWhiteSpace();
        Directory.Exists(directory).Should().BeTrue();
    }

    [Fact]
    public void SafeWriteAllText_Should_Create_File_WithContent()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var content = "Test content";

        // Act
        SafeFileSystem.SafeWriteAllText(filePath, content);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        File.ReadAllText(filePath).Should().Be(content);
    }

    [Fact]
    public void SafeWriteAllText_Should_Create_ParentDirectories()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "subdir", "nested", "test.txt");
        var content = "Test content";

        // Act
        SafeFileSystem.SafeWriteAllText(filePath, content);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        File.ReadAllText(filePath).Should().Be(content);
    }

    [Fact]
    public void SafeWriteAllText_With_EmptyFilePath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            SafeFileSystem.SafeWriteAllText(string.Empty, "content"));
        exception.ParamName.Should().Be("filePath");
    }

    [Fact]
    public void SafeReadAllText_With_ExistingFile_Should_Return_Content()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var content = "Test content";
        File.WriteAllText(filePath, content);

        // Act
        var result = SafeFileSystem.SafeReadAllText(filePath);

        // Assert
        result.Should().Be(content);
    }

    [Fact]
    public void SafeReadAllText_With_NonExistentFile_Should_Throw_FileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() => 
            SafeFileSystem.SafeReadAllText(filePath));
        exception.FileName.Should().Be(filePath);
    }

    [Fact]
    public void SafeReadAllText_With_EmptyFilePath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            SafeFileSystem.SafeReadAllText(string.Empty));
        exception.ParamName.Should().Be("filePath");
    }

    [Fact]
    public void SafeFileExists_With_ExistingFile_Should_Return_True()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "content");

        // Act
        var result = SafeFileSystem.FileExists(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SafeFileExists_With_NonExistentFile_Should_Return_False()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var result = SafeFileSystem.FileExists(filePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SafeFileExists_With_EmptyPath_Should_Return_False()
    {
        // Act
        var result = SafeFileSystem.FileExists(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SafeFileExists_With_NullPath_Should_Return_False()
    {
        // Act
        var result = SafeFileSystem.FileExists(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SafeDeleteDirectory_With_ExistingDirectory_Should_Delete_Directory()
    {
        // Arrange
        var directory = Path.Combine(_testDirectory, "to-delete");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "test.txt"), "content");

        // Act
        SafeFileSystem.SafeDeleteDirectory(directory, recursive: true);

        // Assert
        Directory.Exists(directory).Should().BeFalse();
    }

    [Fact]
    public void SafeDeleteDirectory_With_NonExistentDirectory_Should_Not_Throw()
    {
        // Arrange
        var directory = Path.Combine(_testDirectory, "nonexistent");

        // Act
        var act = () => SafeFileSystem.SafeDeleteDirectory(directory, recursive: true);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SafeDeleteDirectory_With_EmptyPath_Should_Not_Throw()
    {
        // Act
        var act = () => SafeFileSystem.SafeDeleteDirectory(string.Empty, recursive: true);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SafeReadAllText_With_PathTraversal_Should_Throw_UnauthorizedAccessException()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base.json");
        File.WriteAllText(basePath, "{}");
        
        // Create a file outside the base directory using a safe temp location
        var outsideDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(outsideDir);
        var outsideFile = Path.Combine(outsideDir, "outside.txt");
        File.WriteAllText(outsideFile, "test");
        
        try
        {
            // Act & Assert - Using absolute path outside base directory
            var action = () => SafeFileSystem.SafeReadAllText(outsideFile, basePath);
            action.Should().Throw<UnauthorizedAccessException>()
                .WithMessage("*Path traversal*");
        }
        finally
        {
            // Cleanup
            try { File.Delete(outsideFile); } catch { }
            try { Directory.Delete(outsideDir, true); } catch { }
        }
    }

    [Fact]
    public void SafeReadAllText_With_ValidPathAndBasePath_Should_ReadSuccessfully()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base.json");
        File.WriteAllText(basePath, "{}");
        var validPath = Path.Combine(_testDirectory, "subdir", "test.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(validPath)!);
        File.WriteAllText(validPath, "test content");

        // Act
        var result = SafeFileSystem.SafeReadAllText(validPath, basePath);

        // Assert
        result.Should().Be("test content");
    }

    [Fact]
    public void SafeReadAllText_WithoutBasePath_Should_ReadSuccessfully()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "test content");

        // Act
        var result = SafeFileSystem.SafeReadAllText(filePath);

        // Assert
        result.Should().Be("test content");
    }

    [Fact]
    public void SafeFileExists_With_PathTraversal_Should_Throw_UnauthorizedAccessException()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base.json");
        File.WriteAllText(basePath, "{}");
        
        // Create a file outside the base directory using a safe temp location
        var outsideDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(outsideDir);
        var outsideFile = Path.Combine(outsideDir, "outside.txt");
        File.WriteAllText(outsideFile, "test");
        
        try
        {
            // Act & Assert - Using absolute path outside base directory
            var action = () => SafeFileSystem.FileExists(outsideFile, basePath);
            action.Should().Throw<UnauthorizedAccessException>()
                .WithMessage("*Path traversal*");
        }
        finally
        {
            // Cleanup
            try { File.Delete(outsideFile); } catch { }
            try { Directory.Delete(outsideDir, true); } catch { }
        }
    }

    [Fact]
    public void SafeFileExists_With_ValidPathAndBasePath_Should_ReturnTrue()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base.json");
        File.WriteAllText(basePath, "{}");
        var validPath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(validPath, "test");

        // Act
        var result = SafeFileSystem.FileExists(validPath, basePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SafeWriteAllText_With_PathTraversal_Should_Throw_UnauthorizedAccessException()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base.json");
        File.WriteAllText(basePath, "{}");
        
        // Create a directory outside the base directory using a safe temp location
        var outsideDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(outsideDir);
        var outsideFile = Path.Combine(outsideDir, "outside.txt");
        
        try
        {
            // Act & Assert - Using absolute path outside base directory
            var action = () => SafeFileSystem.SafeWriteAllText(outsideFile, "content", basePath);
            action.Should().Throw<UnauthorizedAccessException>()
                .WithMessage("*Path traversal*");
        }
        finally
        {
            // Cleanup
            try { File.Delete(outsideFile); } catch { }
            try { Directory.Delete(outsideDir, true); } catch { }
        }
    }

    [Fact]
    public void SafeWriteAllText_With_ValidPathAndBasePath_Should_WriteSuccessfully()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base.json");
        File.WriteAllText(basePath, "{}");
        var validPath = Path.Combine(_testDirectory, "subdir", "test.txt");

        // Act
        SafeFileSystem.SafeWriteAllText(validPath, "test content", basePath);

        // Assert
        File.Exists(validPath).Should().BeTrue();
        File.ReadAllText(validPath).Should().Be("test content");
    }

    [Fact]
    public void CanReadFile_With_ReadableFile_Should_ReturnTrue()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "readable.txt");
        File.WriteAllText(filePath, "test");

        // Act
        var result = SafeFileSystem.CanReadFile(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanReadFile_With_NonExistentFile_Should_ReturnFalse()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var result = SafeFileSystem.CanReadFile(filePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanWriteFile_With_WritableFile_Should_ReturnTrue()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "writable.txt");
        File.WriteAllText(filePath, "test");

        // Act
        var result = SafeFileSystem.CanWriteFile(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanWriteFile_With_NonExistentFile_Should_CheckDirectoryWritable()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "newfile.txt");

        // Act
        var result = SafeFileSystem.CanWriteFile(filePath);

        // Assert
        result.Should().BeTrue(); // Directory should be writable
    }

    public void Dispose()
    {
        SafeFileSystem.SafeDeleteDirectory(_testDirectory, recursive: true);
    }
}

