using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators.Base;

namespace SGuard.ConfigValidation.Validators;

public sealed class InValidator : BaseValidator<object>
{
    public override string ValidatorType => "in";
    
    // In validator works with strings and arrays
    public override IReadOnlySet<Common.ValueType> SupportedValueTypes { get; } = 
        new HashSet<Common.ValueType> { Common.ValueType.String, Common.ValueType.Array, Common.ValueType.JsonElement };

    public override ValidationResult Validate(object? value, ValidatorCondition condition)
    {
        // Use TypedValue for type-safe array access
        var conditionTyped = condition.GetTypedValue();
        
        string[] allowedValues;
        
        if (!conditionTyped.TryGetStringArray(out allowedValues))
        {
            // Check JsonElement for backward compatibility
            if (condition.Value is not System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.Array } jsonElement)
            {
                return CreateFailure(
                    $"Validation failed: Value for '{ValidatorType}' validator must be an array. " +
                    $"Actual value type: {condition.Value?.GetType().Name ?? "null"}. " +
                    "Please provide an array of allowed values.",
                    string.Empty, value, condition.Value);
            }

            var allowedValuesList = new List<string>();
            foreach (var item in jsonElement.EnumerateArray())
            {
                allowedValuesList.Add(item.ToString());
            }
            allowedValues = allowedValuesList.ToArray();
        }

        var valueTyped = TypedValue.From(value);
        var valueString = valueTyped.AsString() ?? string.Empty;
        var isInList = allowedValues.Any(v => string.Equals(v, valueString, StringComparison.OrdinalIgnoreCase));
        
        if (!isInList)
        {
            var allowedValuesStr = string.Join(", ", allowedValues.Select(v => $"'{v}'"));
            return CreateFailure(
                condition.Message, 
                string.Empty, 
                value, 
                $"one of: {allowedValuesStr}");
        }
        return CreateSuccess();
    }
}