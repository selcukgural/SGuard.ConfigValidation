using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Security;

namespace SGuard.ConfigValidation.Hooks
{
    /// <summary>
    /// Hook implementation for executing scripts (bash, PowerShell, etc.).
    /// </summary>
    /// <remarks>
    /// This class executes an external process with redirected output and error streams.
    /// It enforces output size limits and a timeout and logs important events.
    /// Thread-safety: instances are not guaranteed thread-safe; ExecuteAsync is intended to be called once per hook invocation.
    /// </remarks>
    public sealed class ScriptHook : IHook
    {
        private readonly string _command;
        private readonly List<string> _arguments;
        private readonly string? _workingDirectory;
        private readonly Dictionary<string, string> _environmentVariables;
        private readonly int _timeout;
        private readonly SecurityOptions _securityOptions;
        private readonly ILogger<ScriptHook> _logger;

        /// <summary>
        /// Initializes a new instance of the ScriptHook class.
        /// </summary>
        /// <param name="command">The script command or path to execute. Must not be null or empty.</param>
        /// <param name="arguments">The arguments to pass to the script. Nullable; empty list used when null.</param>
        /// <param name="workingDirectory">The working directory for script execution. Nullable.</param>
        /// <param name="environmentVariables">Environment variables to set for script execution. Nullable.</param>
        /// <param name="timeout">Timeout in milliseconds (applies to process execution).</param>
        /// <param name="securityOptions">Security options for configuring security limits. Must not be null.</param>
        /// <param name="logger">Logger instance. Must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/>, <paramref name="securityOptions"/>, or <paramref name="logger"/> is null.</exception>
        /// <remarks>
        /// Constructor performs basic null checks only. Additional validation (e.g. path existence) is caller responsibility.
        /// </remarks>
        public ScriptHook(string command, List<string>? arguments, string? workingDirectory, Dictionary<string, string>? environmentVariables,
                          int timeout, SecurityOptions securityOptions, ILogger<ScriptHook> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(securityOptions);

            _command = command;
            _arguments = arguments ?? new List<string>();
            _workingDirectory = workingDirectory;
            _environmentVariables = environmentVariables ?? new Dictionary<string, string>();
            _timeout = timeout;
            _securityOptions = securityOptions;
            _logger = logger;
        }

        /// <summary>
        /// Executes the configured script asynchronously, capturing stdout and stderr with safety limits.
        /// </summary>
        /// <param name="context">Hook execution context providing template resolution. Must not be null.</param>
        /// <param name="cancellationToken">Cancellation token to cancel execution. Passed to async wait operations.</param>
        /// <returns>A task representing the asynchronous execution.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
        /// <remarks>
        /// - Enforces maximum output size configured in <see cref="SecurityOptions.MaxScriptOutputSizeBytes"/>.
        /// - If output/error exceeds limit, the process is killed and output is marked as truncated.
        /// - If the execution exceeds timeout, the process is killed.
        /// - Exceptions are logged and swallowed so hook failures do not affect the caller flow.
        /// - This method passes <paramref name="cancellationToken"/> into the async wait; cancellation will attempt to stop waiting but will not forcibly kill the process beyond existing kill attempts.
        /// </remarks>
        public async Task ExecuteAsync(HookContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);

            try
            {
                // Resolve template variables in command and arguments
                var resolvedCommand = context.TemplateResolver.Resolve(_command);
                var resolvedArguments = _arguments.Select(arg => context.TemplateResolver.Resolve(arg)).ToList();
                var resolvedWorkingDirectory = _workingDirectory != null ? context.TemplateResolver.Resolve(_workingDirectory) : null;

                // Resolve environment variables
                var resolvedEnvVars = new Dictionary<string, string>();

                foreach (var envVar in _environmentVariables)
                {
                    resolvedEnvVars[envVar.Key] = context.TemplateResolver.Resolve(envVar.Value);
                }

                _logger.LogInformation("Executing script hook: {Command} with {ArgumentCount} argument(s)", resolvedCommand, resolvedArguments.Count);

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
                    // Use StringBuilder instead of string.Join + Select to reduce allocations
                    var argumentsBuilder = new StringBuilder();
                    for (var i = 0; i < resolvedArguments.Count; i++)
                    {
                        if (i > 0)
                        {
                            argumentsBuilder.Append(' ');
                        }
                        
                        var arg = resolvedArguments[i];
                        if (arg.Contains(' '))
                        {
                            argumentsBuilder.Append('"').Append(arg).Append('"');
                        }
                        else
                        {
                            argumentsBuilder.Append(arg);
                        }
                    }
                    processStartInfo.Arguments = argumentsBuilder.ToString();
                }

                // Set a working directory
                if (!string.IsNullOrWhiteSpace(resolvedWorkingDirectory))
                {
                    processStartInfo.WorkingDirectory = resolvedWorkingDirectory;
                }

                // Set environment variables
                foreach (var envVar in resolvedEnvVars)
                {
                    processStartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
                }

                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                // Use an array to hold counters so the captured variable (array reference) is not reassigned later.
                // This avoids capturing a local variable that is modified in the outer scope while still allowing
                // atomic updates to the individual long elements via Interlocked.
                var counters = new long[4];

                // Index constants for readability
                const int outputSizeIndex = 0;
                const int errorSizeIndex = 1;
                const int outputLimitExceededIndex = 2;
                const int errorLimitExceededIndex = 3;

                // Use using statement to ensure Process is properly disposed
                using (var process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = false })
                {

                    process.OutputDataReceived += (_, e) =>
                    {
                        if (e.Data == null || Interlocked.Read(ref counters[outputLimitExceededIndex]) != 0)
                        {
                            return;
                        }

                        var dataBytes = Encoding.UTF8.GetByteCount(e.Data + Environment.NewLine);
                        var newSize = Interlocked.Add(ref counters[outputSizeIndex], dataBytes);

                        if (newSize > _securityOptions.MaxScriptOutputSizeBytes)
                        {
                            if (Interlocked.CompareExchange(ref counters[outputLimitExceededIndex], 1L, 0L) != 0L)
                            {
                                return;
                            }

                            _logger.LogWarning("Script output buffer limit ({Limit} bytes) exceeded. Killing process: {Command}",
                                               _securityOptions.MaxScriptOutputSizeBytes, resolvedCommand);

                            try
                            {
                                if (process is { HasExited: false })
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

                        if (Interlocked.Read(ref counters[outputLimitExceededIndex]) == 0)
                        {
                            outputBuilder.AppendLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (_, e) =>
                    {
                        if (e.Data != null && Interlocked.Read(ref counters[errorLimitExceededIndex]) == 0)
                        {
                            var dataBytes = Encoding.UTF8.GetByteCount(e.Data + Environment.NewLine);
                            var newSize = Interlocked.Add(ref counters[errorSizeIndex], dataBytes);

                            if (newSize > _securityOptions.MaxScriptOutputSizeBytes)
                            {
                                if (Interlocked.CompareExchange(ref counters[errorLimitExceededIndex], 1L, 0L) == 0L)
                                {
                                    _logger.LogWarning("Script error buffer limit ({Limit} bytes) exceeded. Killing process: {Command}",
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
                                        _logger.LogWarning(killEx, "Error killing process after error limit exceeded: {Command}", resolvedCommand);
                                    }
                                }

                                return;
                            }

                            if (Interlocked.Read(ref counters[errorLimitExceededIndex]) == 0)
                            {
                                errorBuilder.AppendLine(e.Data);
                            }
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Wait for completion with timeout using asynchronous wait
                    var waitForExitTask = process.WaitForExitAsync(cancellationToken);
                    var delayTask = Task.Delay(_timeout, cancellationToken);
                    var finishedTask = await Task.WhenAny(waitForExitTask, delayTask).ConfigureAwait(false);

                    if (finishedTask == delayTask)
                    {
                        try
                        {
                            if (process != null && !process.HasExited)
                            {
                                process.Kill();
                            }

                            _logger.LogWarning("Script hook execution timed out after {Timeout}ms: {Command}", _timeout, resolvedCommand);
                        }
                        catch (Exception killEx)
                        {
                            _logger.LogWarning(killEx, "Error killing process after timeout: {Command}", resolvedCommand);
                        }

                        return;
                    }

                    // Ensure a process has exited
                    await waitForExitTask.ConfigureAwait(false);

                    var output = outputBuilder.ToString();
                    var error = errorBuilder.ToString();

                    // Check if the output was truncated due to the size limit
                    if (Interlocked.Read(ref counters[outputLimitExceededIndex]) == 1)
                    {
                        output += $"\n[Output truncated - exceeded limit of {_securityOptions.MaxScriptOutputSizeBytes} bytes]";
                    }

                    // Check if the error was truncated due to the size limit
                    if (Interlocked.Read(ref counters[errorLimitExceededIndex]) == 1)
                    {
                        error += $"\n[Error output truncated - exceeded limit of {_securityOptions.MaxScriptOutputSizeBytes} bytes]";
                    }

                    if (Interlocked.Read(ref counters[outputLimitExceededIndex]) == 1 || Interlocked.Read(ref counters[errorLimitExceededIndex]) == 1)
                    {
                        _logger.LogWarning(
                            "Script output buffer limit exceeded. Output size: {OutputSize} bytes, Error size: {ErrorSize} bytes, Limit: {Limit} bytes. Command: {Command}",
                            Interlocked.Read(ref counters[outputSizeIndex]), Interlocked.Read(ref counters[errorSizeIndex]),
                            _securityOptions.MaxScriptOutputSizeBytes, resolvedCommand);
                    }

                    if (process.ExitCode == 0 && Interlocked.Read(ref counters[outputLimitExceededIndex]) == 0 &&
                        Interlocked.Read(ref counters[errorLimitExceededIndex]) == 0)
                    {
                        _logger.LogInformation("Script hook executed successfully: {Command}. Exit code: {ExitCode}", resolvedCommand,
                                               process.ExitCode);

                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            _logger.LogDebug("Script output: {Output}", output);
                        }
                    }
                    else
                    {
                        var reason = (Interlocked.Read(ref counters[outputLimitExceededIndex]) == 1 ||
                                      Interlocked.Read(ref counters[errorLimitExceededIndex]) == 1)
                                         ? " (output buffer limit exceeded)"
                                         : string.Empty;

                        _logger.LogWarning("Script hook execution failed: {Command}. Exit code: {ExitCode}{Reason}", resolvedCommand,
                                           process.ExitCode, reason);

                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            _logger.LogWarning("Script error output: {Error}", error);
                        }
                    }

                    // Cleanup event handlers before disposal
                    // This ensures async event handlers are properly cancelled
                    try
                    {
                        process.CancelOutputRead();
                    }
                    catch (Exception cancelEx)
                    {
                        _logger.LogDebug(cancelEx, "Error cancelling output read: {Command}", resolvedCommand);
                    }

                    try
                    {
                        process.CancelErrorRead();
                    }
                    catch (Exception cancelEx)
                    {
                        _logger.LogDebug(cancelEx, "Error cancelling error read: {Command}", resolvedCommand);
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
}