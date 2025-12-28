using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;

namespace SGuard.ConfigValidation.Hooks;

/// <summary>
/// Extension methods for registering SGuard hooks services in a dependency injection container.
/// </summary>
public static class HookServiceCollectionExtensions
{
    /// <summary>
    /// Adds SGuard hooks services to the service collection.
    /// Registers HookFactory and HookExecutor for post-validation hook execution.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <example>
    /// <code>
    /// using Microsoft.Extensions.DependencyInjection;
    /// using SGuard.ConfigValidation.Hooks;
    /// 
    /// var services = new ServiceCollection();
    /// services.AddLogging();
    /// services.AddSGuardHooks();
    /// 
    /// var serviceProvider = services.BuildServiceProvider();
    /// var hookExecutor = serviceProvider.GetRequiredService&lt;HookExecutor&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddSGuardHooks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register HookFactory
        // Note: IHttpClientFactory should be registered via services.AddHttpClient() before calling AddSGuardHooks()
        // If IHttpClientFactory is not registered, HookFactory will fall back to creating new HttpClient instances
        // Note: SecurityOptions should be registered via services.Configure<SecurityOptions>() before calling AddSGuardHooks()
        services.TryAddSingleton<HookFactory>(serviceProvider =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var securityOptions = serviceProvider.GetRequiredService<IOptions<SecurityOptions>>();
            var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            return new HookFactory(loggerFactory, securityOptions.Value, httpClientFactory);
        });

        // Register HookExecutor
        // Note: SecurityOptions should be registered via services.Configure<SecurityOptions>() before calling AddSGuardHooks()
        services.TryAddSingleton<HookExecutor>(serviceProvider =>
        {
            var hookFactory = serviceProvider.GetRequiredService<HookFactory>();
            var securityOptions = serviceProvider.GetRequiredService<IOptions<SecurityOptions>>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<HookExecutor>();
            return new HookExecutor(hookFactory, securityOptions.Value, logger);
        });

        return services;
    }
}

