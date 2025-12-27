namespace SGuard.ConfigValidation.Models;

/// <summary>
/// Configuration for a single hook.
/// </summary>
public sealed class HookConfig
{
    /// <summary>
    /// Gets or sets the hook type (e.g., "script", "webhook").
    /// </summary>
    public required string Type { get; set; }


    /// <summary>
    /// Gets or sets the command or script path for script hooks.
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// Gets or sets the arguments for script hooks.
    /// </summary>
    public List<string>? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the working directory for script hooks.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets the environment variables for script hooks.
    /// </summary>
    public Dictionary<string, string>? EnvironmentVariables { get; set; }

    /// <summary>
    /// Gets or sets the timeout in milliseconds for script or webhook hooks.
    /// </summary>
    public int? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the URL for webhook hooks.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method for webhook hooks (default: "POST").
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// Gets or sets the HTTP headers for webhook hooks.
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Gets or sets the request body for webhook hooks.
    /// Can be a JSON object or a template string with variables like {{status}}.
    /// </summary>
    public object? Body { get; set; }

}


