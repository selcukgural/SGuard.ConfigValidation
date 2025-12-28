using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Resources;
using SGuard.ConfigValidation.Security;
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
    // Value includes both the resolved path and last access timestamp for LRU eviction
    private readonly ConcurrentDictionary<string, (string Path, DateTime LastAccess)> _pathCache = new();
    private readonly SecurityOptions _securityOptions;

    // Eviction threshold multiplier: eviction triggers when cache reaches 100% of max size (more aggressive)
    private const double EvictionThresholdMultiplier = 1.0;
    
    // Percentage of cache entries to evict when threshold is exceeded (increased for better memory management)
    private const double EvictionPercentage = 0.3;

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
        // Use string.Concat instead of string interpolation for better performance
        var sanitizedCacheKey = SafeFilePath.SanitizeCacheKey(string.Concat(path, "|", basePath));
        
        // Check cache first
        if (_pathCache.TryGetValue(sanitizedCacheKey, out var cachedEntry))
        {
            // Update last access time for LRU tracking (atomic update)
            _pathCache.TryUpdate(sanitizedCacheKey, (cachedEntry.Path, DateTime.UtcNow), cachedEntry);
            
            // Validate a cached path is still within the base directory (defense in depth)
            SafeFilePath.ValidateResolvedPath(cachedEntry.Path, basePath);
            ValidationMetrics.RecordCacheHit();
            return cachedEntry.Path;
        }
        
        ValidationMetrics.RecordCacheMiss();

        string resolvedPath;

        // If a path is already absolute, validate it against the base directory
        if (Path.IsPathRooted(path))
        {
            resolvedPath = Path.GetFullPath(path);
            
            // Validate that an absolute path is within the base directory
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
            resolvedPath = Path.GetFullPath(string.IsNullOrWhiteSpace(baseDirectory) ?
                                        // If a base path has no directory (unlikely), use the current directory
                                        path : Path.Combine(baseDirectory, path));
            
            // Validate that a resolved path is within the base directory
            SafeFilePath.ValidateResolvedPath(resolvedPath, basePath);
            
            // Validate symlink if the path exists
            if (File.Exists(resolvedPath) || Directory.Exists(resolvedPath))
            {
                SafeFilePath.ValidateSymlink(resolvedPath, basePath);
            }
        }
        
        // Cache the resolved path with current timestamp (only if validation passed)
        // Enforce cache size limit to prevent memory exhaustion
        // Use an atomic check-and-add pattern with safe eviction to avoid race conditions
        var cacheEntry = (resolvedPath, DateTime.UtcNow);
        var added = _pathCache.TryAdd(sanitizedCacheKey, cacheEntry);
        
        if (!added)
        {
            // Key already exists in cache (shouldn't happen due to cache check above, but handle it)
            // Update the existing entry atomically with new timestamp
            _pathCache.TryUpdate(sanitizedCacheKey, cacheEntry, _pathCache[sanitizedCacheKey]);
        }
        
        // Check if cache size exceeds threshold and evict if necessary
        // This is lock-free and uses threshold-based eviction with LRU to minimize overhead
        TryEvictIfNeeded();

        return resolvedPath;
    }

    /// <summary>
    /// Checks if cache size exceeds the eviction threshold and evicts entries if necessary.
    /// Uses a lock-free threshold-based approach with LRU (Least Recently Used) eviction to minimize eviction overhead.
    /// </summary>
    /// <remarks>
    /// Eviction is triggered when cache size exceeds MaxPathCacheSize * EvictionThresholdMultiplier (100%).
    /// When triggered, approximately EvictionPercentage (30%) of entries are evicted, prioritizing least recently used entries.
    /// This approach reduces frequent evictions, improves performance, and prevents memory leaks.
    /// </remarks>
    private void TryEvictIfNeeded()
    {
        // Calculate eviction threshold (100% of max cache size - more aggressive to prevent memory leaks)
        var threshold = (int)(_securityOptions.MaxPathCacheSize * EvictionThresholdMultiplier);
        
        // Fast path: check if eviction is needed (atomic read)
        var currentCount = _pathCache.Count;
        
        if (currentCount <= threshold)
        {
            return; // No eviction needed
        }

        // Double-check pattern: count might have changed since first check
        // This is lock-free and safe because ConcurrentDictionary.Count is atomic
        var doubleCheckCount = _pathCache.Count;
        
        if (doubleCheckCount <= threshold)
        {
            return; // Another thread already evicted or count decreased
        }

        // Calculate how many entries to evict (30% of current cache size - more aggressive)
        // Ensure we evict at least enough to bring cache below max size
        var entriesToEvict = Math.Max(
            (int)(doubleCheckCount * EvictionPercentage),
            doubleCheckCount - _securityOptions.MaxPathCacheSize + 1);

        EvictLeastRecentlyUsedEntries(entriesToEvict);
    }

    /// <summary>
    /// Evicts the specified number of least recently used entries from the cache.
    /// Uses LRU (Least Recently Used) algorithm to prioritize eviction of unused entries.
    /// </summary>
    /// <param name="count">The number of entries to evict.</param>
    /// <remarks>
    /// This method is thread-safe and lock-free. It collects all cache entries, sorts them by last access time,
    /// and evicts the least recently used entries. New entries may be added during eviction, which is acceptable
    /// and handled by the threshold mechanism. This prevents memory leaks by ensuring unused entries are evicted first.
    /// </remarks>
    private void EvictLeastRecentlyUsedEntries(int count)
    {
        if (count <= 0)
        {
            return;
        }

        // Collect all entries with their access times for sorting
        // This is safe because we're only reading, and ConcurrentDictionary supports concurrent enumeration
        var entries = new List<(string Key, DateTime LastAccess)>();
        
        foreach (var kvp in _pathCache)
        {
            entries.Add((kvp.Key, kvp.Value.LastAccess));
        }

        // If we have fewer entries than requested, evict all
        if (entries.Count <= count)
        {
            foreach (var entry in entries)
            {
                _pathCache.TryRemove(entry.Key, out _);
            }

            return;
        }

        // Sort by last access time (oldest first) and evict the least recently used entries
        entries.Sort((a, b) => a.LastAccess.CompareTo(b.LastAccess));
        
        var evictedCount = 0;
        foreach (var entry in entries)
        {
            if (evictedCount >= count)
            {
                break;
            }

            // TryRemove is thread-safe and atomic
            if (_pathCache.TryRemove(entry.Key, out _))
            {
                evictedCount++;
            }
        }
    }
}

