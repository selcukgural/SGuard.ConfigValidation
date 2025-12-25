using FluentAssertions;
using SGuard.ConfigValidation.Common;

namespace SGuard.ConfigValidation.Tests;

public sealed class SafeFileSystemHelperTests : IDisposable
{
    private readonly string _testDirectory;

    public SafeFileSystemHelperTests()
    {
        _testDirectory = SafeFileSystemHelper.CreateSafeTempDirectory("safefilesystem-test");
    }

    [Fact]
    public void CreateSafeTempDirectory_Should_Create_Directory()
    {
        // Act
        var directory = SafeFileSystemHelper.CreateSafeTempDirectory("test-prefix");

        // Assert
        directory.Should().NotBeNullOrWhiteSpace();
        Directory.Exists(directory).Should().BeTrue();
        directory.Should().Contain("test-prefix");
    }

    [Fact]
    public void CreateSafeTempDirectory_WithoutPrefix_Should_Create_Directory()
    {
        // Act
        var directory = SafeFileSystemHelper.CreateSafeTempDirectory();

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
        SafeFileSystemHelper.SafeWriteAllText(filePath, content);

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
        SafeFileSystemHelper.SafeWriteAllText(filePath, content);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        File.ReadAllText(filePath).Should().Be(content);
    }

    [Fact]
    public void SafeWriteAllText_With_EmptyFilePath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            SafeFileSystemHelper.SafeWriteAllText(string.Empty, "content"));
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
        var result = SafeFileSystemHelper.SafeReadAllText(filePath);

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
            SafeFileSystemHelper.SafeReadAllText(filePath));
        exception.FileName.Should().Be(filePath);
    }

    [Fact]
    public void SafeReadAllText_With_EmptyFilePath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            SafeFileSystemHelper.SafeReadAllText(string.Empty));
        exception.ParamName.Should().Be("filePath");
    }

    [Fact]
    public void SafeFileExists_With_ExistingFile_Should_Return_True()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "content");

        // Act
        var result = SafeFileSystemHelper.SafeFileExists(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SafeFileExists_With_NonExistentFile_Should_Return_False()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var result = SafeFileSystemHelper.SafeFileExists(filePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SafeFileExists_With_EmptyPath_Should_Return_False()
    {
        // Act
        var result = SafeFileSystemHelper.SafeFileExists(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SafeFileExists_With_NullPath_Should_Return_False()
    {
        // Act
        var result = SafeFileSystemHelper.SafeFileExists(null!);

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
        SafeFileSystemHelper.SafeDeleteDirectory(directory, recursive: true);

        // Assert
        Directory.Exists(directory).Should().BeFalse();
    }

    [Fact]
    public void SafeDeleteDirectory_With_NonExistentDirectory_Should_Not_Throw()
    {
        // Arrange
        var directory = Path.Combine(_testDirectory, "nonexistent");

        // Act
        var act = () => SafeFileSystemHelper.SafeDeleteDirectory(directory, recursive: true);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SafeDeleteDirectory_With_EmptyPath_Should_Not_Throw()
    {
        // Act
        var act = () => SafeFileSystemHelper.SafeDeleteDirectory(string.Empty, recursive: true);

        // Assert
        act.Should().NotThrow();
    }

    public void Dispose()
    {
        SafeFileSystemHelper.SafeDeleteDirectory(_testDirectory, recursive: true);
    }
}

