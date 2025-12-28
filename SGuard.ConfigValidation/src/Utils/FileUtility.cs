using SGuard.ConfigValidation.Resources;
using SGuard.ConfigValidation.Security;
using static SGuard.ConfigValidation.Common.Throw;

namespace SGuard.ConfigValidation.Utils;

/// <summary>
/// Helper class for safe cross-platform file and directory operations.
/// Provides safe areas for different operating systems and Docker containers.
/// Includes path traversal protection and symlink validation.
/// </summary>
public static class FileUtility
{
    /// <summary>
    /// Safely writes text to a file, creating parent directories if needed.
    /// Includes path traversal and symlink validation, and file permissions check.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="basePath">Optional base path for path traversal validation. If provided, the file path must be within the base directory.</param>
    /// <exception cref="System.ArgumentException">Thrown when filePath is null or empty.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when unable to write due to permissions, path traversal detected, or symlink attack detected.</exception>
    public static void WriteAllText(string filePath, string content, string? basePath = null)
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
                SafeFilePath.ValidateResolvedPath(resolvedPath, basePath);
            }
        }

        // Validate symlink if a base path is provided
        if (!string.IsNullOrWhiteSpace(basePath) && SafeFilePath.IsSymlink(filePath))
        {
            SafeFilePath.ValidateSymlink(filePath, basePath);
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
                throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_CannotCreateDirectory), ex, directory,
                                                  Path.GetFullPath(directory), ex.GetType().Name, ex.Message);
            }
        }

        // Check file write permissions
        if (!CanWriteFile(filePath))
        {
            throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_InsufficientWritePermissions), filePath,
                                              Path.GetFullPath(filePath));
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
            throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_DirectoryNotFound), ex, filePath, Path.GetFullPath(filePath),
                                              ex.Message);
        }
        catch (Exception ex)
        {
            throw InvalidOperationException(nameof(SR.InvalidOperationException_FileWriteUnexpectedError), ex, filePath, Path.GetFullPath(filePath),
                                            ex.GetType().Name, ex.Message);
        }
    }

    /// <summary>
    /// Determines whether the current process is running inside a Docker container.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the application is running in a Docker container; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method checks for the presence of the <c>/.dockerenv</c> file and inspects <c>/proc/self/cgroup</c> for Docker or containerd indicators.
    /// Returns <c>false</c> if the check cannot be performed (e.g., due to missing files or permissions).
    /// Thread-safe and side effect free.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (FileUtility.IsRunningInDocker())
    /// {
    /// // Adjust temp directory or file handling for containerized environment
    /// }
    /// </code>
    /// </example>
    internal static bool IsRunningInDocker()
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
    /// Checks if the specified file can be read by the current process.
    /// </summary>
    /// <param name="filePath">
    /// The path to the file to check. Must not be null, empty, or whitespace. The file must exist.
    /// </param>
    /// <returns>
    /// <c>true</c> if the file can be read by the current process; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method attempts to open the file for reading to verify read permissions. 
    /// Returns <c>false</c> if the file does not exist, the path is invalid, or an exception occurs during the check.
    /// Does not throw exceptions for permission or IO errors.
    /// </remarks>
    /// <example>
    /// <code>
    /// bool canRead = FileUtility.CanReadFile("/tmp/myfile.txt");
    /// if (canRead)
    /// {
    /// // Safe to read the file
    /// }
    /// </code>
    /// </example>
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
    /// Checks if the specified file can be written by the current process.
    /// </summary>
    /// <param name="filePath">
    /// The path to the file. Must not be null, empty, or whitespace. If the file does not exist, the method checks if the containing directory is writable.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the file can be written by the current process; otherwise, returns <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method attempts to open the file for writing if it exists, or checks the parent directory's write permissions if it does not.
    /// Returns <c>false</c> if the file path is invalid, the file or directory is not writable, or an exception occurs during the check.
    /// Does not throw exceptions for permission or IO errors.
    /// </remarks>
    /// <example>
    /// <code>
    /// bool canWrite = FileUtility.CanWriteFile("/tmp/myfile.txt");
    /// if (canWrite)
    /// {
    ///     // Safe to write to the file
    /// }
    /// </code>
    /// </example>
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

            return DirectoryUtility.CanWriteToDirectory(directory);
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
    /// Safely reads all text from a file with proper error handling.
    /// Includes path traversal and symlink validation, and file permissions check.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="basePath">Optional base path for path traversal validation. If provided, the file path must be within the base directory.</param>
    /// <returns>The content of the file.</returns>
    /// <exception cref="System.ArgumentException">Thrown when filePath is null or empty.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when a file does not exist.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when unable to read due to permissions, path traversal detected, or symlink attack detected.</exception>
    public static string ReadAllText(string filePath, string? basePath = null)
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
                SafeFilePath.ValidateResolvedPath(resolvedPath, basePath);
            }
        }

        // Validate symlink if a base path is provided
        if (!string.IsNullOrWhiteSpace(basePath) && SafeFilePath.IsSymlink(filePath))
        {
            SafeFilePath.ValidateSymlink(filePath, basePath);
        }

        var fullPath = Path.GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            throw FileNotFoundException(filePath, nameof(SR.FileNotFoundException_FileNotFound), filePath, fullPath);
        }

        // Check file read permissions
        if (!CanReadFile(fullPath))
        {
            throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_InsufficientReadPermissions), filePath, fullPath);
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
            throw InvalidOperationException(nameof(SR.InvalidOperationException_FileWriteUnexpectedError), ex, filePath, fullPath, ex.GetType().Name,
                                            ex.Message);
        }
    }

    /// <summary>
    /// Safely reads all text from a file asynchronously with proper error handling.
    /// Includes path traversal and symlink validation, and file permissions check.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <param name="basePath">Optional base path for path traversal validation. If provided, the file path must be within the base directory.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the content of the file.</returns>
    /// <exception cref="System.ArgumentException">Thrown when filePath is null or empty.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when a file does not exist.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when unable to read due to permissions, path traversal detected, or symlink attack detected.</exception>
    public static async Task<string> ReadAllTextAsync(string filePath, string? basePath = null, CancellationToken cancellationToken = default)
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
                SafeFilePath.ValidateResolvedPath(resolvedPath, basePath);
            }
        }

        // Validate symlink if a base path is provided
        if (!string.IsNullOrWhiteSpace(basePath) && SafeFilePath.IsSymlink(filePath))
        {
            SafeFilePath.ValidateSymlink(filePath, basePath);
        }

        var fullPath = Path.GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            throw FileNotFoundException(filePath, nameof(SR.FileNotFoundException_FileNotFound), filePath, fullPath);
        }

        // Check file read permissions
        if (!CanReadFile(fullPath))
        {
            throw UnauthorizedAccessException(nameof(SR.UnauthorizedAccessException_InsufficientReadPermissions), filePath, fullPath);
        }

        try
        {
            return await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        }
        catch (DirectoryNotFoundException ex)
        {
            throw FileNotFoundException(filePath, nameof(SR.FileNotFoundException_DirectoryNotFound), filePath, fullPath, ex.Message);
        }
        catch (IOException ex)
        {
            throw InvalidOperationException(nameof(SR.InvalidOperationException_FileWriteUnexpectedError), ex, filePath, fullPath, ex.GetType().Name,
                                            ex.Message);
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
                        SafeFilePath.ValidateResolvedPath(resolvedPath, basePath);
                    }
                }
            }

            // Validate symlink if a base path is provided
            if (!string.IsNullOrWhiteSpace(basePath) && SafeFilePath.IsSymlink(filePath))
            {
                SafeFilePath.ValidateSymlink(filePath, basePath);
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
}