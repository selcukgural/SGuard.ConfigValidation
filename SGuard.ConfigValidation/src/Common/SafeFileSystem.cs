using System.Runtime.InteropServices;
using SGuard.ConfigValidation.Resources;
using static SGuard.ConfigValidation.Common.Throw;

namespace SGuard.ConfigValidation.Common;

/// <summary>
/// Helper class for safe cross-platform file and directory operations.
/// Provides safe areas for different operating systems and Docker containers.
/// Includes path traversal protection and symlink validation.
/// </summary>
public static class SafeFileSystem
{
    /// <summary>
    /// Gets a safe temporary directory path for the current platform.
    /// </summary>
    /// <returns>A safe temporary directory path.</returns>
    private static string GetSafeTempDirectory()
    {
        // Check if running in a Docker container
        if (IsRunningInDocker())
        {
            // Docker containers typically have /tmp with proper permissions
            return "/tmp";
        }
        
        string tempPath;

        // Use platform-specific safe temp directories
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: Use user temp directory
             tempPath = Path.GetTempPath();
            if (string.IsNullOrWhiteSpace(tempPath))
            {
                // Fallback to user profile temp
                tempPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local", "Temp");
            }
            return tempPath;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux: Use /tmp (usually world-writable with sticky bit)
            const string tmpPath = "/tmp";
            if (Directory.Exists(tmpPath) && CanWriteToDirectory(tmpPath))
            {
                return tmpPath;
            }
            // Fallback to user home temp
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tmp");
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Other OS (e.g., FreeBSD): Use system temp
            return Path.GetTempPath();
        }


        // macOS: Use system temp
        tempPath = Path.GetTempPath();

        if (string.IsNullOrWhiteSpace(tempPath))
        {
            tempPath = "/tmp";
        }

        return tempPath;
    }

    /// <summary>
    /// Creates a safe temporary directory with a unique name.
    /// </summary>
    /// <param name="prefix">Optional prefix for the directory name.</param>
    /// <returns>The path to the created directory.</returns>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when unable to create a directory due to permissions.</exception>
    public static string CreateSafeTempDirectory(string? prefix = null)
    {
        var baseTempDir = GetSafeTempDirectory();
        var directoryName = string.IsNullOrWhiteSpace(prefix)
            ? Guid.NewGuid().ToString()
            : $"{prefix}_{Guid.NewGuid()}";

        var fullPath = Path.Combine(baseTempDir, directoryName);

        try
        {
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            // Verify we can write to the directory
            if (!CanWriteToDirectory(fullPath))
            {
                throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_CannotWriteDirectory), fullPath);
            }
            return fullPath;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is DirectoryNotFoundException)
        {
            // Try a fallback location
            var fallbackPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".sguard-temp", directoryName);
            try
            {
                Directory.CreateDirectory(fallbackPath);
                return fallbackPath;
            }
            catch
            {
                throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_UnableCreateTempDirectory), ex, ex.Message);
            }
        }
    }

    /// <summary>
    /// Safely writes text to a file, creating parent directories if needed.
    /// Includes path traversal and symlink validation, and file permissions check.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="basePath">Optional base path for path traversal validation. If provided, the file path must be within the base directory.</param>
    /// <exception cref="System.ArgumentException">Thrown when filePath is null or empty.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when unable to write due to permissions, path traversal detected, or symlink attack detected.</exception>
    public static void SafeWriteAllText(string filePath, string content, string? basePath = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw ArgumentException(nameof(SR.ArgumentException_FilePathNullOrEmpty), nameof(filePath));
        }

        // Validate path traversal if a base path is provided
        if (!string.IsNullOrWhiteSpace(basePath))
        {
            var resolvedPath = Path.GetFullPath(filePath);
            var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(basePath));
            if (!string.IsNullOrWhiteSpace(baseDirectory))
            {
                PathSecurity.ValidateResolvedPath(resolvedPath, basePath);
            }
        }

        // Validate symlink if a base path is provided
        if (!string.IsNullOrWhiteSpace(basePath) && PathSecurity.IsSymlink(filePath))
        {
            PathSecurity.ValidateSymlink(filePath, basePath);
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                var fullPath = Path.GetFullPath(directory);
                throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_CannotCreateDirectory), ex, 
                    directory, fullPath, ex.GetType().Name, ex.Message);
            }
        }

        // Check file write permissions
        if (!CanWriteFile(filePath))
        {
            var fullPath = Path.GetFullPath(filePath);
            throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_InsufficientWritePermissions), 
                filePath, fullPath);
        }

        try
        {
            File.WriteAllText(filePath, content);
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (DirectoryNotFoundException ex)
        {
            var fullPath = Path.GetFullPath(filePath);
            throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_DirectoryNotFound), ex, 
                filePath, fullPath, ex.Message);
        }
        catch (Exception ex)
        {
            var fullPath = Path.GetFullPath(filePath);
            throw InvalidOperationException(nameof(SR.InvalidOperationException_FileWriteUnexpectedError), ex, 
                filePath, fullPath, ex.GetType().Name, ex.Message);
        }
    }

    /// <summary>
    /// Checks if the application is running inside a Docker container.
    /// </summary>
    /// <returns>True if running in Docker, false otherwise.</returns>
    private static bool IsRunningInDocker()
    {
        try
        {
            // Check for the.dockerenv file (common Docker indicator)
            if (File.Exists("/.dockerenv"))
            {
                return true;
            }

            // Check cgroup (Linux containers)
            if (!File.Exists("/proc/self/cgroup"))
            {
                return false;
            }

            var cgroupContent = File.ReadAllText("/proc/self/cgroup");
            return cgroupContent.Contains("docker", StringComparison.OrdinalIgnoreCase) ||
                   cgroupContent.Contains("containerd", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // If we can't check, assume not in Docker
            return false;
        }
    }

    /// <summary>
    /// Checks if a file has read permissions.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>True if the file can be read, false otherwise.</returns>
    public static bool CanReadFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        try
        {
            using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return fileStream.CanRead;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a file has write permissions.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>True if the file can be written, false otherwise.</returns>
    public static bool CanWriteFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        try
        {
            // If a file exists, check write access
            if (File.Exists(filePath))
            {
                using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Write, FileShare.None);
                return fileStream.CanWrite;
            }

            // If a file doesn't exist, check if the directory is writable
            var directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = Directory.GetCurrentDirectory();
            }

            return CanWriteToDirectory(directory);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the current process can write to the specified directory.
    /// </summary>
    /// <param name="directoryPath">The directory path to check.</param>
    /// <returns>True if writable, false otherwise.</returns>
    private static bool CanWriteToDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return false;
        }

        try
        {
            var testFile = Path.Combine(directoryPath, $"test_{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safely reads all text from a file with proper error handling.
    /// Includes path traversal and symlink validation, and file permissions check.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="basePath">Optional base path for path traversal validation. If provided, the file path must be within the base directory.</param>
    /// <returns>The content of the file.</returns>
    /// <exception cref="System.ArgumentException">Thrown when filePath is null or empty.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when a file does not exist.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when unable to read due to permissions, path traversal detected, or symlink attack detected.</exception>
    public static string SafeReadAllText(string filePath, string? basePath = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw ArgumentException(nameof(SR.ArgumentException_FilePathNullOrEmpty), nameof(filePath));
        }

        // Validate path traversal if a base path is provided
        if (!string.IsNullOrWhiteSpace(basePath))
        {
            var resolvedPath = Path.GetFullPath(filePath);
            var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(basePath));
            if (!string.IsNullOrWhiteSpace(baseDirectory))
            {
                PathSecurity.ValidateResolvedPath(resolvedPath, basePath);
            }
        }

        // Validate symlink if a base path is provided
        if (!string.IsNullOrWhiteSpace(basePath) && PathSecurity.IsSymlink(filePath))
        {
            PathSecurity.ValidateSymlink(filePath, basePath);
        }

        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
        {
            throw FileNotFoundException(filePath, nameof(SR.FileNotFoundException_FileNotFound), filePath, fullPath);
        }

        // Check file read permissions
        if (!CanReadFile(fullPath))
        {
            throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_InsufficientReadPermissions), 
                filePath, fullPath);
        }

        try
        {
            return File.ReadAllText(filePath);
        }
        catch (DirectoryNotFoundException ex)
        {
            throw FileNotFoundException(filePath, nameof(SR.FileNotFoundException_DirectoryNotFound), filePath, fullPath, ex.Message);
        }
        catch (IOException ex)
        {
            throw InvalidOperationException(nameof(SR.InvalidOperationException_FileWriteUnexpectedError), ex, 
                filePath, fullPath, ex.GetType().Name, ex.Message);
        }
    }

    /// <summary>
    /// Safely checks if a file exists.
    /// Includes path traversal validation if a base path is provided.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="basePath">Optional base path for path traversal validation. If provided, the file path must be within the base directory.</param>
    /// <returns>True if a file exists, false otherwise.</returns>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when path traversal is detected or symlink attack is detected.</exception>
    public static bool FileExists(string filePath, string? basePath = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        try
        {
            // Validate path traversal if a base path is provided
            if (!string.IsNullOrWhiteSpace(basePath))
            {
                var resolvedPath = Path.GetFullPath(filePath);
                var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(basePath));
                if (!string.IsNullOrWhiteSpace(baseDirectory))
                {
                    // Only validate if a path exists (to avoid false positives for non-existent paths)
                    if (File.Exists(filePath) || Directory.Exists(filePath))
                    {
                        PathSecurity.ValidateResolvedPath(resolvedPath, basePath);
                    }
                }
            }

            // Validate symlink if a base path is provided
            if (!string.IsNullOrWhiteSpace(basePath) && PathSecurity.IsSymlink(filePath))
            {
                PathSecurity.ValidateSymlink(filePath, basePath);
            }

            return File.Exists(filePath);
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw security violations
            throw;
        }
        catch
        {
            // If we can't check (e.g., invalid path), return false
            return false;
        }
    }

    /// <summary>
    /// Safely deletes a directory and all its contents.
    /// </summary>
    /// <param name="directoryPath">The directory path to delete.</param>
    /// <param name="recursive">Whether to delete it recursively.</param>
    public static void SafeDeleteDirectory(string directoryPath, bool recursive = true)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return;
        }

        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        try
        {
            Directory.Delete(directoryPath, recursive);
        }
        catch (Exception ex) when (ex is System.UnauthorizedAccessException or IOException)
        {
            // Log but don't throw - cleanup failures shouldn't break tests
            // In production, you might want to log this
        }
    }
}

