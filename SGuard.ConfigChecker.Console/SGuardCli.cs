using System.CommandLine;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Output;
using SGuard.ConfigValidation.Services;

namespace SGuard.ConfigChecker.Console;

/// <summary>
/// Command-line interface for SGuard configuration validation.
/// </summary>
public sealed class SGuardCli
{
    private readonly IRuleEngine _ruleEngine;
    private readonly ILogger<SGuardCli> _logger;
    private readonly IOutputFormatter _outputFormatter;
    private readonly RootCommand _rootCommand;
    private static readonly string[] First = ["validate"];

    /// <summary>
    /// Initializes a new instance of the SGuardCli class.
    /// </summary>
    /// <param name="ruleEngine">The rule engine service.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="outputFormatter">Optional output formatter. Defaults to ConsoleOutputFormatter.</param>
    public SGuardCli(IRuleEngine ruleEngine, ILogger<SGuardCli> logger, IOutputFormatter? outputFormatter = null)
    {
        _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _outputFormatter = outputFormatter ?? new ConsoleOutputFormatter();
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

            var environmentId = parseResult.GetValue(envOption);
            var allEnvironments = parseResult.GetValue(allOption);

            var outputFormat = parseResult.GetValue(outputOption);

            if (string.IsNullOrWhiteSpace(outputFormat))
            {
                outputFormat = "console";
            }

            var verbose = parseResult.GetValue(verboseOption);

            // Validate output format
            if (!string.IsNullOrWhiteSpace(outputFormat))
            {
                var validFormats = new[] { "json", "text", "console" };

                if (!validFormats.Contains(outputFormat.ToLowerInvariant()))
                {
                    await System.Console.Error.WriteLineAsync($"Invalid output format: {outputFormat}. Supported formats: json, text, console");
                    return (int)ExitCode.SystemError;
                }
            }

            // Check for --env and --all conflict
            if (allEnvironments && !string.IsNullOrWhiteSpace(environmentId))
            {
                await System.Console.Error.WriteLineAsync(
                    "Error: Cannot specify both --env and --all options. Use --all to validate all environments or --env to validate a specific environment.");
                return (int)ExitCode.SystemError;
            }

            var exitCode = await HandleValidateCommand(configPath, environmentId, allEnvironments, outputFormat, verbose);
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
        _logger.LogDebug("Starting SGuard CLI with arguments: {Args}", string.Join(" ", args));

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
            _logger.LogError(ex, "💥 Fatal error during CLI execution");
            return ExitCode.SystemError;
        }
    }

    /// <summary>
    /// Handles the validate command execution.
    /// </summary>
    private async Task<ExitCode> HandleValidateCommand(string configPath, string? environmentId, bool allEnvironments, string outputFormat,
                                                       bool verbose)
    {
        if (verbose)
        {
            _logger.LogInformation("Verbose mode enabled");
        }

        // Get the appropriate output formatter
        var formatter = !outputFormat.Equals("console", StringComparison.InvariantCultureIgnoreCase)
                            ? OutputFormatterFactory.Create(outputFormat)
                            : _outputFormatter;

        try
        {
            RuleEngineResult result;

            if (allEnvironments || string.IsNullOrWhiteSpace(environmentId))
            {
                _logger.LogInformation("Validating all environments from {ConfigPath}", configPath);

                result = IsJsonContent(configPath)
                             ? _ruleEngine.ValidateAllEnvironmentsFromJson(configPath)
                             : _ruleEngine.ValidateAllEnvironments(configPath);
            }
            else
            {
                _logger.LogInformation("Validating environment {EnvironmentId} from {ConfigPath}", environmentId, configPath);

                result = IsJsonContent(configPath)
                             ? _ruleEngine.ValidateEnvironmentFromJson(configPath, environmentId)
                             : _ruleEngine.ValidateEnvironment(configPath, environmentId);
            }

            await formatter.FormatAsync(result);

            var exitCode = result is { IsSuccess: true, HasValidationErrors: false } ? ExitCode.Success : ExitCode.ValidationErrors;
            _logger.LogDebug("Validation completed with exit code: {ExitCode}", exitCode);

            return exitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Fatal error during validation");
            return ExitCode.SystemError;
        }
    }

    private static bool IsJsonContent(string pathOrContent)
    {
        return pathOrContent.StartsWith('{') || pathOrContent.Contains("\"version\"");
    }
}