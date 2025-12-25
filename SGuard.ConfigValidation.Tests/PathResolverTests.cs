using FluentAssertions;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Services;

namespace SGuard.ConfigValidation.Tests;

public sealed class PathResolverTests
{
    private readonly PathResolver _resolver;

    public PathResolverTests()
    {
        var securityOptions = Options.Create(new SecurityOptions());
        _resolver = new PathResolver(securityOptions);
    }

    [Fact]
    public void ResolvePath_With_AbsolutePath_Should_Return_AsIs()
    {
        // Arrange
        var absolutePath = Path.GetFullPath("test.json");
        var basePath = Path.GetFullPath("base.json");

        // Act
        var result = _resolver.ResolvePath(absolutePath, basePath);

        // Assert
        result.Should().Be(absolutePath);
        Path.IsPathRooted(result).Should().BeTrue();
    }

    [Fact]
    public void ResolvePath_With_RelativePath_Should_Resolve_AgainstBasePath()
    {
        // Arrange
        var relativePath = "test.json";
        var basePath = Path.GetFullPath("base.json");
        var baseDirectory = Path.GetDirectoryName(basePath);
        var expectedPath = Path.GetFullPath(Path.Combine(baseDirectory!, relativePath));

        // Act
        var result = _resolver.ResolvePath(relativePath, basePath);

        // Assert
        result.Should().Be(expectedPath);
        Path.IsPathRooted(result).Should().BeTrue();
    }

    [Fact]
    public void ResolvePath_With_EmptyPath_Should_Return_Empty()
    {
        // Arrange
        var basePath = Path.GetFullPath("base.json");

        // Act
        var result = _resolver.ResolvePath(string.Empty, basePath);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ResolvePath_With_WhitespacePath_Should_Return_Whitespace()
    {
        // Arrange
        var basePath = Path.GetFullPath("base.json");

        // Act
        var result = _resolver.ResolvePath("   ", basePath);

        // Assert
        result.Should().Be("   ");
    }

    [Fact]
    public void ResolvePath_With_NullPath_Should_Return_Null()
    {
        // Arrange
        var basePath = Path.GetFullPath("base.json");

        // Act
        var result = _resolver.ResolvePath(null!, basePath);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ResolvePath_With_RelativePathAndNestedDirectory_Should_Resolve_Correctly()
    {
        // Arrange - Use a path that stays within base directory
        var relativePath = "subdir/test.json";
        var basePath = Path.GetFullPath(Path.Combine("dir", "base.json"));
        var baseDirectory = Path.GetDirectoryName(basePath);
        var expectedPath = Path.GetFullPath(Path.Combine(baseDirectory!, relativePath));

        // Act
        var result = _resolver.ResolvePath(relativePath, basePath);

        // Assert
        result.Should().Be(expectedPath);
        Path.IsPathRooted(result).Should().BeTrue();
    }

    [Fact]
    public void ResolvePath_With_PathTraversal_Should_Throw_UnauthorizedAccessException()
    {
        // Arrange - Path that tries to escape base directory
        var relativePath = "../other/test.json";
        var basePath = Path.GetFullPath(Path.Combine("dir", "base.json"));

        // Act & Assert
        var action = () => _resolver.ResolvePath(relativePath, basePath);
        action.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    [Fact]
    public void ResolvePath_With_AbsolutePathOutsideBaseDirectory_Should_Throw_UnauthorizedAccessException()
    {
        // Arrange - Absolute path outside base directory
        var absolutePath = Path.GetFullPath("/tmp/test.json");
        var basePath = Path.GetFullPath(Path.Combine("dir", "base.json"));

        // Act & Assert
        var action = () => _resolver.ResolvePath(absolutePath, basePath);
        action.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    [Fact]
    public void ResolvePath_With_CachedPath_Should_Return_CachedResult()
    {
        // Arrange
        var relativePath = "test.json";
        var basePath = Path.GetFullPath("base.json");
        var baseDirectory = Path.GetDirectoryName(basePath);
        var expectedPath = Path.GetFullPath(Path.Combine(baseDirectory!, relativePath));

        // Act - Resolve same path twice
        var result1 = _resolver.ResolvePath(relativePath, basePath);
        var result2 = _resolver.ResolvePath(relativePath, basePath);

        // Assert - Both should return same result (cached)
        result1.Should().Be(expectedPath);
        result2.Should().Be(expectedPath);
        result1.Should().Be(result2);
    }
}

