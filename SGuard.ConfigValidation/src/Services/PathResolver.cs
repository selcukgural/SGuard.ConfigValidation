using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Resources;
using SGuard.ConfigValidation.Services.Abstract;
using SGuard.ConfigValidation.Telemetry;
using static SGuard.ConfigValidation.Common.Throw;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Service for resolving relative paths to absolute paths with security validation.
/// </summary>
public sealed class PathResolver : IPathResolver
{
    // Cache for resolved paths to avoid repeated calculations
    // The key format: "{path}|{basePath}"
    private readonly ConcurrentDictionary<string, string> _pathCache = new();
    private readonly SecurityOptions _securityOptions;

    /// <summary>
    /// Initializes a new instance of the PathResolver class.
    /// </summary>
    /// <param name="securityOptions">Security options configured via IOptions pattern.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="securityOptions"/> is null.</exception>
    public PathResolver(IOptions<SecurityOptions> securityOptions)
    {
        System.ArgumentNullException.ThrowIfNull(securityOptions);
        _securityOptions = securityOptions.Value;
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
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw ArgumentException(nameof(SR.ArgumentException_BasePathNullOrEmpty), nameof(basePath));
        }

        // Sanitize a cache key to prevent cache poisoning
        var sanitizedCacheKey = PathSecurity.SanitizeCacheKey($"{path}|{basePath}");
        
        // Check cache first
        if (_pathCache.TryGetValue(sanitizedCacheKey, out var cachedPath))
        {
            // Validate a cached path is still within the base directory (defense in depth)
            PathSecurity.ValidateResolvedPath(cachedPath, basePath);
            ValidationMetrics.RecordCacheHit();
            return cachedPath;
        }
        
        ValidationMetrics.RecordCacheMiss();

        string resolvedPath;

        // If a path is already absolute, validate it against the base directory
        if (Path.IsPathRooted(path))
        {
            resolvedPath = Path.GetFullPath(path);
            
            // Validate that an absolute path is within the base directory
            PathSecurity.ValidateResolvedPath(resolvedPath, basePath);
            
            // Validate symlink if the path exists
            if (File.Exists(resolvedPath) || Directory.Exists(resolvedPath))
            {
                PathSecurity.ValidateSymlink(resolvedPath, basePath);
            }
        }
        else
        {
            // Resolve relative path based on the base path's directory
            var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(basePath));
            resolvedPath = Path.GetFullPath(string.IsNullOrWhiteSpace(baseDirectory) ?
                                        // If a base path has no directory (unlikely), use the current directory
                                        path : Path.Combine(baseDirectory, path));
            
            // Validate that a resolved path is within the base directory
            PathSecurity.ValidateResolvedPath(resolvedPath, basePath);
            
            // Validate symlink if the path exists
            if (File.Exists(resolvedPath) || Directory.Exists(resolvedPath))
            {
                PathSecurity.ValidateSymlink(resolvedPath, basePath);
            }
        }
        
        // Cache the resolved path (only if validation passed)
        // Enforce cache size limit to prevent memory exhaustion
        // Use atomic check-and-add pattern to avoid race conditions
        // TryAdd will fail if cache is full, then we evict and retry
        if (!_pathCache.TryAdd(sanitizedCacheKey, resolvedPath))
        {
            // Key already exists in cache (shouldn't happen due to cache check above, but handle it)
            // Update the existing entry
            _pathCache[sanitizedCacheKey] = resolvedPath;
        }
        else
        {
            // Successfully added, but check if we need to evict entries
            // Use atomic check: if count exceeds limit, evict oldest entries
            // This is still not perfect (race condition possible), but better than before
            // In high-concurrency scenarios, cache might temporarily exceed limit, but will self-correct
            if (_pathCache.Count > _securityOptions.MaxPathCacheSize)
            {
                // Evict entries until we're under the limit
                // Remove oldest entries (simple eviction: remove first N entries)
                var entriesToRemove = _pathCache.Count - _securityOptions.MaxPathCacheSize + 1; // Remove one extra to make room
                var keysToRemove = _pathCache.Keys.Take(entriesToRemove).ToList();
                foreach (var key in keysToRemove)
                {
                    _pathCache.TryRemove(key, out _);
                }
            }
        }
        
        return resolvedPath;
    }
}

