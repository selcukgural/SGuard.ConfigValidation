using System.Runtime.InteropServices;

namespace SGuard.ConfigValidation.Common;

/// <summary>
/// Helper class for safe cross-platform file and directory operations.
/// Provides safe areas for different operating systems and Docker containers.
/// </summary>
public static class SafeFileSystemHelper
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
    /// <exception cref="UnauthorizedAccessException">Thrown when unable to create a directory due to permissions.</exception>
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
            return !CanWriteToDirectory(fullPath) ? throw new UnauthorizedAccessException($"Cannot write to directory: {fullPath}") : fullPath;
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
                throw new UnauthorizedAccessException($"Unable to create temporary directory. Original error: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Safely writes text to a file, creating parent directories if needed.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="content">The content to write.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when unable to write due to permissions.</exception>
    public static void SafeWriteAllText(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
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
                throw new UnauthorizedAccessException($"Cannot create directory: {directory}", ex);
            }
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
            throw new UnauthorizedAccessException($"Directory not found for path: {filePath}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to write file: {filePath}", ex);
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
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>The content of the file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when a file does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when unable to read due to permissions.</exception>
    public static string SafeReadAllText(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}. Please ensure the file exists and the path is correct.", filePath);
        }

        try
        {
            return File.ReadAllText(filePath);
        }
        catch (DirectoryNotFoundException ex)
        {
            throw new FileNotFoundException($"Directory not found for file: {filePath}", ex);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException($"Error reading file: {filePath}", ex);
        }
    }

    /// <summary>
    /// Safely checks if a file exists.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>True if a file exists, false otherwise.</returns>
    public static bool SafeFileExists(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        try
        {
            return File.Exists(filePath);
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
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // Log but don't throw - cleanup failures shouldn't break tests
            // In production, you might want to log this
        }
    }
}

