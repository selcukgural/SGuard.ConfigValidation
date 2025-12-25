using System.Collections.Concurrent;

namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Service for resolving relative paths to absolute paths.
/// </summary>
public sealed class PathResolver : IPathResolver
{
    // Cache for resolved paths to avoid repeated calculations
    // The key format: "{path}|{basePath}"
    private readonly ConcurrentDictionary<string, string> _pathCache = new();

    /// <summary>
    /// Resolves a relative path to an absolute path based on the base path.
    /// If the path is already absolute, it is returned as-is.
    /// </summary>
    /// <param name="path">The path to resolve (can be relative or absolute).</param>
    /// <param name="basePath">The base path used for resolving relative paths.</param>
    /// <returns>An absolute path.</returns>
    public string ResolvePath(string path, string basePath)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        // If a path is already absolute, return as-is (no need to cache)
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        // Check cache first
        var cacheKey = $"{path}|{basePath}";
        if (_pathCache.TryGetValue(cacheKey, out var cachedPath))
        {
            return cachedPath;
        }

        // Resolve relative path based on the base path's directory
        var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(basePath));
        var resolvedPath = Path.GetFullPath(string.IsNullOrWhiteSpace(baseDirectory) ?
                                    // If a base path has no directory (unlikely), use the current directory
                                    path : Path.Combine(baseDirectory, path));
        
        // Cache the resolved path
        _pathCache.TryAdd(cacheKey, resolvedPath);
        
        return resolvedPath;
    }
}

