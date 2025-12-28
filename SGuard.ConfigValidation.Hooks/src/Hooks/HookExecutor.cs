using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Common;
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
    private readonly SecurityOptions _securityOptions;
    private readonly ILogger<HookExecutor> _logger;

    /// <summary>
    /// Initializes a new instance of the HookExecutor class.
    /// </summary>
    /// <param name="hookFactory">Factory for creating hook instances.</param>
    /// <param name="securityOptions">Security options for configuring security limits.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="hookFactory"/>, <paramref name="securityOptions"/>, or <paramref name="logger"/> is null.</exception>
    public HookExecutor(HookFactory hookFactory, SecurityOptions securityOptions, ILogger<HookExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(hookFactory);
        ArgumentNullException.ThrowIfNull(securityOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _hookFactory = hookFactory;
        _securityOptions = securityOptions;
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
    public async Task ExecuteHooksAsync(RuleEngineResult result, SGuardConfig config, string? environmentId,
                                        CancellationToken cancellationToken = default)
    {
        if (config.Hooks == null)
        {
            return; // No hooks configured
        }

        var templateResolver = new TemplateVariableResolver(result, environmentId, _securityOptions);
        var context = new HookContext(result, environmentId, templateResolver);

        // Determine which hooks to execute based on a result
        var hooksToExecute = GetHooksToExecute(result, config, environmentId);

        if (hooksToExecute.Count == 0)
        {
            return; // No hooks to execute
        }

        _logger.LogInformation("Executing {HookCount} hook(s) for environment: {EnvironmentId}", hooksToExecute.Count, environmentId ?? "all");

        // Execute all hooks in parallel (non-blocking)
        var tasks = hooksToExecute.Select(hook => ExecuteHookAsync(hook, context, cancellationToken));
        await Task.WhenAll(tasks);
    }
    
    /// <summary>
    /// Determines the list of hooks to execute based on the validation result, configuration, and environment.
    /// </summary>
    /// <param name="result">
    /// The <see cref="RuleEngineResult"/> representing the outcome of the validation process. Must not be <c>null</c>.
    /// </param>
    /// <param name="config">
    /// The <see cref="SGuardConfig"/> containing hook configuration. Must not be <c>null</c>.
    /// </param>
    /// <param name="environmentId">
    /// The environment identifier for which hooks should be selected. If <c>null</c>, global hooks are considered.
    /// </param>
    /// <returns>
    /// A <see cref="List{HookConfig}"/> containing the hooks to execute. Returns an empty list if no hooks are applicable.
    /// </returns>
    /// <remarks>
    /// This method prioritizes environment-specific hooks over global hooks. It selects hooks based on the validation result:
    /// - On success, selects <c>OnSuccess</c> hooks.
    /// - On system error, selects <c>OnSystemError</c> hooks.
    /// - On validation error, selects <c>OnValidationError</c> hooks.
    /// - On general failure, selects <c>OnFailure</c> hooks.
    /// The returned list may be empty if no hooks are configured for the given scenario.
    /// </remarks>
    /// <example>
    /// <code>
    /// var hooks = GetHooksToExecute(result, config, environmentId);
    /// foreach (var hook in hooks)
    /// {
    ///     // Execute each hook as needed
    /// }
    /// </code>
    /// </example>
    private static List<HookConfig> GetHooksToExecute(RuleEngineResult result, SGuardConfig config, string? environmentId)
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
        if (!string.IsNullOrWhiteSpace(environmentId) && config.Hooks.Environments != null &&
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
    /// <param name="hookConfig">
    /// The configuration for the hook to execute. Must not be <c>null</c>.
    /// </param>
    /// <param name="context">
    /// The <see cref="HookContext"/> providing execution context and variables for the hook. Must not be <c>null</c>.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to observe while waiting for the task to complete. Optional.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation. The task completes when the hook execution finishes or fails.
    /// </returns>
    /// <remarks>
    /// This method creates a hook instance using the provided <paramref name="hookConfig"/> and executes it asynchronously.
    /// If the hook cannot be created, a warning is logged and the method returns.
    /// Any exceptions thrown during hook execution are caught and logged as errors; they do not propagate or affect the caller.
    /// Hook failures are intentionally non-blocking and do not impact the main validation flow.
    /// </remarks>
    /// <example>
    /// <code>
    /// await ExecuteHookAsync(hookConfig, context, cancellationToken);
    /// </code>
    /// </example>
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