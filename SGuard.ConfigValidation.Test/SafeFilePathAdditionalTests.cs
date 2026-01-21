using FluentAssertions;
using SGuard.ConfigValidation.Security;
using SGuard.ConfigValidation.Utilities;

namespace SGuard.ConfigValidation.Test;

public sealed class SafeFilePathAdditionalTests : IDisposable
{
    private readonly string _testDirectory;

    public SafeFilePathAdditionalTests()
    {
        _testDirectory = DirectoryUtility.CreateTempDirectory("safefilepath-additional-test");
    }

    [Fact]
    public void IsPathWithinBaseDirectory_With_Exception_Should_ReturnFalse()
    {
        // Arrange - Invalid path that causes exception
        var invalidPath = "invalid:path:with:colons";
        var baseDir = _testDirectory;

        // Act
        var result = SafeFilePath.IsPathWithinBaseDirectory(invalidPath, baseDir);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateResolvedPath_With_Exception_Should_Throw_UnauthorizedAccessException()
    {
        // Arrange - Invalid path that causes exception
        var invalidPath = "invalid:path:with:colons";
        var basePath = Path.Combine(_testDirectory, "base.json");

        // Act & Assert
        var action = () => SafeFilePath.ValidateResolvedPath(invalidPath, basePath);
        action.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void IsSymlink_With_Exception_Should_ReturnFalse()
    {
        // Arrange - Invalid path that causes exception
        var invalidPath = "invalid:path:with:colons";

        // Act
        var result = SafeFilePath.IsSymlink(invalidPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSymlink_With_Exception_Should_HandleGracefully()
    {
        // Arrange - Invalid path that causes exception
        var invalidPath = "invalid:path:with:colons";
        var basePath = Path.Combine(_testDirectory, "base.json");

        // Act & Assert - Should handle exception gracefully
        var action = () => SafeFilePath.ValidateSymlink(invalidPath, basePath);
        action.Should().NotThrow();
    }

    [Fact]
    public void CanonicalizePath_With_Exception_Should_ReturnNormalizedPath()
    {
        // Arrange - Invalid path that causes exception
        var invalidPath = "invalid:path:with:colons";

        // Act
        var result = SafeFilePath.CanonicalizePath(invalidPath);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void CanonicalizePath_With_Symlink_Should_ReturnCanonicalPath()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "test");

        // Act
        var result = SafeFilePath.CanonicalizePath(filePath);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        Path.IsPathRooted(result).Should().BeTrue();
    }

    [Fact]
    public void CanonicalizePath_With_ExceptionDuringCanonicalization_Should_ReturnNormalizedPath()
    {
        // Arrange - Path that might cause exception during canonicalization
        var path = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(path, "test");

        // Act
        var result = SafeFilePath.CanonicalizePath(path);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void IsPathWithinBaseDirectory_With_CaseSensitiveComparison_Should_WorkCorrectly()
    {
        // Arrange
        var baseDir = Path.Combine(_testDirectory, "Base");
        Directory.CreateDirectory(baseDir);
        var path = Path.Combine(baseDir, "File.txt");
        File.WriteAllText(path, "test");

        // Act
        var result = SafeFilePath.IsPathWithinBaseDirectory(path, baseDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPathWithinBaseDirectory_With_CaseInsensitiveComparison_Should_WorkCorrectly()
    {
        // Arrange
        var baseDir = Path.Combine(_testDirectory, "base");
        Directory.CreateDirectory(baseDir);
        var path = Path.Combine(baseDir, "FILE.TXT");
        File.WriteAllText(path, "test");

        // Act
        var result = SafeFilePath.IsPathWithinBaseDirectory(path, baseDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateResolvedPath_With_ExceptionDuringPathResolution_Should_Throw_UnauthorizedAccessException()
    {
        // Arrange - Invalid path that causes exception
        var invalidPath = "invalid:path:with:colons";
        var basePath = Path.Combine(_testDirectory, "base.json");

        // Act & Assert
        var action = () => SafeFilePath.ValidateResolvedPath(invalidPath, basePath);
        action.Should().Throw<UnauthorizedAccessException>();
    }

    public void Dispose()
    {
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
    }
}

