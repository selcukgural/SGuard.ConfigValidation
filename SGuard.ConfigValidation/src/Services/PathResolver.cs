using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Resources;
using SGuard.ConfigValidation.Security;
using SGuard.ConfigValidation.Services.Abstract;
using SGuard.ConfigValidation.Telemetry;
using static SGuard.ConfigValidation.Common.Throw;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Service for resolving relative paths to absolute paths with security validation.
/// Uses MemoryCache with sliding expiration for efficient LRU-like behavior.
/// </summary>
public sealed class PathResolver : IPathResolver, IDisposable
{
    private readonly MemoryCache _pathCache;
    private readonly MemoryCacheEntryOptions _cacheEntryOptions;
    private bool _disposed;

    /// <summary>
    /// Default sliding expiration for cache entries (30 minutes).
    /// Entries not accessed within this period will be automatically evicted.
    /// </summary>
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Initializes a new instance of the PathResolver class.
    /// </summary>
    /// <param name="securityOptions">Security options configured via IOptions pattern.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="securityOptions"/> is null.</exception>
    public PathResolver(IOptions<SecurityOptions> securityOptions)
    {
        System.ArgumentNullException.ThrowIfNull(securityOptions);

        var cacheOptions = new MemoryCacheOptions
        {
            SizeLimit = securityOptions.Value.MaxPathCacheSize,
            // Compact 30% of the cache when the size limit is reached
            CompactionPercentage = 0.3
        };
        _pathCache = new MemoryCache(cacheOptions);

        // Configure cache entry options with sliding expiration
        // Each entry has size 1 for simple counting
        _cacheEntryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = DefaultSlidingExpiration,
            Size = 1,
            Priority = CacheItemPriority.Normal
        };
    }

    /// <summary>
    /// Resolves a relative path to an absolute path based on the base path.
    /// Validates that the resolved path is within the base directory to prevent path traversal attacks.
    /// If the path is already absolute, it is validated against the base directory.
    /// </summary>
    /// <param name="path">The path to resolve (can be relative or absolute).</param>
    /// <param name="basePath">The base path used for resolving relative paths.</param>
    /// <returns>An absolute path that is guaranteed to be within the base directory.</returns>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when the resolved path is outside the base directory (path traversal attempt).</exception>
    /// <exception cref="System.ObjectDisposedException">Thrown when the PathResolver has been disposed.</exception>
    /// <example>
    /// <code>
    /// using Microsoft.Extensions.Options;
    /// using SGuard.ConfigValidation.Common;
    /// using SGuard.ConfigValidation.Services;
    /// 
    /// var securityOptions = Options.Create(new SecurityOptions());
    /// var pathResolver = new PathResolver(securityOptions);
    /// 
    /// try
    /// {
    ///     // Resolve a relative path
    ///     var basePath = "/app/config";
    ///     var relativePath = "appsettings.Production.json";
    ///     var resolvedPath = pathResolver.ResolvePath(relativePath, basePath);
    ///     Console.WriteLine($"Resolved path: {resolvedPath}");
    ///     
    ///     // Resolve an absolute path (validated against base)
    ///     var absolutePath = "/app/config/appsettings.Dev.json";
    ///     var validatedPath = pathResolver.ResolvePath(absolutePath, basePath);
    ///     Console.WriteLine($"Validated path: {validatedPath}");
    /// }
    /// catch (UnauthorizedAccessException ex)
    /// {
    ///     Console.WriteLine($"Path traversal attempt detected: {ex.Message}");
    /// }
    /// </code>
    /// </example>
    public string ResolvePath(string path, string basePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw ArgumentException(nameof(SR.ArgumentException_BasePathNullOrEmpty), nameof(basePath));
        }

        // Sanitize a cache key to prevent cache poisoning
        // Use string.Concat instead of string interpolation for better performance
        var sanitizedCacheKey = SafeFilePath.SanitizeCacheKey(string.Concat(path, "|", basePath));

        // Check cache first - MemoryCache handles sliding expiration automatically
        if (_pathCache.TryGetValue(sanitizedCacheKey, out string? cachedPath) && cachedPath != null)
        {
            // Validate a cached path is still within the base directory (defense in depth)
            SafeFilePath.ValidateResolvedPath(cachedPath, basePath);
            ValidationMetrics.RecordCacheHit();
            return cachedPath;
        }

        ValidationMetrics.RecordCacheMiss();

        string resolvedPath;

        // If path is already absolute, validate it against the base directory
        if (Path.IsPathRooted(path))
        {
            resolvedPath = Path.GetFullPath(path);

            // Validate that absolute path is within the base directory
            SafeFilePath.ValidateResolvedPath(resolvedPath, basePath);

            // Validate symlink if the path exists
            if (File.Exists(resolvedPath) || Directory.Exists(resolvedPath))
            {
                SafeFilePath.ValidateSymlink(resolvedPath, basePath);
            }
        }
        else
        {
            // Resolve relative path based on the base path's directory
            var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(basePath));
            resolvedPath = Path.GetFullPath(string.IsNullOrWhiteSpace(baseDirectory)
                ? path // If a base path has no directory (unlikely), use the current directory
                : Path.Combine(baseDirectory, path));

            // Validate that a resolved path is within the base directory
            SafeFilePath.ValidateResolvedPath(resolvedPath, basePath);

            // Validate symlink if the path exists
            if (File.Exists(resolvedPath) || Directory.Exists(resolvedPath))
            {
                SafeFilePath.ValidateSymlink(resolvedPath, basePath);
            }
        }

        // Cache the resolved path (only if validation passed)
        // MemoryCache handles size limit and eviction automatically
        _pathCache.Set(sanitizedCacheKey, resolvedPath, _cacheEntryOptions);

        return resolvedPath;
    }

    /// <summary>
    /// Releases all resources used by the PathResolver.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _pathCache.Dispose();
        _disposed = true;
    }
}
