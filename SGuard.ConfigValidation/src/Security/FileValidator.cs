using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Utils;

namespace SGuard.ConfigValidation.Security;

/// <summary>
/// Helper class for file security validations.
/// Provides reusable methods for file size validation and file existence checks.
/// </summary>
public static class FileValidator
{
    private const double BytesToMegabytes = 1024.0 * 1024.0;

    /// <summary>
    /// Validates that a file does not exceed the maximum allowed size.
    /// Throws a ConfigurationException if the file size exceeds the limit.
    /// </summary>
    /// <param name="filePath">The path to the file to validate.</param>
    /// <param name="maxSizeBytes">The maximum allowed file size in bytes.</param>
    /// <param name="logger">Logger instance for logging errors.</param>
    /// <param name="resourceKey">The resource key for the error message.</param>
    /// <exception cref="SGuard.ConfigValidation.Exceptions.ConfigurationException">Thrown when the file size exceeds the maximum allowed size.</exception>
    public static void ValidateFileSize(string filePath, long maxSizeBytes, ILogger logger, string resourceKey)
    {
        var fileInfo = new FileInfo(filePath);

        if (fileInfo.Length <= maxSizeBytes)
        {
            return;
        }

        var fileSizeMb = fileInfo.Length / BytesToMegabytes;
        var maxSizeMb = maxSizeBytes / BytesToMegabytes;
            
        logger.LogError(
            "File {FilePath} exceeds maximum size limit. Size: {FileSize} bytes ({FileSizeMB:F2} MB), Limit: {MaxSize} bytes ({MaxSizeMB:F2} MB)",
            filePath, fileInfo.Length, fileSizeMb, maxSizeBytes, maxSizeMb);
            
        throw Throw.ConfigurationException(resourceKey,
                                           filePath, Path.GetFullPath(filePath), fileSizeMb, fileInfo.Length, maxSizeMb, maxSizeBytes, fileSizeMb - maxSizeMb);
    }

    /// <summary>
    /// Ensures that a file exists at the specified path.
    /// Throws a FileNotFoundException if the file does not exist.
    /// </summary>
    /// <param name="filePath">The path to the file to check.</param>
    /// <param name="resourceKey">The resource key for the error message.</param>
    /// <param name="logger">Logger instance for logging errors.</param>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when the file does not exist.</exception>
    public static void EnsureFileExists(string filePath, string resourceKey, ILogger logger)
    {
        if (FileUtility.FileExists(filePath))
        {
            return;
        }

        var fullPath = Path.GetFullPath(filePath);
        logger.LogError("File not found: {FilePath} (resolved to: {FullPath})", filePath, fullPath);
        throw Throw.FileNotFoundException(filePath, resourceKey, filePath, fullPath);
    }
}

