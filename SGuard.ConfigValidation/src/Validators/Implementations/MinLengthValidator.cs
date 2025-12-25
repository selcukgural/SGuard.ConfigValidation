using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators.Base;

namespace SGuard.ConfigValidation.Validators;

public sealed class MinLengthValidator : BaseValidator<object>
{
    public override string ValidatorType => "min_len";
    
    // MinLength validator primarily works with strings
    public override IReadOnlySet<Common.ValueType> SupportedValueTypes { get; } = 
        new HashSet<Common.ValueType> { Common.ValueType.String, Common.ValueType.JsonElement };

    public override ValidationResult Validate(object? value, ValidatorCondition condition)
    {
        // Use TypedValue for type-safe access
        var typedValue = TypedValue.From(value);
        var stringValue = typedValue.AsString() ?? string.Empty;

        // Get condition value in a type-safe manner
        var conditionTypedValue = condition.GetTypedValue();
        int minLength;
        
        if (!conditionTypedValue.TryGetInt32(out minLength))
        {
            // Use JsonElementHelper for backward compatibility (may throw exception)
            minLength = JsonElementHelper.GetInt32(condition.Value, ValidatorType);
        }

        return stringValue.Length < minLength ? CreateFailure(condition.Message, string.Empty, value) : CreateSuccess();
    }
}