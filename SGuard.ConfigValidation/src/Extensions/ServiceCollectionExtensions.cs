using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Output;
using SGuard.ConfigValidation.Security;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Services.Abstract;
using SGuard.ConfigValidation.Validators;
using SGuard.ConfigValidation.Validators.Plugin;
using FileValidator = SGuard.ConfigValidation.Services.FileValidator;

namespace SGuard.ConfigValidation.Extensions;

/// <summary>
/// Extension methods for registering SGuard.ConfigValidation services in dependency injection containers.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <param name="services">The service collection to add services to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds all SGuard.ConfigValidation services to the service collection.
        /// This is a convenience method that registers all core services with default implementations.
        /// </summary>
        /// <param name="configuration">Optional configuration instance. If provided, SecurityOptions will be bound from the "Security" section.</param>
        /// <param name="configureSecurityOptions">Optional action to configure SecurityOptions. If provided, this takes precedence over configuration binding.</param>
        /// <param name="pluginDirectories">Optional list of directories to scan for validator plugins.</param>
        /// <param name="logLevel">Optional logging level. If specified, logging will be configured with this minimum level for SGuard namespaces. If logging is already registered, this will override the existing configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This method registers:
        /// - SecurityOptions (from configuration or defaults)
        /// - IValidatorFactory
        /// - IConfigValidator
        /// - IConfigLoader
        /// - IFileValidator
        /// - IPathResolver
        /// - IRuleEngine
        /// - IYamlLoader (optional)
        /// - ISchemaValidator (optional)
        /// - Output formatters (ConsoleOutputFormatter, JsonOutputFormatter)
        /// 
        /// If <paramref name="logLevel"/> is specified, logging will be automatically configured. Otherwise, logging must be registered separately using services.AddLogging() or your preferred logging setup.
        /// </remarks>
        /// <example>
        /// <code>
        /// using Microsoft.Extensions.Configuration;
        /// using Microsoft.Extensions.DependencyInjection;
        /// using SGuard.ConfigValidation.Extensions;
        /// 
        /// var services = new ServiceCollection();
        /// 
        /// // Register logging first
        /// services.AddLogging(builder => builder.AddConsole());
        /// 
        /// var configuration = new ConfigurationBuilder()
        ///     .AddJsonFile("appsettings.json")
        ///     .Build();
        /// 
        /// // Register all services
        /// .AddSGuardConfigValidation(configuration);
        /// 
        /// var serviceProvider = services.BuildServiceProvider();
        /// var ruleEngine = serviceProvider.GetRequiredService&lt;IRuleEngine&gt;();
        /// </code>
        /// </example>
        public IServiceCollection AddSGuardConfigValidation(IConfiguration? configuration = null,
                                                            Action<SecurityOptions>? configureSecurityOptions = null,
                                                            IEnumerable<string>? pluginDirectories = null,
                                                            LogLevel? logLevel = null)
        {
            // Register logging if logLevel is specified
            if (logLevel.HasValue)
            {
                var loggingAlreadyRegistered = services.Any(sd => sd.ServiceType == typeof(ILoggerFactory));
            
                if (!loggingAlreadyRegistered)
                {
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(logLevel.Value);
                        builder.AddFilter("SGuard", logLevel.Value);
                    });
                }
                else
                {
                    // Logging already registered, configure the level
                    // Note: This will add additional logging configuration
                    // The developer should ensure their logging provider supports this
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(logLevel.Value);
                        builder.AddFilter("SGuard", logLevel.Value);
                    });
                }
            }

            // Register SecurityOptions
            if (configureSecurityOptions != null)
            {
                services.Configure(configureSecurityOptions);
            }
            else if (configuration != null)
            {
                services.Configure<SecurityOptions>(configuration.GetSection("Security"));
            }
            else
            {
                // Use default SecurityOptions
                services.AddSingleton(Options.Create(new SecurityOptions()));
            }

            // Register plugin discovery if plugin directories are provided
            if (pluginDirectories != null)
            {
                services.AddSingleton<ValidatorPluginDiscovery>(sp =>
                {
                    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger<ValidatorPluginDiscovery>();
                    return new ValidatorPluginDiscovery(logger);
                });
            }

            // Register core services
            services.AddSingleton<IValidatorFactory>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ValidatorFactory>>();
                var discovery = sp.GetService<ValidatorPluginDiscovery>();
                return new ValidatorFactory(logger, discovery, pluginDirectories);
            });

            services.AddSingleton<IConfigValidator, ConfigValidator>();
            services.AddSingleton<IConfigLoader, ConfigLoader>();
            services.AddSingleton<IFileValidator, FileValidator>();
            services.AddSingleton<IPathResolver, PathResolver>();
            services.AddSingleton<IRuleEngine, RuleEngine>();

            // Register optional services
            services.AddSingleton<IYamlLoader, YamlLoader>();
            services.AddSingleton<ISchemaValidator, JsonSchemaValidator>();

            // Register output formatters
            services.AddSingleton<IOutputFormatter, ConsoleOutputFormatter>();
            services.AddSingleton<IOutputFormatter, JsonOutputFormatter>();

            return services;
        }

        /// <summary>
        /// Adds SGuard.ConfigValidation core services only (without optional services like YAML loader and schema validator).
        /// Use this method when you want more control over which services are registered.
        /// </summary>
        /// <param name="configuration">Optional configuration instance. If provided, SecurityOptions will be bound from the "Security" section.</param>
        /// <param name="configureSecurityOptions">Optional action to configure SecurityOptions. If provided, this takes precedence over configuration binding.</param>
        /// <param name="pluginDirectories">Optional list of directories to scan for validator plugins.</param>
        /// <param name="logLevel">Optional logging level. If specified, logging will be configured with this minimum level for SGuard namespaces. If logging is already registered, this will override the existing configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// If <paramref name="logLevel"/> is specified, logging will be automatically configured. Otherwise, logging must be registered separately using services.AddLogging() or your preferred logging setup.
        /// </remarks>
        /// <example>
        /// <code>
        /// var services = new ServiceCollection();
        /// 
        /// // Register logging-first
        /// services.AddLogging();
        /// 
        /// // Register only core services
        /// .AddSGuardConfigValidationCore();
        /// 
        /// // Optionally add YAML support
        /// services.AddSingleton&lt;IYamlLoader, YamlLoader&gt;();
        /// </code>
        /// </example>
        public IServiceCollection AddSGuardConfigValidationCore(IConfiguration? configuration = null,
                                                                Action<SecurityOptions>? configureSecurityOptions = null,
                                                                IEnumerable<string>? pluginDirectories = null,
                                                                LogLevel? logLevel = null)
        {
            // Register logging if logLevel is specified
            if (logLevel.HasValue)
            {
                var loggingAlreadyRegistered = services.Any(sd => sd.ServiceType == typeof(ILoggerFactory));
            
                if (!loggingAlreadyRegistered)
                {
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(logLevel.Value);
                        builder.AddFilter("SGuard", logLevel.Value);
                    });
                }
                else
                {
                    // Logging already registered, configure the level
                    // Note: This will add additional logging configuration
                    // The developer should ensure their logging provider supports this
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(logLevel.Value);
                        builder.AddFilter("SGuard", logLevel.Value);
                    });
                }
            }

            // Register SecurityOptions
            if (configureSecurityOptions != null)
            {
                services.Configure(configureSecurityOptions);
            }
            else if (configuration != null)
            {
                services.Configure<SecurityOptions>(configuration.GetSection("Security"));
            }
            else
            {
                // Use default SecurityOptions
                services.AddSingleton(Options.Create(new SecurityOptions()));
            }

            // Register plugin discovery if plugin directories are provided
            if (pluginDirectories != null)
            {
                services.AddSingleton<ValidatorPluginDiscovery>(sp =>
                {
                    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger<ValidatorPluginDiscovery>();
                    return new ValidatorPluginDiscovery(logger);
                });
            }

            // Register core services only
            services.AddSingleton<IValidatorFactory>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ValidatorFactory>>();
                var discovery = sp.GetService<ValidatorPluginDiscovery>();
                return new ValidatorFactory(logger, discovery, pluginDirectories);
            });

            services.AddSingleton<IConfigValidator, ConfigValidator>();
            services.AddSingleton<IConfigLoader, ConfigLoader>();
            services.AddSingleton<IFileValidator, FileValidator>();
            services.AddSingleton<IPathResolver, PathResolver>();
            services.AddSingleton<IRuleEngine, RuleEngine>();

            return services;
        }
    }
}
