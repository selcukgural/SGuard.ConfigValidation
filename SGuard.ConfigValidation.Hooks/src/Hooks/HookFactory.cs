using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Models;

namespace SGuard.ConfigValidation.Hooks;

/// <summary>
/// Factory for creating hook instances from hook configurations.
/// </summary>
public sealed class HookFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly SecurityOptions _securityOptions;

    /// <summary>
    /// Initializes a new instance of the HookFactory class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory instance.</param>
    /// <param name="securityOptions">Security options for configuring security limits.</param>
    /// <param name="httpClientFactory">Optional HTTP client factory for creating HttpClient instances. If not provided, a new HttpClient will be created for each webhook hook (not recommended for production).</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="loggerFactory"/> or <paramref name="securityOptions"/> is null.</exception>
    public HookFactory(ILoggerFactory loggerFactory, SecurityOptions securityOptions, IHttpClientFactory? httpClientFactory = null)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(securityOptions);

        _loggerFactory = loggerFactory;
        _securityOptions = securityOptions;
        _httpClientFactory = httpClientFactory;
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
                                  _securityOptions, _loggerFactory.CreateLogger<ScriptHook>());
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

        // Use IHttpClientFactory if available, otherwise create a new HttpClient (fallback for backward compatibility)
        HttpClient httpClient;
        if (_httpClientFactory != null)
        {
            httpClient = _httpClientFactory.CreateClient();
        }
        else
        {
            _loggerFactory.CreateLogger<HookFactory>().LogWarning(
                "IHttpClientFactory is not available. Creating a new HttpClient for webhook hook. " +
                "This may lead to socket exhaustion in production. Consider registering IHttpClientFactory in DI container.");
            httpClient = new HttpClient();
        }

        var method = string.IsNullOrWhiteSpace(config.Method) ? "POST" : config.Method.ToUpperInvariant();
        var timeout = config.Timeout ?? 10000; // Default 10 seconds

        // Set timeout on HttpClient
        httpClient.Timeout = TimeSpan.FromMilliseconds(timeout);

        return new WebhookHook(config.Url, method, config.Headers ?? new Dictionary<string, string>(), config.Body,
                               timeout,
                               httpClient, _loggerFactory.CreateLogger<WebhookHook>());
    }

}