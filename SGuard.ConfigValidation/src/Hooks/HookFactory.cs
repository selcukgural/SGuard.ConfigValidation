using System.Net.Http;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Hooks.Implementations;
using static SGuard.ConfigValidation.Common.Throw;

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
        System.ArgumentNullException.ThrowIfNull(loggerFactory);
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
        System.ArgumentNullException.ThrowIfNull(config);

        if (string.IsNullOrWhiteSpace(config.Type))
        {
            _loggerFactory.CreateLogger<HookFactory>().LogWarning("Hook configuration has empty type, skipping");
            return null;
        }

        var hookType = config.Type.ToLowerInvariant();

        return hookType switch
        {
            "script" => CreateScriptHook(config),
            "webhook" => CreateWebhookHook(config),
            _ => null
        };
    }

    /// <summary>
    /// Creates a script hook instance.
    /// </summary>
    private IHook? CreateScriptHook(HookConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Command))
        {
            _loggerFactory.CreateLogger<HookFactory>().LogWarning("Script hook configuration missing 'command' property");
            return null;
        }

        return new ScriptHook(
            config.Command,
            config.Arguments ?? new List<string>(),
            config.WorkingDirectory,
            config.EnvironmentVariables ?? new Dictionary<string, string>(),
            config.Timeout ?? 30000, // Default 30 seconds
            _loggerFactory.CreateLogger<ScriptHook>());
    }

    /// <summary>
    /// Creates a webhook hook instance.
    /// </summary>
    private IHook? CreateWebhookHook(HookConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Url))
        {
            _loggerFactory.CreateLogger<HookFactory>().LogWarning("Webhook hook configuration missing 'url' property");
            return null;
        }

        var httpClient = new System.Net.Http.HttpClient();
        var method = string.IsNullOrWhiteSpace(config.Method) ? "POST" : config.Method.ToUpperInvariant();

        return new WebhookHook(
            config.Url,
            method,
            config.Headers ?? new Dictionary<string, string>(),
            config.Body,
            config.Timeout ?? 10000, // Default 10 seconds
            httpClient,
            _loggerFactory.CreateLogger<WebhookHook>());
    }

}

