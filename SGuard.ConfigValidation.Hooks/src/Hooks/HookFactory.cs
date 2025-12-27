using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Models;

namespace SGuard.ConfigValidation.Hooks;

/// <summary>
/// Factory for creating hook instances from hook configurations.
/// </summary>
public sealed class HookFactory
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the HookFactory class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory instance.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="loggerFactory"/> is null.</exception>
    public HookFactory(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates a hook instance from the hook configuration.
    /// </summary>
    /// <param name="config">The hook configuration.</param>
    /// <returns>An <see cref="IHook"/> instance, or null if the hook type is not supported.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    public IHook? CreateHook(HookConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (string.IsNullOrWhiteSpace(config.Type))
        {
            _loggerFactory.CreateLogger<HookFactory>().LogWarning("Hook configuration has empty type, skipping");
            return null;
        }

        var hookType = config.Type.ToLowerInvariant();

        return hookType switch
        {
            HookType.Web    => CreateWebhookHook(config),
            HookType.Script => CreateScriptHook(config),
            _               => null
        };
    }

    /// <summary>
    /// Creates a script hook instance.
    /// </summary>
    private ScriptHook? CreateScriptHook(HookConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.Command))
        {
            return new ScriptHook(config.Command, config.Arguments ?? [], config.WorkingDirectory,
                                  config.EnvironmentVariables ?? new Dictionary<string, string>(), config.Timeout ?? 30000, // Default 30 seconds
                                  _loggerFactory.CreateLogger<ScriptHook>());
        }

        _loggerFactory.CreateLogger<HookFactory>().LogWarning("Script hook configuration missing 'command' property");
        return null;
    }

    /// <summary>
    /// Creates a webhook hook instance.
    /// </summary>
    private WebhookHook? CreateWebhookHook(HookConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Url))
        {
            _loggerFactory.CreateLogger<HookFactory>().LogWarning("Webhook hook configuration missing 'url' property");
            return null;
        }

        var httpClient = new HttpClient();
        var method = string.IsNullOrWhiteSpace(config.Method) ? "POST" : config.Method.ToUpperInvariant();

        return new WebhookHook(config.Url, method, config.Headers ?? new Dictionary<string, string>(), config.Body,
                               config.Timeout ?? 10000, // Default 10 seconds
                               httpClient, _loggerFactory.CreateLogger<WebhookHook>());
    }

}