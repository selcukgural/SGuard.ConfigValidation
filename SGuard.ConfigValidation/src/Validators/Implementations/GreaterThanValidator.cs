using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators.Base;

namespace SGuard.ConfigValidation.Validators;

public sealed class GreaterThanValidator : BaseValidator<object>
{
    public override string ValidatorType => "gt";
    
    // GreaterThan validator works with numeric values
    public override IReadOnlySet<Common.ValueType> SupportedValueTypes { get; } = 
        new HashSet<Common.ValueType> { Common.ValueType.Number, Common.ValueType.String, Common.ValueType.JsonElement };

    public override ValidationResult Validate(object? value, ValidatorCondition condition)
    {
        if (value == null)
        {
            return CreateSuccess(); // Null values pass by default
        }

        // Use TypedValue for type-safe numeric comparison
        var valueTyped = TypedValue.From(value);
        var conditionTyped = condition.GetTypedValue();

        if (valueTyped.TryGetNumeric(out var valueNumeric) && conditionTyped.TryGetNumeric(out var conditionNumeric))
        {
            // Numeric comparison
            return valueNumeric > conditionNumeric 
                ? CreateSuccess() 
                : CreateFailure(condition.Message, string.Empty, value);
        }

        // Use ValueConversionHelper for backward compatibility
        try
        {
            var comparisonResult = ValueConversionHelper.CompareValues(value, condition.Value);
            if (comparisonResult <= 0)
            {
                return CreateFailure(condition.Message, string.Empty, value);
            }
        }
        catch (ArgumentException)
        {
            return CreateFailure($"Value for '{ValidatorType}' validator must be comparable", string.Empty, value);
        }
        
        return CreateSuccess();
    }
}
