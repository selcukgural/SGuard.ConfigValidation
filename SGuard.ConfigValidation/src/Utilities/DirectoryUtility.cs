using System.Runtime.InteropServices;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Resources;

namespace SGuard.ConfigValidation.Utilities;

public static class DirectoryUtility
{
    /// <summary>
    /// Creates a safe temporary directory with a unique name.
    /// </summary>
    /// <param name="prefix">
    /// Optional prefix for the directory name. If <c>null</c> or whitespace, only a GUID will be used as the directory name.
    /// </param>
    /// <returns>
    /// The full path to the created temporary directory. The directory is guaranteed to exist and be writable by the current process.
    /// </returns>
    /// <exception cref="System.UnauthorizedAccessException">
    /// Thrown when the directory cannot be created or is not writable by the current process, even after attempting a fallback location.
    /// </exception>
    /// <remarks>
    /// This method first attempts to create a directory under a platform-appropriate temporary location. If creation fails due to permissions or missing parent directories,
    /// it falls back to a user-specific temp directory (e.g., <c>~/.sguard-temp</c> on Unix-like systems). The method verifies that the resulting directory is writable,
    /// and throws if it is not. The returned path is always absolute and unique for each call.
    /// </remarks>
    /// <example>
    /// <code>
    /// var tempDir = DirectoryUtility.CreateTempDirectory("myjob");
    /// // Use tempDir for temporary files
    /// </code>
    /// </example>
    public static string CreateTempDirectory(string? prefix = null)
    {
        var baseTempDir = GetTempDirectory();

        var directoryName = string.IsNullOrWhiteSpace(prefix) ? Guid.NewGuid().ToString() : $"{prefix}_{Guid.NewGuid()}";

        var fullPath = Path.Combine(baseTempDir, directoryName);

        try
        {
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            // Verify we can write to the directory
            return !CanWriteToDirectory(fullPath)
                       ? throw Throw.UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_CannotWriteDirectory), fullPath)
                       : fullPath;
        }
        catch (Exception ex) when (ex is System.UnauthorizedAccessException or DirectoryNotFoundException)
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
                throw Throw.UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_UnableCreateTempDirectory), ex, ex.Message);
            }
        }
    }

    /// <summary>
    /// Safely deletes a directory and all its contents.
    /// </summary>
    /// <param name="directoryPath">
    /// The directory path to delete. Must not be null or whitespace. If the directory does not exist, the method returns without error.
    /// </param>
    /// <param name="recursive">
    /// Whether to delete the directory recursively. Defaults to <c>true</c>.
    /// </param>
    /// <remarks>
    /// This method attempts to delete the specified directory. If the directory does not exist or the path is invalid, the method returns without error.
    /// If an <see cref="System.UnauthorizedAccessException"/> or <see cref="System.IO.IOException"/> occurs, the exception is caught and ignored.
    /// This is intended for cleanup scenarios where deletion failures should not break tests or main flow. In production, consider logging such failures.
    /// </remarks>
    /// <example>
    /// <code>
    /// DirectoryUtility.DeleteDirectory("/tmp/my-temp-dir");
    /// </code>
    /// </example>
    public static void DeleteDirectory(string directoryPath, bool recursive = true)
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

    /// <summary>
    /// Checks if the current process can write to the specified directory.
    /// </summary>
    /// <param name="directoryPath">
    /// The directory path to check. Must not be null, empty, or whitespace. The directory must already exist.
    /// </param>
    /// <returns>
    /// <c>true</c> if the directory is writable by the current process; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method attempts to create and delete a temporary file in the specified directory to verify write permissions.
    /// If the directory does not exist, the method returns <c>false</c>.
    /// If an exception occurs during the write/delete operation, the method returns <c>false</c>.
    /// This method does not throw exceptions for permission or IO errors.
    /// </remarks>
    /// <example>
    /// <code>
    /// bool canWrite = DirectoryUtility.CanWriteToDirectory("/tmp/mydir");
    /// if (canWrite)
    /// {
    ///     // Safe to write files in this directory
    /// }
    /// </code>
    /// </example>
    internal static bool CanWriteToDirectory(string directoryPath)
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
    /// Returns a safe temporary directory path appropriate for the current operating system and environment.
    /// </summary>
    /// <returns>
    /// A string representing the absolute path to a temporary directory that is suitable for use by the current process.
    /// </returns>
    /// <remarks>
    /// This method determines a platform-appropriate temporary directory path, taking into account Docker environments and OS-specific conventions.
    /// - On Docker, returns <c>/tmp</c>.
    /// - On Windows, returns the user temp directory or falls back to <c>AppData\Local\Temp</c>.
    /// - On Linux, prefers <c>/tmp</c> if writable, otherwise falls back to <c>~/.tmp</c>.
    /// - On macOS and other platforms, returns the system temp directory or <c>/tmp</c> as a fallback.
    /// The returned directory may not be unique per call; callers should create subdirectories as needed.
    /// </remarks>
    /// <example>
    /// <code>
    /// string tempDir = DirectoryUtility.GetTempDirectory();
    /// // Use Path.Combine(tempDir, "myfile.txt") for temporary files
    /// </code>
    /// </example>
    public static string GetTempDirectory()
    {
        // Check if running in a Docker container
        if (FileUtility.IsRunningInDocker())
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
}