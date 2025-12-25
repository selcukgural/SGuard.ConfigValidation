namespace SGuard.ConfigValidation;

public sealed class ConfigurationValidator : IConfigurationValidator
{
    private readonly List<ConfigRule> _results = [];
    
    private ConfigurationValidator() { }
    
    public static ConfigurationValidator Create() => new();

    public void AddRule(ConfigRule configRule)
    {
        _results.Add(configRule);
    }

    public void AddRangeRule(IEnumerable<ConfigRule> rules)
    {
        _results.AddRange(rules);
    }

    public IEnumerable<ConfigRuleResult> Validate()
    {
        foreach (var rule in _results)
        {
            yield return rule.Validate();
        }
    }
}

public interface IConfigurationValidator
{
    void AddRule(ConfigRule configRule);
    void AddRangeRule(IEnumerable<ConfigRule> rules);
    IEnumerable<ConfigRuleResult> Validate();
}

public abstract class ConfigRule(object value)
{
    protected readonly object? Value = value;
    public abstract ConfigRuleResult Validate();
}

public sealed record ConfigRuleResult(bool IsValid, string? Message, object? Value, Exception? Exception);