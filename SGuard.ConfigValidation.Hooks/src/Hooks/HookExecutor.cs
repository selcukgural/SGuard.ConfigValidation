using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Results;

namespace SGuard.ConfigValidation.Hooks;

/// <summary>
/// Executes post-validation hooks based on validation results.
/// Hooks are executed asynchronously and non-blocking - failures do not affect validation results.
/// </summary>
public sealed class HookExecutor
{
    private readonly HookFactory _hookFactory;
    private readonly ILogger<HookExecutor> _logger;

    /// <summary>
    /// Initializes a new instance of the HookExecutor class.
    /// </summary>
    /// <param name="hookFactory">Factory for creating hook instances.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="hookFactory"/> or <paramref name="logger"/> is null.</exception>
    public HookExecutor(HookFactory hookFactory, ILogger<HookExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(hookFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _hookFactory = hookFactory;
        _logger = logger;
    }

    /// <summary>
    /// Executes hooks based on the validation result and configuration.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <param name="config">The SGuard configuration containing hooks.</param>
    /// <param name="environmentId">The environment ID that was validated, or null if all environments were validated.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteHooksAsync(
        RuleEngineResult result,
        SGuardConfig config,
        string? environmentId,
        CancellationToken cancellationToken = default)
    {
        if (config.Hooks == null)
        {
            return; // No hooks configured
        }

        var templateResolver = new TemplateVariableResolver(result, environmentId);
        var context = new HookContext(result, environmentId, templateResolver);

        // Determine which hooks to execute based on a result
        var hooksToExecute = GetHooksToExecute(result, config, environmentId);

        if (hooksToExecute.Count == 0)
        {
            return; // No hooks to execute
        }

        _logger.LogInformation("Executing {HookCount} hook(s) for environment: {EnvironmentId}", 
            hooksToExecute.Count, environmentId ?? "all");

        // Execute all hooks in parallel (non-blocking)
        var tasks = hooksToExecute.Select(hook => ExecuteHookAsync(hook, context, cancellationToken));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Gets the list of hooks to execute based on the validation result.
    /// </summary>
    private List<HookConfig> GetHooksToExecute(RuleEngineResult result, SGuardConfig config, string? environmentId)
    {
        var hooks = new List<HookConfig>();

        if (config.Hooks == null)
        {
            return hooks;
        }

        // Determine hook trigger based on result
        var isSuccess = result is { IsSuccess: true, HasValidationErrors: false };
        var isSystemError = !result.IsSuccess;

        // Get environment-specific hooks first (higher priority)
        if (!string.IsNullOrWhiteSpace(environmentId) && 
            config.Hooks.Environments != null &&
            config.Hooks.Environments.TryGetValue(environmentId, out var envHooks))
        {
            switch (isSuccess)
            {
                case true when envHooks.OnSuccess != null:
                    hooks.AddRange(envHooks.OnSuccess);
                    break;
                case false when envHooks.OnFailure != null:
                    hooks.AddRange(envHooks.OnFailure);
                    break;
            }
        }

        // Add global hooks
        var globalHooks = config.Hooks.Global;

        if (globalHooks == null)
        {
            return hooks;
        }

        switch (isSuccess)
        {
            case true when globalHooks.OnSuccess != null:
                hooks.AddRange(globalHooks.OnSuccess);
                break;
            case false when isSystemError && globalHooks.OnSystemError != null:
                hooks.AddRange(globalHooks.OnSystemError);
                break;
            case false when result.HasValidationErrors && globalHooks.OnValidationError != null:
                hooks.AddRange(globalHooks.OnValidationError);
                break;
            case false:
            {
                if (globalHooks.OnFailure != null)
                {
                    hooks.AddRange(globalHooks.OnFailure);
                }

                break;
            }
        }

        return hooks;
    }

    /// <summary>
    /// Executes a single hook asynchronously, catching and logging any errors.
    /// </summary>
    private async Task ExecuteHookAsync(HookConfig hookConfig, HookContext context, CancellationToken cancellationToken)
    {
        try
        {
            var hook = _hookFactory.CreateHook(hookConfig);
            if (hook == null)
            {
                _logger.LogWarning("Failed to create hook instance for type: {HookType}", hookConfig.Type);
                return;
            }

            await hook.ExecuteAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing hook: {HookType}", hookConfig.Type);
            // Don't throw - hook failures should not affect validation
        }
    }
}

