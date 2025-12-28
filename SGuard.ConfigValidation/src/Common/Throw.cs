using SGuard.ConfigValidation.Exceptions;
using SGuard.ConfigValidation.Resources;

namespace SGuard.ConfigValidation.Common;

/// <summary>
/// Helper class for creating exceptions with messages from resource files.
/// Provides a clean API: throw ArgumentNullException(...), throw ArgumentException(...), etc.
/// Use with: using static SGuard.ConfigValidation.Common.Throw;
/// </summary>
public static class Throw
{
    /// <summary>
    /// Creates an <see cref="System.ArgumentNullException"/> with a message from resources.
    /// </summary>
    /// <param name="resourceKey">The resource key for the error message.</param>
    /// <param name="paramName">The name of the parameter that is null.</param>
    /// <param name="args">Optional arguments for formatting the message.</param>
    /// <returns>An <see cref="System.ArgumentNullException"/> instance.</returns>
    public static ArgumentNullException ArgumentNullException(string resourceKey, string paramName, params object[] args)
    {
        var message = GetResourceString(resourceKey, args);
        return new ArgumentNullException(paramName, message);
    }

    /// <summary>
    /// Creates an <see cref="System.ArgumentException"/> with a message from resources.
    /// </summary>
    /// <param name="resourceKey">The resource key for the error message.</param>
    /// <param name="paramName">The name of the parameter that caused the exception.</param>
    /// <param name="args">Optional arguments for formatting the message.</param>
    /// <returns>An <see cref="System.ArgumentException"/> instance.</returns>
    public static ArgumentException ArgumentException(string resourceKey, string paramName, params object[] args)
    {
        var message = GetResourceString(resourceKey, args);
        return new ArgumentException(message, paramName);
    }

    /// <summary>
    /// Creates a <see cref="SGuard.ConfigValidation.Exceptions.ConfigurationException"/> with a message from resources.
    /// </summary>
    /// <param name="resourceKey">The resource key for the error message.</param>
    /// <param name="args">Optional arguments for formatting the message.</param>
    /// <returns>A <see cref="SGuard.ConfigValidation.Exceptions.ConfigurationException"/> instance.</returns>
    public static ConfigurationException ConfigurationException(string resourceKey, params object[] args)
    {
        var message = GetResourceString(resourceKey, args);
        return new ConfigurationException(message);
    }

    /// <summary>
    /// Creates a <see cref="SGuard.ConfigValidation.Exceptions.ConfigurationException"/> with a message from resources and an inner exception.
    /// </summary>
    /// <param name="resourceKey">The resource key for the error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="args">Optional arguments for formatting the message.</param>
    /// <returns>A <see cref="SGuard.ConfigValidation.Exceptions.ConfigurationException"/> instance.</returns>
    public static ConfigurationException ConfigurationException(string resourceKey, Exception innerException, params object[] args)
    {
        var message = GetResourceString(resourceKey, args);
        return new ConfigurationException(message, innerException);
    }

    /// <summary>
    /// Creates a <see cref="System.IO.FileNotFoundException"/> with a message from resources.
    /// </summary>
    /// <param name="fileName">The name of the file that was not found.</param>
    /// <param name="resourceKey">The resource key for the error message.</param>
    /// <param name="args">Optional arguments for formatting the message.</param>
    /// <returns>A <see cref="System.IO.FileNotFoundException"/> instance.</returns>
    public static FileNotFoundException FileNotFoundException(string fileName, string resourceKey, params object[] args)
    {
        var message = GetResourceString(resourceKey, args);
        return new FileNotFoundException(message, fileName);
    }

    /// <summary>
    /// Creates an <see cref="System.InvalidOperationException"/> with a message from resources.
    /// </summary>
    /// <param name="resourceKey">The resource key for the error message.</param>
    /// <param name="args">Optional arguments for formatting the message.</param>
    /// <returns>An <see cref="System.InvalidOperationException"/> instance.</returns>
    public static InvalidOperationException InvalidOperationException(string resourceKey, params object[] args)
    {
        var message = GetResourceString(resourceKey, args);
        return new InvalidOperationException(message);
    }

    /// <summary>
    /// Creates an <see cref="System.InvalidOperationException"/> with a message from resources and an inner exception.
    /// </summary>
    /// <param name="resourceKey">The resource key for the error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="args">Optional arguments for formatting the message.</param>
    /// <returns>An <see cref="System.InvalidOperationException"/> instance.</returns>
    public static InvalidOperationException InvalidOperationException(string resourceKey, Exception innerException, params object[] args)
    {
        var message = GetResourceString(resourceKey, args);
        return new InvalidOperationException(message, innerException);
    }

    /// <summary>
    /// Creates an <see cref="System.UnauthorizedAccessException"/> with a message from resources.
    /// </summary>
    /// <param name="resourceKey">The resource key for the error message.</param>
    /// <param name="args">Optional arguments for formatting the message.</param>
    /// <returns>An <see cref="System.UnauthorizedAccessException"/> instance.</returns>
    public static UnauthorizedAccessException UnauthorizedAccessException(string resourceKey, params object[] args)
    {
        var message = GetResourceString(resourceKey, args);
        return new UnauthorizedAccessException(message);
    }

    /// <summary>
    /// Creates an <see cref="System.UnauthorizedAccessException"/> with a message from resources and an inner exception.
    /// </summary>
    /// <param name="resourceKey">The resource key for the error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="args">Optional arguments for formatting the message.</param>
    /// <returns>An <see cref="System.UnauthorizedAccessException"/> instance.</returns>
    public static UnauthorizedAccessException UnauthorizedAccessException(string resourceKey, Exception innerException, params object[] args)
    {
        var message = GetResourceString(resourceKey, args);
        return new UnauthorizedAccessException(message, innerException);
    }

    /// <summary>
    /// Creates a <see cref="System.NotSupportedException"/> with a message from resources.
    /// </summary>
    /// <param name="resourceKey">The resource key for the error message.</param>
    /// <param name="args">Optional arguments for formatting the message.</param>
    /// <returns>A <see cref="System.NotSupportedException"/> instance.</returns>
    public static NotSupportedException NotSupportedException(string resourceKey, params object[] args)
    {
        var message = GetResourceString(resourceKey, args);
        return new NotSupportedException(message);
    }
    
    /// <summary>
    /// Determines if an exception is a critical system exception that should be re-thrown immediately.
    /// Critical exceptions indicate severe system-level problems that cannot be handled by normal error handling.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns><c>true</c> if the exception is critical and should be re-thrown; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// Critical exceptions include:
    /// - <see cref="OutOfMemoryException"/>: Indicates insufficient memory
    /// - <see cref="StackOverflowException"/>: Indicates stack overflow
    /// - <see cref="AccessViolationException"/>: Indicates invalid memory access
    /// - <see cref="BadImageFormatException"/>: Indicates invalid assembly format
    /// - <see cref="InvalidProgramException"/>: Indicates invalid program execution
    /// 
    /// These exceptions should not be caught and handled as normal errors, as they indicate
    /// fundamental system problems that require immediate termination or re-throwing.
    /// </remarks>
    public static bool IsCriticalException(Exception ex)
    {
        return ex is OutOfMemoryException
            or StackOverflowException
            or AccessViolationException
            or BadImageFormatException
            or InvalidProgramException;
    }

    /// <summary>
    /// Gets a resource string and formats it with the provided arguments.
    /// </summary>
    /// <param name="resourceKey">The resource key.</param>
    /// <param name="args">Optional arguments for formatting.</param>
    /// <returns>The formatted resource string.</returns>
    private static string GetResourceString(string resourceKey, params object[] args)
    {
        // Fallback to a resource key if not found (should not happen in production)
        var message = SR.ResourceManager.GetString(resourceKey) ?? $"Resource key '{resourceKey}' not found.";

        if (args is not { Length: > 0 })
        {
            return message;
        }

        try
        {
            message = string.Format(message, args);
        }
        catch (FormatException)
        {
            // If formatting fails, return the unformatted message
            // This should not happen if resource strings are correct
        }

        return message;
    }
}
