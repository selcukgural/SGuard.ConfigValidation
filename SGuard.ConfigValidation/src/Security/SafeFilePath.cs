using System.Runtime.InteropServices;
using SGuard.ConfigValidation.Resources;
using static SGuard.ConfigValidation.Common.Throw;

namespace SGuard.ConfigValidation.Security;

/// <summary>
/// Helper class for secure path operations and validation.
/// Provides path normalization, base directory validation, symlink detection, and security checks.
/// </summary>
public static class SafeFilePath
{
    /// <summary>
    /// Checks if a resolved path is within the specified base directory.
    /// This prevents path traversal attacks by ensuring the resolved path
    /// cannot escape the base directory.
    /// </summary>
    /// <param name="resolvedPath">The absolute resolved path to validate.</param>
    /// <param name="baseDirectory">The base directory that the path must be within.</param>
    /// <returns>True if the resolved path is within the base directory; otherwise, false.</returns>
    public static bool IsPathWithinBaseDirectory(string resolvedPath, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(resolvedPath) || string.IsNullOrWhiteSpace(baseDirectory))
        {
            return false;
        }

        try
        {
            // Normalize both paths to handle different path separators and case sensitivity
            var normalizedResolvedPath = NormalizePath(resolvedPath);
            var normalizedBaseDirectory = NormalizePath(baseDirectory);

            // Ensure the base directory ends with a separator for proper comparison
            if (!normalizedBaseDirectory.EndsWith(Path.DirectorySeparatorChar) && !normalizedBaseDirectory.EndsWith(Path.AltDirectorySeparatorChar))
            {
                normalizedBaseDirectory += Path.DirectorySeparatorChar;
            }

            // Check if a resolved path starts with the base directory
            // Use case-insensitive comparison on Windows, case-sensitive on Unix
            var comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            return normalizedResolvedPath.StartsWith(normalizedBaseDirectory, comparison);
        }
        catch
        {
            // If path normalization fails, consider it unsafe
            return false;
        }
    }

    /// <summary>
    /// Validates that a resolved path is within the base path's directory.
    /// Throws an exception if the path is outside the allowed directory.
    /// </summary>
    /// <param name="resolvedPath">The absolute resolved path to validate.</param>
    /// <param name="basePath">The base file path used for resolution.</param>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when the resolved path is outside the base directory.</exception>
    public static void ValidateResolvedPath(string resolvedPath, string basePath)
    {
        if (string.IsNullOrWhiteSpace(resolvedPath) || string.IsNullOrWhiteSpace(basePath))
        {
            var resolvedPathStr = string.IsNullOrWhiteSpace(resolvedPath) ? "null" : $"'{resolvedPath}' (empty)";
            var basePathStr = string.IsNullOrWhiteSpace(basePath) ? "null" : $"'{basePath}' (empty)";

            throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_PathSecurityNullEmpty), resolvedPathStr, basePathStr);
        }

        try
        {
            // Get the base directory from the base path
            var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(basePath));

            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                // If a base path has no directory (e.g., just a filename), use the current directory
                baseDirectory = Directory.GetCurrentDirectory();
            }

            // Check if a resolved path is within the base directory
            if (IsPathWithinBaseDirectory(resolvedPath, baseDirectory))
            {
                return;
            }

            var resolvedFullPath = Path.GetFullPath(resolvedPath);
            var baseFullPath = Path.GetFullPath(baseDirectory);

            throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_PathTraversalDetected), resolvedPath, resolvedFullPath,
                                              baseDirectory, baseFullPath);
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var resolvedFullPath = Path.GetFullPath(resolvedPath);
            var baseFullPath = Path.GetFullPath(basePath);

            throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_PathValidationUnexpectedError), ex, resolvedPath,
                                              resolvedFullPath, basePath, baseFullPath, ex.GetType().Name, ex.Message);
        }
    }

    /// <summary>
    /// Normalizes a path by converting it to an absolute path and handling
    /// different path separators and case sensitivity.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>A normalized absolute path.</returns>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        try
        {
            // Convert to an absolute path (handles relative paths, .., ., etc.)
            var fullPath = Path.GetFullPath(path);

            // Normalize path separators
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                       ?
                       // Windows: Use backslash as a separator
                       fullPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                       :
                       // Unix-like: Use forward slash as a separator
                       fullPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            // If normalization fails, return the original path
            // The calling method should handle this appropriately
            return path;
        }
    }

    /// <summary>
    /// Sanitizes a cache key by removing potentially dangerous characters.
    /// This prevents cache poisoning attacks.
    /// </summary>
    /// <param name="cacheKey">The cache key to sanitize.</param>
    /// <returns>A sanitized cache key safe for use in dictionaries.</returns>
    public static string SanitizeCacheKey(string cacheKey)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            return cacheKey;
        }

        // Remove null characters and control characters that could be used for cache poisoning
        var sanitized = cacheKey.Replace("\0", string.Empty)  // Null characters
                                .Replace("\r", string.Empty)  // Carriage return
                                .Replace("\n", string.Empty)  // Line feed
                                .Replace("\t", string.Empty); // Tab

        // Limit cache key length to prevent DoS
        const int maxCacheKeyLength = 1000;

        if (sanitized.Length > maxCacheKeyLength)
        {
            sanitized = sanitized[..maxCacheKeyLength];
        }

        return sanitized;
    }

    /// <summary>
    /// Checks if a file or directory is a symbolic link (symlink).
    /// This helps prevent symlink attacks where a symlink could point to an unauthorized location.
    /// </summary>
    /// <param name="path">The path to check for symlink.</param>
    /// <returns>True if the path is a symlink; otherwise, false.</returns>
    public static bool IsSymlink(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                return false;
            }

            var fileInfo = new FileInfo(path);

            // On Windows, check for reparse point (which includes symlinks)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return (fileInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
            }

            // On Unix-like systems, check if LinkTarget is not null (available in .NET 6+)
            // For older .NET versions, we check if the file exists and has special attributes
            try
            {
                // .NET 6+ has LinkTarget property
                return !string.IsNullOrEmpty(fileInfo.LinkTarget);
            }
            catch
            {
                // Fallback: Check if it's a symlink by attempting to read the link
                // This is a best-effort check for older .NET versions
                return false;
            }
        }
        catch
        {
            // If we can't determine, assume it's not a symlink to be safe
            // The path validation will still catch unauthorized access
            return false;
        }
    }

    /// <summary>
    /// Validates that a path is not a symlink pointing outside the base directory.
    /// This prevents symlink attacks where a symlink could bypass path validation.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="basePath">The base path used for validation.</param>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when the path is a symlink pointing outside the base directory.</exception>
    public static void ValidateSymlink(string path, string basePath)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(basePath))
        {
            return; // Let other validation methods handle null/empty paths
        }

        if (!IsSymlink(path))
        {
            return; // Not a symlink, no need to validate
        }

        try
        {
            var fileInfo = new FileInfo(path);
            var linkTarget = fileInfo.LinkTarget;

            string pathFull, baseFull;

            if (string.IsNullOrEmpty(linkTarget))
            {
                // Can't determine link target, reject to be safe
                pathFull = Path.GetFullPath(path);
                baseFull = Path.GetFullPath(basePath);
                throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_SymlinkUnresolvable), path, pathFull, basePath, baseFull);
            }

            // Resolve the symlink target to an absolute path
            var resolvedTarget = Path.IsPathRooted(linkTarget)
                                     ? Path.GetFullPath(linkTarget)
                                     : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path) ?? ".", linkTarget));

            // Get base directory
            var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(basePath));

            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                baseDirectory = Directory.GetCurrentDirectory();
            }

            // Validate that symlink target is within base directory
            if (IsPathWithinBaseDirectory(resolvedTarget, baseDirectory))
            {
                return;
            }
            
            pathFull = Path.GetFullPath(path);
            baseFull = Path.GetFullPath(baseDirectory);
            var targetFull = Path.GetFullPath(resolvedTarget);

            throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_SymlinkAttackDetected), path, pathFull, resolvedTarget,
                                              targetFull, baseDirectory, baseFull);
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // If symlink validation fails, reject to be safe
            var pathFull = Path.GetFullPath(path);
            var baseFull = Path.GetFullPath(basePath);

            throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_SymlinkValidationUnexpectedError), ex, path, pathFull, basePath,
                                              baseFull, ex.GetType().Name, ex.Message);
        }
    }

    /// <summary>
    /// Canonicalizes a path by resolving all symlinks and normalizing the path.
    /// This provides the actual file system path without symlink indirection.
    /// </summary>
    /// <param name="path">The path to canonicalize.</param>
    /// <returns>A canonicalized absolute path with all symlinks resolved.</returns>
    /// <remarks>
    /// This method attempts to resolve symlinks but may not work on all platforms
    /// or .NET versions. Use ValidateSymlink for security-critical operations.
    /// </remarks>
    public static string CanonicalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        try
        {
            var fullPath = Path.GetFullPath(path);
            var fileInfo = new FileInfo(fullPath);

            // Try to resolve the symlink target if it's a symlink
            if (!IsSymlink(fullPath))
            {
                return fullPath;
            }

            var linkTarget = fileInfo.LinkTarget;

            if (string.IsNullOrEmpty(linkTarget))
            {
                return fullPath;
            }

            // Resolve symlink target
            var resolvedTarget = Path.IsPathRooted(linkTarget)
                                     ? Path.GetFullPath(linkTarget)
                                     : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fullPath) ?? ".", linkTarget));

            // Recursively canonicalize the target (in the case of symlink chains)
            return CanonicalizePath(resolvedTarget);

        }
        catch
        {
            // If canonicalization fails, return a normalized path
            return NormalizePath(path);
        }
    }
}