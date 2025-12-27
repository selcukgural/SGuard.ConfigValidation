using SGuard.ConfigValidation.Results;

namespace SGuard.ConfigValidation.Hooks;

/// <summary>
/// Context information passed to hooks during execution.
/// Contains validation result, environment information, and template variable resolver.
/// </summary>
public sealed class HookContext
{
    /// <summary>
    /// Gets the validation result from the rule engine.
    /// </summary>
    public RuleEngineResult Result { get; }

    /// <summary>
    /// Gets the environment ID that was validated, or null if all environments were validated.
    /// </summary>
    public string? EnvironmentId { get; }

    /// <summary>
    /// Gets the template variable resolver for resolving dynamic values in hook configurations.
    /// </summary>
    public ITemplateVariableResolver TemplateResolver { get; }

    /// <summary>
    /// Initializes a new instance of the HookContext class.
    /// </summary>
    /// <param name="result">The validation result from the rule engine.</param>
    /// <param name="environmentId">The environment ID that was validated, or null if all environments were validated.</param>
    /// <param name="templateResolver">The template variable resolver for resolving dynamic values.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="result"/> or <paramref name="templateResolver"/> is null.</exception>
    public HookContext(RuleEngineResult result, string? environmentId, ITemplateVariableResolver templateResolver)
    {
        System.ArgumentNullException.ThrowIfNull(result);
        System.ArgumentNullException.ThrowIfNull(templateResolver);

        Result = result;
        EnvironmentId = environmentId;
        TemplateResolver = templateResolver;
    }
}

