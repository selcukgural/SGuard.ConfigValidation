using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Common;

namespace SGuard.ConfigValidation.Hooks.Implementations;

/// <summary>
/// Hook implementation for sending HTTP webhook requests.
/// </summary>
public sealed class WebhookHook : IHook
{
    private readonly string _url;
    private readonly string _method;
    private readonly Dictionary<string, string> _headers;
    private readonly object? _body;
    private readonly int _timeout;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookHook> _logger;

    /// <summary>
    /// Initializes a new instance of the WebhookHook class.
    /// </summary>
    /// <param name="url">The webhook URL.</param>
    /// <param name="method">The HTTP method (default: "POST").</param>
    /// <param name="headers">HTTP headers to include in the request.</param>
    /// <param name="body">The request body (can be a JSON object or template string).</param>
    /// <param name="timeout">Timeout in milliseconds.</param>
    /// <param name="httpClient">HTTP client for making requests.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="url"/>, <paramref name="httpClient"/>, or <paramref name="logger"/> is null.</exception>
    public WebhookHook(
        string url,
        string method,
        Dictionary<string, string> headers,
        object? body,
        int timeout,
        HttpClient httpClient,
        ILogger<WebhookHook> logger)
    {
        System.ArgumentNullException.ThrowIfNull(url);
        System.ArgumentNullException.ThrowIfNull(httpClient);
        System.ArgumentNullException.ThrowIfNull(logger);

        _url = url;
        _method = method;
        _headers = headers ?? new Dictionary<string, string>();
        _body = body;
        _timeout = timeout;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromMilliseconds(timeout);
        _logger = logger;
    }

    /// <summary>
    /// Executes the webhook hook asynchronously.
    /// </summary>
    public async Task ExecuteAsync(HookContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Resolve template variables in URL
            var resolvedUrl = context.TemplateResolver.Resolve(_url);

            _logger.LogInformation("Executing webhook hook: {Method} {Url}", _method, resolvedUrl);

            // Resolve headers
            var resolvedHeaders = new Dictionary<string, string>();
            foreach (var header in _headers)
            {
                resolvedHeaders[header.Key] = context.TemplateResolver.Resolve(header.Value);
            }

            // Resolve body
            object? resolvedBody = null;
            if (_body != null)
            {
                if (_body is string bodyString)
                {
                    // If body is a string, resolve template variables
                    resolvedBody = context.TemplateResolver.Resolve(bodyString);
                }
                else
                {
                    // If body is an object, serialize to JSON and resolve template variables
                    var jsonBody = JsonSerializer.Serialize(_body, JsonOptions.Internal);
                    var resolvedJsonBody = context.TemplateResolver.Resolve(jsonBody);
                    resolvedBody = JsonSerializer.Deserialize<object>(resolvedJsonBody, JsonOptions.Deserialization);
                }
            }

            using var request = new HttpRequestMessage(new HttpMethod(_method), resolvedUrl);

            // Add headers
            foreach (var header in resolvedHeaders)
            {
                if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    // Content-Type will be set when we set the content
                    continue;
                }

                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Set body if provided
            if (resolvedBody != null)
            {
                var jsonContent = JsonSerializer.Serialize(resolvedBody, JsonOptions.Internal);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook hook executed successfully: {Url}. Status: {StatusCode}", 
                    resolvedUrl, response.StatusCode);
            }
            else
            {
                _logger.LogWarning("Webhook hook execution failed: {Url}. Status: {StatusCode}, Response: {Response}", 
                    resolvedUrl, response.StatusCode, responseContent);
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("Webhook hook execution timed out after {Timeout}ms: {Url}", 
                _timeout, _url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing webhook hook: {Url}", _url);
            // Don't throw - hook failures should not affect validation
        }
    }
}

