namespace SGuard.ConfigValidation.Exceptions;

/// <summary>
/// Exception thrown when there is an error during validation.
/// This exception is used for validation-related errors such as unsupported validator types,
/// invalid validation arguments, or unexpected errors during validation operations.
/// </summary>
/// <param name="message">The error message that explains the reason for the exception.</param>
/// <param name="innerException">The exception that is the cause of the current exception.</param>
public sealed class ValidationException(string message, Exception innerException) : Exception(message, innerException);

