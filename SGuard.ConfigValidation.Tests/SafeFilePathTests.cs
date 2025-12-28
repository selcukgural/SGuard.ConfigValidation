using FluentAssertions;
using System.Runtime.InteropServices;
using SGuard.ConfigValidation.Security;

namespace SGuard.ConfigValidation.Tests;

public sealed class SafeFilePathTests
{
    [Fact]
    public void IsPathWithinBaseDirectory_With_ValidPath_Should_Return_True()
    {
        // Arrange
        var baseDir = Path.GetFullPath("test");
        var resolvedPath = Path.GetFullPath(Path.Combine("test", "subdir", "file.json"));

        // Act
        var result = SafeFilePath.IsPathWithinBaseDirectory(resolvedPath, baseDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPathWithinBaseDirectory_With_PathOutsideBase_Should_Return_False()
    {
        // Arrange
        var baseDir = Path.GetFullPath("test");
        var resolvedPath = Path.GetFullPath("/tmp/file.json");

        // Act
        var result = SafeFilePath.IsPathWithinBaseDirectory(resolvedPath, baseDir);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateResolvedPath_With_ValidPath_Should_NotThrow()
    {
        // Arrange
        var basePath = Path.GetFullPath(Path.Combine("test", "config.json"));
        var resolvedPath = Path.GetFullPath(Path.Combine("test", "subdir", "file.json"));

        // Act
        var action = () => SafeFilePath.ValidateResolvedPath(resolvedPath, basePath);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateResolvedPath_With_PathTraversal_Should_Throw_UnauthorizedAccessException()
    {
        // Arrange
        var basePath = Path.GetFullPath(Path.Combine("test", "config.json"));
        var resolvedPath = Path.GetFullPath("/tmp/file.json");

        // Act
        var action = () => SafeFilePath.ValidateResolvedPath(resolvedPath, basePath);

        // Assert
        action.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    [Fact]
    public void SanitizeCacheKey_With_NormalKey_Should_Return_Same()
    {
        // Arrange
        var cacheKey = "path|basePath";

        // Act
        var result = SafeFilePath.SanitizeCacheKey(cacheKey);

        // Assert
        result.Should().Be(cacheKey);
    }

    [Fact]
    public void SanitizeCacheKey_With_ControlCharacters_Should_Remove_Them()
    {
        // Arrange
        var cacheKey = "path\0\r\n\t|basePath";

        // Act
        var result = SafeFilePath.SanitizeCacheKey(cacheKey);

        // Assert
        result.Should().Be("path|basePath");
        result.Should().NotContain("\0");
        result.Should().NotContain("\r");
        result.Should().NotContain("\n");
        result.Should().NotContain("\t");
    }

    [Fact]
    public void SanitizeCacheKey_With_LongKey_Should_Truncate()
    {
        // Arrange
        var cacheKey = new string('a', 2000);

        // Act
        var result = SafeFilePath.SanitizeCacheKey(cacheKey);

        // Assert
        result.Length.Should().BeLessThanOrEqualTo(1000);
    }

    [Fact]
    public void IsSymlink_With_NonExistentPath_Should_Return_False()
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("nonexistent", "file.json"));

        // Act
        var result = SafeFilePath.IsSymlink(path);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanonicalizePath_With_NormalPath_Should_Return_Normalized()
    {
        // Arrange
        var path = Path.Combine("test", "..", "test", "file.json");

        // Act
        var result = SafeFilePath.CanonicalizePath(path);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        Path.IsPathRooted(result).Should().BeTrue();
    }

    [Fact]
    public void CanonicalizePath_With_EmptyPath_Should_Return_Empty()
    {
        // Arrange
        var path = string.Empty;

        // Act
        var result = SafeFilePath.CanonicalizePath(path);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSymlink_With_ValidSymlink_Should_NotThrow()
    {
        // Arrange
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Skip test on unsupported platforms
            return;
        }

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var basePath = Path.Combine(tempDir, "base.json");
            File.WriteAllText(basePath, "{}");
            var targetFile = Path.Combine(tempDir, "target.txt");
            File.WriteAllText(targetFile, "target");
            var symlinkPath = Path.Combine(tempDir, "symlink.txt");

            // Create symlink (platform-specific)
            bool symlinkCreated = false;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: Use File.CreateSymbolicLink (.NET 6+)
                try
                {
                    File.CreateSymbolicLink(symlinkPath, targetFile);
                    symlinkCreated = true;
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip if symlink creation fails (may require admin rights)
                    return;
                }
                catch (IOException)
                {
                    // Skip if symlink creation fails (file system may not support symlinks)
                    return;
                }
            }
            else
            {
                // Unix-like: Use symbolic link
                try
                {
                    var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "ln",
                        Arguments = $"-s \"{targetFile}\" \"{symlinkPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    });
                    process?.WaitForExit();
                    if (process?.ExitCode == 0)
                    {
                        symlinkCreated = true;
                    }
                    else
                    {
                        // Skip if symlink creation fails
                        return;
                    }
                }
                catch
                {
                    // Skip if ln command is not available
                    return;
                }
            }

            if (!symlinkCreated)
            {
                return; // Skip if symlink was not created
            }

            // Act
            var action = () => SafeFilePath.ValidateSymlink(symlinkPath, basePath);

            // Assert
            action.Should().NotThrow();
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [Fact]
    public void ValidateSymlink_With_SymlinkOutsideBase_Should_Throw_UnauthorizedAccessException()
    {
        // Arrange
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var outsideFile = Path.Combine(Path.GetTempPath(), $"outside_{Guid.NewGuid()}.txt");
        try
        {
            var basePath = Path.Combine(tempDir, "subdir", "base.json");
            Directory.CreateDirectory(Path.GetDirectoryName(basePath)!);
            File.WriteAllText(basePath, "{}");
            File.WriteAllText(outsideFile, "outside");
            var symlinkPath = Path.Combine(tempDir, "subdir", "symlink.txt");

            // Create symlink pointing outside
            bool symlinkCreated = false;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    File.CreateSymbolicLink(symlinkPath, outsideFile);
                    symlinkCreated = true;
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip if symlink creation fails (may require admin rights)
                    return;
                }
                catch (IOException)
                {
                    // Skip if symlink creation fails (file system may not support symlinks)
                    return;
                }
            }
            else
            {
                try
                {
                    var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "ln",
                        Arguments = $"-s \"{outsideFile}\" \"{symlinkPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    });
                    process?.WaitForExit();
                    if (process?.ExitCode == 0)
                    {
                        symlinkCreated = true;
                    }
                    else
                    {
                        return; // Skip if symlink creation fails
                    }
                }
                catch
                {
                    // Skip if ln command is not available
                    return;
                }
            }

            if (!symlinkCreated)
            {
                return; // Skip if symlink was not created
            }

            // Act & Assert
            var action = () => SafeFilePath.ValidateSymlink(symlinkPath, basePath);
            action.Should().Throw<UnauthorizedAccessException>()
                .WithMessage("*Symlink attack detected*");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
            try { File.Delete(outsideFile); } catch { }
        }
    }

    [Fact]
    public void ValidateResolvedPath_With_NullResolvedPath_Should_Throw_UnauthorizedAccessException()
    {
        // Arrange
        var basePath = Path.GetFullPath("test.json");

        // Act & Assert
        var action = () => SafeFilePath.ValidateResolvedPath(null!, basePath);
        action.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void ValidateResolvedPath_With_EmptyResolvedPath_Should_Throw_UnauthorizedAccessException()
    {
        // Arrange
        var basePath = Path.GetFullPath("test.json");

        // Act & Assert
        var action = () => SafeFilePath.ValidateResolvedPath("", basePath);
        action.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void ValidateResolvedPath_With_NullBasePath_Should_Throw_UnauthorizedAccessException()
    {
        // Arrange
        var resolvedPath = Path.GetFullPath("test.json");

        // Act & Assert
        var action = () => SafeFilePath.ValidateResolvedPath(resolvedPath, null!);
        action.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void ValidateResolvedPath_With_EmptyBasePath_Should_Throw_UnauthorizedAccessException()
    {
        // Arrange
        var resolvedPath = Path.GetFullPath("test.json");

        // Act & Assert
        var action = () => SafeFilePath.ValidateResolvedPath(resolvedPath, "");
        action.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void IsPathWithinBaseDirectory_With_NullResolvedPath_Should_ReturnFalse()
    {
        // Arrange
        var baseDir = Path.GetFullPath("test");

        // Act
        var result = SafeFilePath.IsPathWithinBaseDirectory(null!, baseDir);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPathWithinBaseDirectory_With_EmptyResolvedPath_Should_ReturnFalse()
    {
        // Arrange
        var baseDir = Path.GetFullPath("test");

        // Act
        var result = SafeFilePath.IsPathWithinBaseDirectory("", baseDir);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPathWithinBaseDirectory_With_NullBaseDirectory_Should_ReturnFalse()
    {
        // Arrange
        var resolvedPath = Path.GetFullPath("test.json");

        // Act
        var result = SafeFilePath.IsPathWithinBaseDirectory(resolvedPath, null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPathWithinBaseDirectory_With_EmptyBaseDirectory_Should_ReturnFalse()
    {
        // Arrange
        var resolvedPath = Path.GetFullPath("test.json");

        // Act
        var result = SafeFilePath.IsPathWithinBaseDirectory(resolvedPath, "");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSymlink_With_NullPath_Should_ReturnFalse()
    {
        // Act
        var result = SafeFilePath.IsSymlink(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSymlink_With_EmptyPath_Should_ReturnFalse()
    {
        // Act
        var result = SafeFilePath.IsSymlink("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSymlink_With_RegularFile_Should_ReturnFalse()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
        try
        {
            File.WriteAllText(tempFile, "test");

            // Act
            var result = SafeFilePath.IsSymlink(tempFile);

            // Assert
            result.Should().BeFalse();
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    [Fact]
    public void ValidateSymlink_With_NullPath_Should_NotThrow()
    {
        // Arrange
        var basePath = Path.GetFullPath("test.json");

        // Act
        var action = () => SafeFilePath.ValidateSymlink(null!, basePath);

        // Assert - Should return early without throwing
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateSymlink_With_EmptyPath_Should_NotThrow()
    {
        // Arrange
        var basePath = Path.GetFullPath("test.json");

        // Act
        var action = () => SafeFilePath.ValidateSymlink("", basePath);

        // Assert - Should return early without throwing
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateSymlink_With_NullBasePath_Should_NotThrow()
    {
        // Arrange
        var path = Path.GetFullPath("test.json");

        // Act
        var action = () => SafeFilePath.ValidateSymlink(path, null!);

        // Assert - Should return early without throwing
        action.Should().NotThrow();
    }

    [Fact]
    public void IsPathWithinBaseDirectory_With_NullPaths_Should_ReturnFalse()
    {
        // Act
        var result = SafeFilePath.IsPathWithinBaseDirectory(null!, "base");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPathWithinBaseDirectory_With_EmptyPaths_Should_ReturnFalse()
    {
        // Act
        var result = SafeFilePath.IsPathWithinBaseDirectory("", "");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPathWithinBaseDirectory_With_WhitespacePaths_Should_ReturnFalse()
    {
        // Act
        var result = SafeFilePath.IsPathWithinBaseDirectory("   ", "   ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateResolvedPath_With_BasePathNoDirectory_Should_UseCurrentDirectory()
    {
        // Arrange
        var basePath = "filename.json"; // No directory
        var resolvedPath = Path.Combine(Directory.GetCurrentDirectory(), "resolved.json");
        File.WriteAllText(resolvedPath, "test");

        try
        {
            // Act - Should not throw because resolved path is in current directory
            SafeFilePath.ValidateResolvedPath(resolvedPath, basePath);

            // Assert - No exception thrown
        }
        finally
        {
            try { File.Delete(resolvedPath); } catch { }
        }
    }

    [Fact]
    public void ValidateSymlink_With_EmptyBasePath_Should_NotThrow()
    {
        // Arrange
        var path = Path.GetFullPath("test.json");

        // Act
        var action = () => SafeFilePath.ValidateSymlink(path, "");

        // Assert - Should return early without throwing
        action.Should().NotThrow();
    }

    [Fact]
    public void CanonicalizePath_With_RelativePath_Should_ReturnAbsolute()
    {
        // Arrange
        var path = "test/file.json";

        // Act
        var result = SafeFilePath.CanonicalizePath(path);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        Path.IsPathRooted(result).Should().BeTrue();
    }
}

