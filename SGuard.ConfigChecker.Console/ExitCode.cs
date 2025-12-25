namespace SGuard.ConfigChecker.Console;

/// <summary>
/// Represents the exit codes used by the application.
/// These codes follow standard Unix/Linux exit code conventions and are cross-platform compatible.
/// </summary>
public enum ExitCode
{
    /// <summary>
    /// Success - The application completed successfully with no errors.
    /// </summary>
    Success = 0,

    /// <summary>
    /// Validation errors - The application completed but validation errors were found.
    /// </summary>
    ValidationErrors = 1,

    /// <summary>
    /// System errors - The application encountered a fatal error (e.g., file not found, configuration error).
    /// </summary>
    SystemError = 2
}

