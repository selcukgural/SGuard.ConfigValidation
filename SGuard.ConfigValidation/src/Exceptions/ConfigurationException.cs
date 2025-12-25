namespace SGuard.ConfigValidation.Exceptions;

/// <summary>
/// Exception thrown when there is an error loading or parsing configuration files.
/// This exception is used for configuration-related errors such as invalid file format,
/// missing required fields, schema validation failures, and structure validation failures.
/// </summary>
public sealed class ConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the ConfigurationException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ConfigurationException class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception or null if no inner exception is specified.</param>
    public ConfigurationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}

