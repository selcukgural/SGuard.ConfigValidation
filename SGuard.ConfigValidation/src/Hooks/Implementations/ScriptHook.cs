using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SGuard.ConfigValidation.Hooks.Implementations;

/// <summary>
/// Hook implementation for executing scripts (bash, PowerShell, etc.).
/// </summary>
public sealed class ScriptHook : IHook
{
    private readonly string _command;
    private readonly List<string> _arguments;
    private readonly string? _workingDirectory;
    private readonly Dictionary<string, string> _environmentVariables;
    private readonly int _timeout;
    private readonly ILogger<ScriptHook> _logger;

    /// <summary>
    /// Initializes a new instance of the ScriptHook class.
    /// </summary>
    /// <param name="command">The script command or path to execute.</param>
    /// <param name="arguments">The arguments to pass to the script.</param>
    /// <param name="workingDirectory">The working directory for script execution.</param>
    /// <param name="environmentVariables">Environment variables to set for script execution.</param>
    /// <param name="timeout">Timeout in milliseconds.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="command"/> or <paramref name="logger"/> is null.</exception>
    public ScriptHook(
        string command,
        List<string> arguments,
        string? workingDirectory,
        Dictionary<string, string> environmentVariables,
        int timeout,
        ILogger<ScriptHook> logger)
    {
        System.ArgumentNullException.ThrowIfNull(command);
        System.ArgumentNullException.ThrowIfNull(logger);

        _command = command;
        _arguments = arguments ?? new List<string>();
        _workingDirectory = workingDirectory;
        _environmentVariables = environmentVariables ?? new Dictionary<string, string>();
        _timeout = timeout;
        _logger = logger;
    }

    /// <summary>
    /// Executes the script hook asynchronously.
    /// </summary>
    public async Task ExecuteAsync(HookContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Resolve template variables in command and arguments
            var resolvedCommand = context.TemplateResolver.Resolve(_command);
            var resolvedArguments = _arguments.Select(arg => context.TemplateResolver.Resolve(arg)).ToList();
            var resolvedWorkingDirectory = _workingDirectory != null 
                ? context.TemplateResolver.Resolve(_workingDirectory) 
                : null;

            // Resolve environment variables
            var resolvedEnvVars = new Dictionary<string, string>();
            foreach (var envVar in _environmentVariables)
            {
                resolvedEnvVars[envVar.Key] = context.TemplateResolver.Resolve(envVar.Value);
            }

            _logger.LogInformation("Executing script hook: {Command} with {ArgumentCount} argument(s)", 
                resolvedCommand, resolvedArguments.Count);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = resolvedCommand,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Add arguments
            if (resolvedArguments.Count > 0)
            {
                var argumentsString = string.Join(" ", resolvedArguments.Select(arg => 
                    arg.Contains(' ') ? $"\"{arg}\"" : arg));
                processStartInfo.Arguments = argumentsString;
            }

            // Set working directory
            if (!string.IsNullOrWhiteSpace(resolvedWorkingDirectory))
            {
                processStartInfo.WorkingDirectory = resolvedWorkingDirectory;
            }

            // Set environment variables
            foreach (var envVar in resolvedEnvVars)
            {
                processStartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
            }

            using var process = new Process { StartInfo = processStartInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for completion with timeout
            var completed = await Task.Run(() => process.WaitForExit(_timeout), cancellationToken);

            if (!completed)
            {
                process.Kill();
                _logger.LogWarning("Script hook execution timed out after {Timeout}ms: {Command}", 
                    _timeout, resolvedCommand);
                return;
            }

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Script hook executed successfully: {Command}. Exit code: {ExitCode}", 
                    resolvedCommand, process.ExitCode);
            }
            else
            {
                _logger.LogWarning("Script hook execution failed: {Command}. Exit code: {ExitCode}", 
                    resolvedCommand, process.ExitCode);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    _logger.LogWarning("Script error output: {Error}", error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing script hook: {Command}", _command);
            // Don't throw - hook failures should not affect validation
        }
    }
}

