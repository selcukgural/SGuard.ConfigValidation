using System.CommandLine;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Hooks;
using SGuard.ConfigValidation.Output;
using System.Runtime.InteropServices;
using SGuard.ConfigValidation.Results;
using SGuard.ConfigValidation.Services.Abstract;

namespace SGuard.ConfigChecker.Console;

/// <summary>
/// Command-line interface for SGuard configuration validation.
/// </summary>
public sealed class SGuardCli
{
    private readonly IRuleEngine _ruleEngine;
    private readonly IConfigLoader _configLoader;
    private readonly HookExecutor _hookExecutor;
    private readonly ILogger<SGuardCli> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOutputFormatter _outputFormatter;
    private readonly RootCommand _rootCommand;
    private static readonly string[] First = ["validate"];
    private static readonly string[] ValidFormats = ["json", "text", "console"];

    /// <summary>
    /// Initializes a new instance of the SGuardCli class.
    /// </summary>
    /// <param name="ruleEngine">The rule engine service.</param>
    /// <param name="configLoader">The configuration loader service.</param>
    /// <param name="hookExecutor">The hook executor service.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="loggerFactory">Logger factory for creating loggers.</param>
    /// <param name="outputFormatter">Optional output formatter. Defaults to ConsoleOutputFormatter.</param>
    public SGuardCli(IRuleEngine ruleEngine, IConfigLoader configLoader, HookExecutor hookExecutor, ILogger<SGuardCli> logger, ILoggerFactory loggerFactory, IOutputFormatter? outputFormatter = null)
    {
        ArgumentNullException.ThrowIfNull(ruleEngine);
        ArgumentNullException.ThrowIfNull(configLoader);
        ArgumentNullException.ThrowIfNull(hookExecutor);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        
        _ruleEngine = ruleEngine;
        _configLoader = configLoader;
        _hookExecutor = hookExecutor;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _outputFormatter = outputFormatter ?? new ConsoleOutputFormatter(loggerFactory.CreateLogger<ConsoleOutputFormatter>());
        _rootCommand = CreateRootCommand();
    }

    /// <summary>
    /// Creates the root command with all options and handlers.
    /// </summary>
    private RootCommand CreateRootCommand()
    {
        var configOption = new Option<string>("--config", "-c")
        {
            Description = "Path to the configuration file (default: sguard.json)"
        };

        var envOption = new Option<string?>("--env", "-e")
        {
            Description = "Environment ID to validate (if not specified, all environments are validated)"
        };

        var allOption = new Option<bool>("--all", "-a")
        {
            Description = "Validate all environments"
        };

        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "Output format: json, text, or console (default: console)"
        };

        var outputFileOption = new Option<string?>("--output-file", "-f")
        {
            Description = "Path to output file. If specified, results will be written to this file instead of console. Works with both json and text formats."
        };

        var verboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Enable verbose logging"
        };

        // Note: --env and --all conflicts will be checked in the handler

        var validateCommand = new Command("validate", "Validate configuration files against rules");
        validateCommand.Options.Add(configOption);
        validateCommand.Options.Add(envOption);
        validateCommand.Options.Add(allOption);
        validateCommand.Options.Add(outputOption);
        validateCommand.Options.Add(outputFileOption);
        validateCommand.Options.Add(verboseOption);

        // Use SetAction for proper exit code management (System.CommandLine 2.0.1 API)
        validateCommand.SetAction(async parseResult =>
        {
            // Get values from ParseResult (with default values)
            var configPath = parseResult.GetValue(configOption);

            if (string.IsNullOrWhiteSpace(configPath))
            {
                configPath = "sguard.json";
            }

            // Sanitize and validate config path
            var configPathValidation = ValidateAndSanitizePath(configPath, "config path", _logger);

            if (!configPathValidation.IsValid)
            {
                await System.Console.Error.WriteLineAsync($"Invalid config path: {configPathValidation.ErrorMessage}");
                return (int)ExitCode.SystemError;
            }

            configPath = configPathValidation.SanitizedPath;

            var environmentId = parseResult.GetValue(envOption);

            // Sanitize and validate environment ID if provided
            if (!string.IsNullOrWhiteSpace(environmentId))
            {
                var envIdValidation = ValidateAndSanitizeEnvironmentId(environmentId, _logger);

                if (!envIdValidation.IsValid)
                {
                    await System.Console.Error.WriteLineAsync($"Invalid environment ID: {envIdValidation.ErrorMessage}");
                    return (int)ExitCode.SystemError;
                }

                environmentId = envIdValidation.SanitizedId;
            }

            var allEnvironments = parseResult.GetValue(allOption);

            var outputFormat = parseResult.GetValue(outputOption);

            if (string.IsNullOrWhiteSpace(outputFormat))
            {
                outputFormat = "console";
            }

            var outputFile = parseResult.GetValue(outputFileOption);

            var verbose = parseResult.GetValue(verboseOption);

            // Validate output format
            if (!string.IsNullOrWhiteSpace(outputFormat))
            {
                if (!ValidFormats.Contains(outputFormat.ToLowerInvariant()))
                {
                    await System.Console.Error.WriteLineAsync($"Invalid output format: {outputFormat}. Supported formats: json, text, console");
                    return (int)ExitCode.SystemError;
                }
            }

            // Validate output file path if provided
            if (!string.IsNullOrWhiteSpace(outputFile))
            {
                var outputFileValidation = ValidateAndSanitizePath(outputFile, "output file path", _logger);
                if (!outputFileValidation.IsValid)
                {
                    await System.Console.Error.WriteLineAsync($"Invalid output file path: {outputFileValidation.ErrorMessage}");
                    return (int)ExitCode.SystemError;
                }
                outputFile = outputFileValidation.SanitizedPath;
            }

            // Check for --env and --all conflict
            if (allEnvironments && !string.IsNullOrWhiteSpace(environmentId))
            {
                _logger.LogError("Invalid command-line arguments: Both --env and --all options specified. Environment ID: {EnvironmentId}",
                                 environmentId);

                await System.Console.Error.WriteLineAsync("Error: Invalid command-line arguments. " +
                                                          $"Cannot specify both --env ('{environmentId}') and --all options simultaneously. " +
                                                          "Please use either --all to validate all environments or --env to validate a specific environment, but not both.");
                return (int)ExitCode.SystemError;
            }

            var exitCode = await HandleValidateCommand(configPath, environmentId, allEnvironments, outputFormat, outputFile, verbose);
            return (int)exitCode;
        });

        var rootCommand = new RootCommand("SGuard ConfigChecker - A lightweight tool to catch critical configuration issues before runtime")
        {
            validateCommand
        };

        // Set validating as the default command
        rootCommand.TreatUnmatchedTokensAsErrors = false;

        return rootCommand;
    }

    /// <summary>
    /// Runs the CLI with the specified arguments.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code indicating the result of the operation.</returns>
    public async Task<ExitCode> RunAsync(string[] args)
    {
        try
        {
            int invokeExitCode;

            // If no arguments provided or first argument is not a command, default to validate
            if (args.Length == 0 || (args.Length > 0 && args[0] != "validate" && !args[0].StartsWith('-')))
            {
                // Prepend "validate" if not present
                var newArgs = First.Concat(args).ToArray();
                var parseResult = _rootCommand.Parse(newArgs);
                invokeExitCode = await parseResult.InvokeAsync();
            }
            else
            {
                var parseResult = _rootCommand.Parse(args);
                invokeExitCode = await parseResult.InvokeAsync();
            }

            // System.CommandLine will return the exit code from SetAction
            return (ExitCode)invokeExitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during CLI execution. Exception type: {ExceptionType}, Message: {ErrorMessage}", ex.GetType().Name,
                             ex.Message);
            
            return ExitCode.SystemError;
        }
    }

    /// <summary>
    /// Handles the validate command execution.
    /// </summary>
    private async Task<ExitCode> HandleValidateCommand(string configPath, string? environmentId, bool allEnvironments, string outputFormat,
                                                       string? outputFile, bool verbose)
    {
        if (verbose)
        {
            _logger.LogInformation("Verbose mode enabled");
        }

        // Get the appropriate output formatter
        IOutputFormatter formatter;
        if (!string.IsNullOrWhiteSpace(outputFile))
        {
            // File output requested
            formatter = OutputFormatterFactory.Create(outputFormat, _loggerFactory, outputFile);
            _logger.LogInformation("Writing validation results to file: {OutputFile}", outputFile);
        }
        else if (!outputFormat.Equals("console", StringComparison.InvariantCultureIgnoreCase))
        {
            // Console output with specific format
            formatter = OutputFormatterFactory.Create(outputFormat, _loggerFactory);
        }
        else
        {
            // Default console output
            formatter = _outputFormatter;
        }

        try
        {
            RuleEngineResult result;

            if (allEnvironments || string.IsNullOrWhiteSpace(environmentId))
            {
                _logger.LogInformation("Validating all environments from {ConfigPath}", configPath);

                result = IsJsonContent(configPath)
                             ? await _ruleEngine.ValidateAllEnvironmentsFromJsonAsync(configPath)
                             : await _ruleEngine.ValidateAllEnvironmentsAsync(configPath);
            }
            else
            {
                _logger.LogInformation("Validating environment {EnvironmentId} from {ConfigPath}", environmentId, configPath);

                result = IsJsonContent(configPath)
                             ? await _ruleEngine.ValidateEnvironmentFromJsonAsync(configPath, environmentId)
                             : await _ruleEngine.ValidateEnvironmentAsync(configPath, environmentId);
            }

            await formatter.FormatAsync(result);

            // Execute hooks after validation (non-blocking, failures don't affect exit code)
            try
            {
                var config = await _configLoader.LoadConfigAsync(configPath);
                await _hookExecutor.ExecuteHooksAsync(result, config, environmentId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to execute hooks. This does not affect validation result.");
                // Don't throw - hook failures should not affect validation
            }

            var exitCode = result is { IsSuccess: true, HasValidationErrors: false } ? ExitCode.Success : ExitCode.ValidationErrors;

            return exitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Fatal error during validation. Exception type: {ExceptionType}, Message: {ErrorMessage}, Config path: {ConfigPath}, Environment: {EnvironmentId}",
                ex.GetType().Name, ex.Message, configPath, environmentId ?? "all");
            
            return ExitCode.SystemError;
        }
    }

    private static bool IsJsonContent(string pathOrContent)
    {
        return pathOrContent.StartsWith('{') || pathOrContent.Contains("\"version\"");
    }

    /// <summary>
    /// Validates and sanitizes a file path input.
    /// Prevents path traversal attacks and dangerous characters.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="parameterName">The name of the parameter for error messages.</param>
    /// <param name="logger">Logger instance for logging warnings.</param>
    /// <returns>A validation result with sanitized path or error message.</returns>
    private static (bool IsValid, string SanitizedPath, string ErrorMessage) ValidateAndSanitizePath(
        string path, string parameterName, ILogger<SGuardCli> logger)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return (false, path,
                    $"Input validation failed: {parameterName} is required but was null or empty. " + "Please provide a valid file path.");
        }

        // Check for path traversal attempts
        if (path.Contains("..", StringComparison.Ordinal) || path.Contains("//", StringComparison.Ordinal) ||
            (path.Length > 1 && path[0] == '/' && path[1] == '.'))
        {
            logger.LogWarning("Path traversal attempt detected in {ParameterName}: {Path}", parameterName, path);

            return (false, path,
                    $"Input validation failed: Path traversal attempt detected in {parameterName}. " + $"Path value: '{path}'. " +
                    "Paths containing '..' (parent directory references) or '//' (consecutive separators) are not allowed for security reasons. " +
                    "Please use a valid relative or absolute path without path traversal sequences.");
        }

        // Check for dangerous characters (null bytes, control characters)
        if (path.Contains('\0') || path.Any(c => char.IsControl(c) && c != '\t' && c != '\n' && c != '\r'))
        {
            logger.LogWarning("Dangerous characters detected in {ParameterName}: {Path}", parameterName, path);
            
            var dangerousChars = path.Where(c => char.IsControl(c) && c != '\t' && c != '\n' && c != '\r').Distinct().ToList();

            var charInfo = dangerousChars.Count > 0
                               ? $" Control characters found: {string.Join(", ", dangerousChars.Select(c => $"U+{(int)c:X4}"))}."
                               : "";

            return (false, path,
                    $"Input validation failed: Dangerous characters detected in {parameterName}. " + $"Path value: '{path}'.{charInfo} " +
                    "Control characters (except tab, newline, carriage return) are not allowed in file paths. " +
                    "Please remove any control characters from the path.");
        }

        // Check path length (prevent DoS through extremely long paths)
        if (path.Length > SecurityConstants.MaxPathLengthHardLimit)
        {
            logger.LogWarning("Path length exceeds maximum limit in {ParameterName}: {Length} characters", parameterName, path.Length);

            return (false, path,
                    $"Input validation failed: Path length exceeds maximum limit. " + $"Path value: '{path}'. " +
                    $"Actual length: {path.Length} characters. " + $"Maximum allowed: {SecurityConstants.MaxPathLengthHardLimit} characters. " +
                    $"Exceeded by: {path.Length - SecurityConstants.MaxPathLengthHardLimit} characters. " +
                    "Please shorten the path or contact your administrator to adjust the security limits.");
        }

        // Sanitize: Remove leading/trailing whitespace and normalize path separators
        var sanitized = path.Trim();

        // Normalize path separators (Windows accepts both / and \)
        sanitized = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? sanitized.Replace('/', '\\') : sanitized.Replace('\\', '/');

        return (true, sanitized, string.Empty);
    }

    /// <summary>
    /// Validates and sanitizes an environment ID input.
    /// Prevents injection attacks and ensures a valid format.
    /// </summary>
    /// <param name="environmentId">The environment ID to validate.</param>
    /// <param name="logger">Logger instance for logging warnings.</param>
    /// <returns>A validation result with sanitized ID or error message.</returns>
    private static (bool IsValid, string SanitizedId, string ErrorMessage) ValidateAndSanitizeEnvironmentId(
        string environmentId, ILogger<SGuardCli> logger)
    {
        if (string.IsNullOrWhiteSpace(environmentId))
        {
            return (false, environmentId,
                    "Input validation failed: Environment ID is required but was null or empty. " + "Please provide a valid environment ID.");
        }

        // Check for dangerous characters
        if (environmentId.Contains('\0') || environmentId.Any(char.IsControl))
        {
            logger.LogWarning("Dangerous characters detected in environment ID: {EnvironmentId}", environmentId);
            
            var dangerousChars = environmentId.Where(char.IsControl).Distinct().ToList();

            var charInfo = dangerousChars.Count > 0
                               ? $" Control characters found: {string.Join(", ", dangerousChars.Select(c => $"U+{(int)c:X4}"))}."
                               : "";

            return (false, environmentId,
                    $"Input validation failed: Environment ID contains dangerous characters. " +
                    $"Environment ID value: '{environmentId}'.{charInfo} " + "Control characters are not allowed in environment IDs. " +
                    "Please remove any control characters from the environment ID.");
        }

        // Check length (reasonable limit for IDs)
        if (environmentId.Length > 256)
        {
            logger.LogWarning("Environment ID length exceeds maximum limit: {Length} characters", environmentId.Length);

            return (false, environmentId,
                    $"Input validation failed: Environment ID length exceeds maximum limit. " + $"Environment ID value: '{environmentId}'. " +
                    $"Actual length: {environmentId.Length} characters. " + $"Maximum allowed: 256 characters. " +
                    $"Exceeded by: {environmentId.Length - 256} characters. " + "Please shorten the environment ID.");
        }

        // Sanitize: Remove leading/trailing whitespace
        var sanitized = environmentId.Trim();

        // Validate a format: alphanumeric, dash, underscore only (common ID patterns)
        // Allow more flexible format but log if unusual characters are detected
        var hasUnusualChars = sanitized.Any(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != '.');
        if (!hasUnusualChars)
        {
            return (true, sanitized, string.Empty);
        }
        
        logger.LogWarning("Unusual characters detected in environment ID: {EnvironmentId}", environmentId);
        
        // Don't reject, but sanitize by removing dangerous characters
        sanitized = new string(sanitized.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.').ToArray());

        return (true, sanitized, string.Empty);
    }
}