using System.Collections.Generic;

namespace SGuard.ConfigValidation.Models;

/// <summary>
/// Configuration for post-validation hooks.
/// Supports both global hooks and environment-specific hooks.
/// </summary>
public sealed class HooksConfig
{
    /// <summary>
    /// Gets or sets the global hooks that run for all environments.
    /// </summary>
    public GlobalHooksConfig? Global { get; set; }

    /// <summary>
    /// Gets or sets the environment-specific hooks.
    /// Key is the environment ID, value is the hooks configuration for that environment.
    /// </summary>
    public Dictionary<string, EnvironmentHooksConfig>? Environments { get; set; }
}

/// <summary>
/// Configuration for global hooks that run for all environments.
/// </summary>
public sealed class GlobalHooksConfig
{
    /// <summary>
    /// Gets or sets the hooks to execute when validation succeeds (no errors).
    /// </summary>
    public List<HookConfig>? OnSuccess { get; set; }

    /// <summary>
    /// Gets or sets the hooks to execute when validation fails (has errors or system error).
    /// </summary>
    public List<HookConfig>? OnFailure { get; set; }

    /// <summary>
    /// Gets or sets the hooks to execute when validation errors are found (exit code 1).
    /// </summary>
    public List<HookConfig>? OnValidationError { get; set; }

    /// <summary>
    /// Gets or sets the hooks to execute when a system error occurs (exit code 2).
    /// </summary>
    public List<HookConfig>? OnSystemError { get; set; }
}

/// <summary>
/// Configuration for environment-specific hooks.
/// </summary>
public sealed class EnvironmentHooksConfig
{
    /// <summary>
    /// Gets or sets the hooks to execute when validation succeeds for this environment.
    /// </summary>
    public List<HookConfig>? OnSuccess { get; set; }

    /// <summary>
    /// Gets or sets the hooks to execute when validation fails for this environment.
    /// </summary>
    public List<HookConfig>? OnFailure { get; set; }
}

