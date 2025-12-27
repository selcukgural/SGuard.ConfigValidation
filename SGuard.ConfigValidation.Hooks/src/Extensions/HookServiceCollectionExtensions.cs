using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

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
        services.TryAddSingleton<HookFactory>(serviceProvider =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            return new HookFactory(loggerFactory);
        });

        // Register HookExecutor
        services.TryAddSingleton<HookExecutor>(serviceProvider =>
        {
            var hookFactory = serviceProvider.GetRequiredService<HookFactory>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<HookExecutor>();
            return new HookExecutor(hookFactory, logger);
        });

        return services;
    }
}

