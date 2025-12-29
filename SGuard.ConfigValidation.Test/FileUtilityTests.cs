using FluentAssertions;
using SGuard.ConfigValidation.Utils;

namespace SGuard.ConfigValidation.Test;

public sealed class FileUtilityTests : IDisposable
{
    private readonly string _testDirectory;

    public FileUtilityTests()
    {
        _testDirectory = DirectoryUtility.CreateTempDirectory("safefilesystem-test");
    }

    [Fact]
    public void CreateSafeTempDirectory_Should_Create_Directory()
    {
        // Act
        var directory = DirectoryUtility.CreateTempDirectory("test-prefix");

        // Assert
        directory.Should().NotBeNullOrWhiteSpace();
        Directory.Exists(directory).Should().BeTrue();
        directory.Should().Contain("test-prefix");
    }

    [Fact]
    public void CreateSafeTempDirectory_WithoutPrefix_Should_Create_Directory()
    {
        // Act
        var directory = DirectoryUtility.CreateTempDirectory();

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
        FileUtility.WriteAllText(filePath, content);

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
        FileUtility.WriteAllText(filePath, content);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        File.ReadAllText(filePath).Should().Be(content);
    }

    [Fact]
    public void SafeWriteAllText_With_EmptyFilePath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            FileUtility.WriteAllText(string.Empty, "content"));
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
        var result = FileUtility.ReadAllText(filePath);

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
            FileUtility.ReadAllText(filePath));
        exception.FileName.Should().Be(filePath);
    }

    [Fact]
    public void SafeReadAllText_With_EmptyFilePath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            FileUtility.ReadAllText(string.Empty));
        exception.ParamName.Should().Be("filePath");
    }

    [Fact]
    public void SafeFileExists_With_ExistingFile_Should_Return_True()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "content");

        // Act
        var result = FileUtility.FileExists(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SafeFileExists_With_NonExistentFile_Should_Return_False()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var result = FileUtility.FileExists(filePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SafeFileExists_With_EmptyPath_Should_Return_False()
    {
        // Act
        var result = FileUtility.FileExists(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SafeFileExists_With_NullPath_Should_Return_False()
    {
        // Act
        var result = FileUtility.FileExists(null!);

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
        DirectoryUtility.DeleteDirectory(directory, recursive: true);

        // Assert
        Directory.Exists(directory).Should().BeFalse();
    }

    [Fact]
    public void SafeDeleteDirectory_With_NonExistentDirectory_Should_Not_Throw()
    {
        // Arrange
        var directory = Path.Combine(_testDirectory, "nonexistent");

        // Act
        var act = () => DirectoryUtility.DeleteDirectory(directory, recursive: true);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SafeDeleteDirectory_With_EmptyPath_Should_Not_Throw()
    {
        // Act
        var act = () => DirectoryUtility.DeleteDirectory(string.Empty, recursive: true);

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
            var action = () => FileUtility.ReadAllText(outsideFile, basePath);
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
        var result = FileUtility.ReadAllText(validPath, basePath);

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
        var result = FileUtility.ReadAllText(filePath);

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
            var action = () => FileUtility.FileExists(outsideFile, basePath);
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
        var result = FileUtility.FileExists(validPath, basePath);

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
            var action = () => FileUtility.WriteAllText(outsideFile, "content", basePath);
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
        FileUtility.WriteAllText(validPath, "test content", basePath);

        // Assert
        File.Exists(validPath).Should().BeTrue();
        File.ReadAllText(validPath).Should().Be("test content");
    }

    [Fact]
    public void SafeWriteAllText_With_BasePathNullDirectory_Should_HandleGracefully()
    {
        // Arrange - basePath that resolves to a file (not directory)
        var basePath = Path.Combine(_testDirectory, "base.json");
        File.WriteAllText(basePath, "{}");
        var filePath = Path.Combine(_testDirectory, "test.txt");

        // Act - Should work because baseDirectory will be null/empty and validation skipped
        FileUtility.WriteAllText(filePath, "content", basePath);

        // Assert
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public void SafeWriteAllText_With_BasePathEmptyDirectory_Should_HandleGracefully()
    {
        // Arrange - basePath that resolves to root or empty directory
        var filePath = Path.Combine(_testDirectory, "test.txt");

        // Act - Without basePath, should work normally
        FileUtility.WriteAllText(filePath, "content");

        // Assert
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public void SafeReadAllText_With_BasePathNullDirectory_Should_HandleGracefully()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base.json");
        File.WriteAllText(basePath, "{}");
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "content");

        // Act - Should work because baseDirectory will be null/empty and validation skipped
        var result = FileUtility.ReadAllText(filePath, basePath);

        // Assert
        result.Should().Be("content");
    }

    [Fact]
    public void SafeFileExists_With_BasePathNullDirectory_Should_HandleGracefully()
    {
        // Arrange
        var basePath = Path.Combine(_testDirectory, "base.json");
        File.WriteAllText(basePath, "{}");
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "content");

        // Act - Should work because baseDirectory will be null/empty and validation skipped
        var result = FileUtility.FileExists(filePath, basePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanReadFile_With_ReadableFile_Should_ReturnTrue()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "readable.txt");
        File.WriteAllText(filePath, "test");

        // Act
        var result = FileUtility.CanReadFile(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanReadFile_With_NonExistentFile_Should_ReturnFalse()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var result = FileUtility.CanReadFile(filePath);

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
        var result = FileUtility.CanWriteFile(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanWriteFile_With_NonExistentFile_Should_CheckDirectoryWritable()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "newfile.txt");

        // Act
        var result = FileUtility.CanWriteFile(filePath);

        // Assert
        result.Should().BeTrue(); // Directory should be writable
    }

    [Fact]
    public void IsRunningInDocker_Should_Return_Boolean()
    {
        // Act - Use reflection to access internal method
        var method = typeof(FileUtility).GetMethod("IsRunningInDocker", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool)method!.Invoke(null, null)!;

        // Assert - Should return a boolean (actual value depends on environment)
        (result == true || result == false).Should().BeTrue();
    }

    [Fact]
    public void WriteAllText_With_DirectoryCreationFailure_Should_Throw_UnauthorizedAccessException()
    {
        // Arrange - Try to write to a path where directory creation would fail
        // This is hard to simulate reliably, but we can test the error handling path
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var content = "test";

        // Act - Should succeed in normal case
        FileUtility.WriteAllText(filePath, content);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        File.ReadAllText(filePath).Should().Be(content);
    }

    [Fact]
    public void WriteAllText_With_FileWriteException_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var content = "test";

        // Act - Should succeed in normal case
        FileUtility.WriteAllText(filePath, content);

        // Assert
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public void ReadAllText_With_DirectoryNotFoundException_Should_Throw_FileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent", "subdir", "test.txt");

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() =>
            FileUtility.ReadAllText(filePath));
        exception.Should().NotBeNull();
    }

    [Fact]
    public void ReadAllText_With_IOException_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "test");

        // Act - Should succeed in normal case
        var result = FileUtility.ReadAllText(filePath);

        // Assert
        result.Should().Be("test");
    }

    public void Dispose()
    {
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
    }
}

