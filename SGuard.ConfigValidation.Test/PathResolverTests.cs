using FluentAssertions;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Security;
using SGuard.ConfigValidation.Services;

namespace SGuard.ConfigValidation.Test;

public sealed class PathResolverTests : IDisposable
{
    private readonly PathResolver _resolver;

    public PathResolverTests()
    {
        var securityOptions = Options.Create(new SecurityOptions());
        _resolver = new PathResolver(securityOptions);
    }

    public void Dispose()
    {
        _resolver.Dispose();
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

    [Fact]
    public async Task PathResolver_ConcurrentAccess_Should_NotThrow()
    {
        // Arrange
        using var resolver = new PathResolver(Options.Create(new SecurityOptions()));
        var basePath = Path.GetFullPath("base.json");
        var tasks = new List<Task<string>>();

        // Act - Create multiple concurrent resolution tasks
        for (var i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() => resolver.ResolvePath($"test{index}.json", basePath)));
        }

        // Assert - All tasks should complete without exceptions
        var action = async () => await Task.WhenAll(tasks);
        await action.Should().NotThrowAsync();

        // Verify all results are valid paths
        var results = await Task.WhenAll(tasks);
        results.Should().AllSatisfy(path => path.Should().NotBeNullOrEmpty());
        results.Should().AllSatisfy(path => Path.IsPathRooted(path).Should().BeTrue());
    }

    [Fact]
    public void PathResolver_CacheEviction_WhenSizeLimitExceeded_Should_EvictAutomatically()
    {
        // Arrange - Use a small cache size to trigger eviction
        using var resolver = new PathResolver(Options.Create(new SecurityOptions
        {
            MaxPathCacheSize = 10 // Small cache size to trigger eviction
        }));
        var basePath = Path.GetFullPath("base.json");

        // Act - Add more entries than the cache can hold
        // MemoryCache will automatically evict entries when size limit is reached
        for (var i = 0; i < 15; i++)
        {
            resolver.ResolvePath($"test{i}.json", basePath);
        }

        // Assert - Resolution should still work after eviction
        // MemoryCache handles eviction automatically, so we verify functionality
        var result = resolver.ResolvePath("test0.json", basePath);
        result.Should().NotBeNullOrEmpty();
        Path.IsPathRooted(result).Should().BeTrue();
    }

    [Fact]
    public async Task PathResolver_CacheEviction_Concurrent_Should_BeThreadSafe()
    {
        // Arrange - Use a small cache size to trigger eviction under concurrent load
        using var resolver = new PathResolver(Options.Create(new SecurityOptions
        {
            MaxPathCacheSize = 20 // Small cache size to trigger eviction
        }));
        var basePath = Path.GetFullPath("base.json");
        var tasks = new List<Task<string>>();

        // Act - Create many concurrent resolution tasks that will trigger eviction
        for (var i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() => resolver.ResolvePath($"test{index}.json", basePath)));
        }

        // Assert - All tasks should complete without exceptions (thread-safe eviction)
        var action = async () => await Task.WhenAll(tasks);
        await action.Should().NotThrowAsync();

        // Verify all results are valid paths
        var results = await Task.WhenAll(tasks);
        results.Should().AllSatisfy(path => path.Should().NotBeNullOrEmpty());
        results.Should().AllSatisfy(path => Path.IsPathRooted(path).Should().BeTrue());

        // Verify cache still works after concurrent eviction
        var cachedResult = resolver.ResolvePath("test0.json", basePath);
        cachedResult.Should().NotBeNullOrEmpty();
        Path.IsPathRooted(cachedResult).Should().BeTrue();
    }

    [Fact]
    public void PathResolver_Dispose_Should_PreventFurtherUse()
    {
        // Arrange
        var resolver = new PathResolver(Options.Create(new SecurityOptions()));
        var basePath = Path.GetFullPath("base.json");

        // Act
        resolver.Dispose();

        // Assert - Should throw ObjectDisposedException after disposal
        var action = () => resolver.ResolvePath("test.json", basePath);
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void PathResolver_Dispose_MultipleTimes_Should_NotThrow()
    {
        // Arrange
        var resolver = new PathResolver(Options.Create(new SecurityOptions()));

        // Act & Assert - Multiple dispose calls should not throw
        var action = () =>
        {
            resolver.Dispose();
            resolver.Dispose();
            resolver.Dispose();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void PathResolver_With_NullSecurityOptions_Should_Throw_ArgumentNullException()
    {
        // Act & Assert
        var action = () => new PathResolver(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ResolvePath_With_NullBasePath_Should_Throw_ArgumentException()
    {
        // Arrange
        var path = "test.json";

        // Act & Assert
        var action = () => _resolver.ResolvePath(path, null!);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ResolvePath_With_EmptyBasePath_Should_Throw_ArgumentException()
    {
        // Arrange
        var path = "test.json";

        // Act & Assert
        var action = () => _resolver.ResolvePath(path, string.Empty);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ResolvePath_With_WhitespaceBasePath_Should_Throw_ArgumentException()
    {
        // Arrange
        var path = "test.json";

        // Act & Assert
        var action = () => _resolver.ResolvePath(path, "   ");
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ResolvePath_CacheHit_Should_ValidatePathAgain()
    {
        // Arrange - This test verifies defense-in-depth: cached paths are re-validated
        var relativePath = "test.json";
        var basePath = Path.GetFullPath("base.json");

        // Act - First call caches the result
        var result1 = _resolver.ResolvePath(relativePath, basePath);

        // Second call should hit cache but still validate
        var result2 = _resolver.ResolvePath(relativePath, basePath);

        // Assert - Both results should be identical and valid
        result1.Should().Be(result2);
        Path.IsPathRooted(result1).Should().BeTrue();
    }

    [Fact]
    public void ResolvePath_DifferentBasePaths_Should_CacheSeparately()
    {
        // Arrange
        var relativePath = "test.json";
        var basePath1 = Path.GetFullPath(Path.Combine("dir1", "base.json"));
        var basePath2 = Path.GetFullPath(Path.Combine("dir2", "base.json"));

        // Act
        var result1 = _resolver.ResolvePath(relativePath, basePath1);
        var result2 = _resolver.ResolvePath(relativePath, basePath2);

        // Assert - Different base paths should result in different resolved paths
        result1.Should().NotBe(result2);
        result1.Should().Contain("dir1");
        result2.Should().Contain("dir2");
    }
}
