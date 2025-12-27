namespace SGuard.ConfigValidation.Hooks;

/// <summary>
/// Interface for resolving template variables in hook configurations.
/// Template variables are placeholders like {{status}}, {{environment}}, etc. that are replaced with actual values.
/// </summary>
public interface ITemplateVariableResolver
{
    /// <summary>
    /// Resolves template variables in the given template string.
    /// </summary>
    /// <param name="template">The template string containing variables like {{status}}, {{environment}}, etc.</param>
    /// <returns>The resolved string with all template variables replaced with their actual values.</returns>
    string Resolve(string template);

    /// <summary>
    /// Gets the value of a specific template variable.
    /// </summary>
    /// <param name="variableName">The variable name without braces (e.g., "status" for {{status}}).</param>
    /// <returns>The resolved value of the variable, or null if the variable is not found.</returns>
    string? GetVariable(string variableName);
}

