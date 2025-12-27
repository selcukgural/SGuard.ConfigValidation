using System.Text.RegularExpressions;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Results;
using System.Text.Json;

namespace SGuard.ConfigValidation.Hooks;

/// <summary>
/// Resolves template variables in hook configurations.
/// Supports variables like {{status}}, {{environment}}, {{errorCount}}, etc.
/// </summary>
public sealed partial class TemplateVariableResolver : ITemplateVariableResolver
{
    private readonly RuleEngineResult _result;
    private readonly string? _environmentId;
    private readonly Dictionary<string, string> _variables;

    /// <summary>
    /// Initializes a new instance of the TemplateVariableResolver class.
    /// </summary>
    /// <param name="result">The validation result from the rule engine.</param>
    /// <param name="environmentId">The environment ID that was validated or null if all environments were validated.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    public TemplateVariableResolver(RuleEngineResult result, string? environmentId)
    {
        ArgumentNullException.ThrowIfNull(result);

        _result = result;
        _environmentId = environmentId;
        _variables = BuildVariables();
    }

    /// <summary>
    /// Resolves template variables in the given template string.
    /// </summary>
    /// <param name="template">The template string containing variables like {{status}}, {{environment}}, etc.</param>
    /// <returns>The resolved string with all template variables replaced with their actual values.</returns>
    public string Resolve(string? template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template ?? string.Empty;
        }

        // Match {{variable}} pattern
        return MatchVariablePatternRegex().Replace(template, match =>
        {
            var variableName = match.Groups[1].Value;
            var value = GetVariable(variableName);
            return value ?? match.Value; // Return original if variable not found
        });
    }

    /// <summary>
    /// Gets the value of a specific template variable.
    /// </summary>
    /// <param name="variableName">The variable name without braces (e.g., "status" for {{status}}).</param>
    /// <returns>The resolved value of the variable, or null if the variable is not found.</returns>
    public string? GetVariable(string variableName)
    {
        return _variables.GetValueOrDefault(variableName);
    }

    /// <summary>
    /// Builds the dictionary of available template variables.
    /// </summary>
    private Dictionary<string, string> BuildVariables()
    {
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Status: "success" or "failure"
        var status = _result is { IsSuccess: true, HasValidationErrors: false } ? "success" : "failure";
        variables["status"] = status;

        // Status color: "good" (green) or "danger" (red)
        variables["statusColor"] = status == "success" ? "good" : "danger";

        // Environment ID
        variables["environment"] = _environmentId ?? "all";

        // Error count
        var errorCount = _result.ValidationResults.Sum(r => r.ErrorCount) + (_result.SingleResult?.ErrorCount ?? 0);

        variables["errorCount"] = errorCount.ToString();

        // Errors as JSON array
        var errors = new List<object>();

        if (_result.SingleResult != null)
        {
            foreach (var error in _result.SingleResult.Errors)
            {
                errors.Add(new
                {
                    error.Message,
                    error.ValidatorType,
                    error.Key,
                    Value = error.Value?.ToString()
                });
            }
        }
        else
        {
            foreach (var fileResult in _result.ValidationResults)
            {
                foreach (var error in fileResult.Errors)
                {
                    errors.Add(new
                    {
                        error.Message,
                        error.ValidatorType,
                        error.Key,
                        Value = error.Value?.ToString(),
                        File = fileResult.Path
                    });
                }
            }
        }

        variables["errors"] = JsonSerializer.Serialize(errors, JsonOptions.Output);

        // Results as JSON
        var results = new List<object>();

        if (_result.SingleResult != null)
        {
            results.Add(FormatFileResult(_result.SingleResult));
        }
        else
        {
            foreach (var fileResult in _result.ValidationResults)
            {
                results.Add(FormatFileResult(fileResult));
            }
        }

        variables["results"] = JsonSerializer.Serialize(results, JsonOptions.Output);

        return variables;
    }

    /// <summary>
    /// Formats a file validation result into an anonymous object for JSON serialization.
    /// </summary>
    private static object FormatFileResult(FileValidationResult fileResult)
    {
        return new
        {
            fileResult.Path,
            fileResult.IsValid,
            fileResult.ErrorCount,
            Results = fileResult.Results.Select(r => new
            {
                r.IsValid,
                r.Message,
                r.ValidatorType,
                r.Key,
                Value = r.Value?.ToString()
            }),
            Errors = fileResult.Errors.Select(e => new
            {
                e.Message,
                e.ValidatorType,
                e.Key,
                Value = e.Value?.ToString()
            })
        };
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex MatchVariablePatternRegex();
}