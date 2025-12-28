using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Common;

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
    private readonly SecurityOptions _securityOptions;

    /// <summary>
    /// Initializes a new instance of the ScriptHook class.
    /// </summary>
    /// <param name="command">The script command or path to execute.</param>
    /// <param name="arguments">The arguments to pass to the script.</param>
    /// <param name="workingDirectory">The working directory for script execution.</param>
    /// <param name="environmentVariables">Environment variables to set for script execution.</param>
    /// <param name="timeout">Timeout in milliseconds.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="securityOptions">Security options for buffer size limits.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="command"/>, <paramref name="logger"/>, or <paramref name="securityOptions"/> is null.</exception>
    public ScriptHook(
        string command,
        List<string> arguments,
        string? workingDirectory,
        Dictionary<string, string> environmentVariables,
        int timeout,
        ILogger<ScriptHook> logger,
        SecurityOptions securityOptions)
    {
        System.ArgumentNullException.ThrowIfNull(command);
        System.ArgumentNullException.ThrowIfNull(logger);
        System.ArgumentNullException.ThrowIfNull(securityOptions);

        _command = command;
        _arguments = arguments ?? new List<string>();
        _workingDirectory = workingDirectory;
        _environmentVariables = environmentVariables ?? new Dictionary<string, string>();
        _timeout = timeout;
        _logger = logger;
        _securityOptions = securityOptions;
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

            Process? process = null;
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            var outputSizeBytes = 0L;
            var errorSizeBytes = 0L;
            var outputLimitExceeded = false;
            var errorLimitExceeded = false;

            try
            {
                process = new Process { StartInfo = processStartInfo };

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null && !outputLimitExceeded)
                    {
                        var dataBytes = System.Text.Encoding.UTF8.GetByteCount(e.Data + Environment.NewLine);
                        if (outputSizeBytes + dataBytes > _securityOptions.MaxScriptOutputSizeBytes)
                        {
                            outputLimitExceeded = true;
                            _logger.LogWarning(
                                "Script output buffer limit ({Limit} bytes) exceeded. Killing process: {Command}",
                                _securityOptions.MaxScriptOutputSizeBytes, resolvedCommand);
                            
                            try
                            {
                                if (process != null && !process.HasExited)
                                {
                                    process.Kill();
                                }
                            }
                            catch (Exception killEx)
                            {
                                _logger.LogWarning(killEx, "Error killing process after output limit exceeded: {Command}", resolvedCommand);
                            }
                            return;
                        }
                        
                        outputSizeBytes += dataBytes;
                        outputBuilder.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null && !errorLimitExceeded)
                    {
                        var dataBytes = System.Text.Encoding.UTF8.GetByteCount(e.Data + Environment.NewLine);
                        if (errorSizeBytes + dataBytes > _securityOptions.MaxScriptOutputSizeBytes)
                        {
                            errorLimitExceeded = true;
                            _logger.LogWarning(
                                "Script error output buffer limit ({Limit} bytes) exceeded. Killing process: {Command}",
                                _securityOptions.MaxScriptOutputSizeBytes, resolvedCommand);
                            
                            try
                            {
                                if (process != null && !process.HasExited)
                                {
                                    process.Kill();
                                }
                            }
                            catch (Exception killEx)
                            {
                                _logger.LogWarning(killEx, "Error killing process after error output limit exceeded: {Command}", resolvedCommand);
                            }
                            return;
                        }
                        
                        errorSizeBytes += dataBytes;
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
                    try
                    {
                        process.Kill();
                        _logger.LogWarning("Script hook execution timed out after {Timeout}ms: {Command}", 
                            _timeout, resolvedCommand);
                    }
                    catch (Exception killEx)
                    {
                        _logger.LogWarning(killEx, "Error killing process after timeout: {Command}", resolvedCommand);
                    }
                    return;
                }

                var output = outputBuilder.ToString();
                var error = errorBuilder.ToString();

                // Truncate output if limit was exceeded (keep first N bytes)
                if (outputLimitExceeded)
                {
                    var maxChars = Math.Min(output.Length, (int)(_securityOptions.MaxScriptOutputSizeBytes / 2)); // Approximate char count
                    output = output.Length > maxChars 
                        ? output.Substring(0, maxChars) + $"{Environment.NewLine}... (truncated, output limit exceeded)"
                        : output;
                }

                if (errorLimitExceeded)
                {
                    var maxChars = Math.Min(error.Length, (int)(_securityOptions.MaxScriptOutputSizeBytes / 2)); // Approximate char count
                    error = error.Length > maxChars 
                        ? error.Substring(0, maxChars) + $"{Environment.NewLine}... (truncated, error output limit exceeded)"
                        : error;
                }

                if (outputLimitExceeded || errorLimitExceeded)
                {
                    _logger.LogWarning(
                        "Script hook output truncated due to buffer limit: {Command}. Output size: {OutputSize} bytes, Error size: {ErrorSize} bytes, Limit: {Limit} bytes",
                        resolvedCommand, outputSizeBytes, errorSizeBytes, _securityOptions.MaxScriptOutputSizeBytes);
                }

                if (process.ExitCode == 0 && !outputLimitExceeded && !errorLimitExceeded)
                {
                    _logger.LogInformation("Script hook executed successfully: {Command}. Exit code: {ExitCode}", 
                        resolvedCommand, process.ExitCode);
                }
                else
                {
                    var reason = outputLimitExceeded || errorLimitExceeded 
                        ? " (output buffer limit exceeded)" 
                        : string.Empty;
                    _logger.LogWarning("Script hook execution failed: {Command}. Exit code: {ExitCode}{Reason}", 
                        resolvedCommand, process.ExitCode, reason);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        _logger.LogWarning("Script error output: {Error}", error);
                    }
                }
            }
            finally
            {
                // Ensure process is properly disposed even in case of exceptions or timeouts
                if (process != null)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            try
                            {
                                process.Kill();
                            }
                            catch (Exception killEx)
                            {
                                _logger.LogWarning(killEx, "Error killing process during disposal: {Command}", resolvedCommand);
                            }
                        }

                        // Wait a bit for the process to exit after kill
                        if (!process.HasExited)
                        {
                            try
                            {
                                process.WaitForExit(1000);
                            }
                            catch (Exception waitEx)
                            {
                                _logger.LogWarning(waitEx, "Error waiting for process exit during disposal: {Command}", resolvedCommand);
                            }
                        }

                        process.Dispose();
                    }
                    catch (Exception disposeEx)
                    {
                        _logger.LogWarning(disposeEx, "Error disposing process: {Command}", resolvedCommand);
                    }
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

