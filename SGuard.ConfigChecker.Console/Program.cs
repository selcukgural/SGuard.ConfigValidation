using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Security;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Services.Abstract;
using SGuard.ConfigValidation.Validators;
using SGuard.ConfigValidation.Validators.Plugin;
using FileValidator = SGuard.ConfigValidation.Services.FileValidator;

namespace SGuard.ConfigChecker.Console;

internal class Program
{ 
    private const string Logging = "Logging";
    
    private static async Task<int> Main(string[] args)
    {
        try
        {
            // Check for verbose flag before setting up services
            var isVerbose = args.Contains("--verbose") || args.Contains("-v");
            
            // Setup dependency injection
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, isVerbose);
            
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            
            try
            {
                var cli = serviceProvider.GetRequiredService<SGuardCli>();
                var exitCode = await cli.RunAsync(args);
                return (int)exitCode;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fatal error during application execution");
                Environment.Exit((int)ExitCode.SystemError);
                return (int)ExitCode.SystemError; // This line won't be reached, but satisfies the compiler
            }
        }
        catch (Exception ex)
        {
            // Fallback error handling if DI setup fails
            System.Console.WriteLine($"💥 Fatal error during startup: {ex.Message}");
            Environment.Exit((int)ExitCode.SystemError);
            return (int)ExitCode.SystemError; // This line won't be reached, but satisfies the compiler
        }
    }

    private static void ConfigureServices(IServiceCollection services, bool isVerbose = false)
    {
        // Get environment name - .NET standard: DOTNET_ENVIRONMENT (preferred) or ASPNETCORE_ENVIRONMENT (fallback)
        var environmentName = GetEnvironmentName();
        
        // Build configuration from appsettings.json and environment-specific files
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Register IHostEnvironment for services that might need it
        var contentRootPath = Directory.GetCurrentDirectory();
        var hostEnvironment = new HostEnvironment
        {
            EnvironmentName = environmentName,
            ApplicationName = "SGuard.ConfigChecker",
            ContentRootPath = contentRootPath,
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath)
        };
        
        services.AddSingleton<IHostEnvironment>(hostEnvironment);
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection(Logging));
            builder.AddConsole();
            
            // Override logging level to Debug if verbose mode is enabled
            if (isVerbose)
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddFilter("SGuard", LogLevel.Debug);
            }
        });

        // Register SecurityOptions from configuration using the IOptions pattern
        services.AddOptions<SecurityOptions>()
            .Bind(configuration.GetSection("Security"))
            .Validate(options =>
            {
                // Validate and clamp security options to ensure they don't exceed hard limits
                // Create a temporary logger factory for validation (before full DI is built)
                using var tempLoggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConfiguration(configuration.GetSection(Logging));
                    builder.AddConsole();
                    
                    // Override logging level to Debug if verbose mode is enabled
                    if (isVerbose)
                    {
                        builder.SetMinimumLevel(LogLevel.Debug);
                        builder.AddFilter("SGuard", LogLevel.Debug);
                    }
                });
                var securityLogger = tempLoggerFactory.CreateLogger<SecurityOptions>();
                options.ValidateAndClamp(securityLogger);
                return true; // Always return true - validation/clamping is done, but we don't fail on it
            })
            .ValidateOnStart(); // Ensure validation runs at startup

        // Register services - .NET DI container will automatically resolve constructor dependencies
        services.AddSingleton<ISchemaValidator, JsonSchemaValidator>();
        services.AddSingleton<IYamlLoader, YamlLoader>();
        services.AddSingleton<ValidatorPluginDiscovery>();
        services.AddSingleton<IValidatorFactory, ValidatorFactory>();
        services.AddSingleton<IConfigValidator, ConfigValidator>();
        services.AddSingleton<IConfigLoader, ConfigLoader>();
        services.AddSingleton<IFileValidator, FileValidator>();
        services.AddSingleton<IPathResolver, PathResolver>();
        services.AddSingleton<IRuleEngine, RuleEngine>();
        
        // Register output formatters
        services.AddSingleton<ConfigValidation.Output.IOutputFormatter, ConfigValidation.Output.ConsoleOutputFormatter>();
        services.AddSingleton<ConfigValidation.Output.IOutputFormatter, ConfigValidation.Output.JsonOutputFormatter>();
        
        // Register CLI
        services.AddSingleton<SGuardCli>();
    }

    /// <summary>
    /// Gets the environment name using .NET standard environment variables.
    /// Checks DOTNET_ENVIRONMENT first (preferred), then ASPNETCORE_ENVIRONMENT (fallback).
    /// Defaults to "Production" if neither is set.
    /// </summary>
    /// <returns>The environment name (Development, Staging, Production, etc.)</returns>
    private static string GetEnvironmentName()
    {
        // .NET 6+ standard: DOTNET_ENVIRONMENT is preferred
        var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        
        // Fallback to ASPNETCORE_ENVIRONMENT for backward compatibility
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        }
        
        // Default to Production if not set
        return string.IsNullOrWhiteSpace(environmentName) 
            ? "Production" 
            : environmentName;
    }

    /// <summary>
    /// Simple implementation of IHostEnvironment for console applications.
    /// </summary>
    private sealed class HostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        
        public bool IsDevelopment() => string.Equals(EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase);
        public bool IsStaging() => string.Equals(EnvironmentName, "Staging", StringComparison.OrdinalIgnoreCase);
        public bool IsProduction() => string.Equals(EnvironmentName, "Production", StringComparison.OrdinalIgnoreCase);
        public bool IsEnvironment(string environmentName) => string.Equals(EnvironmentName, environmentName, StringComparison.OrdinalIgnoreCase);
    }
}
