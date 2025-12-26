using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators.Base;

namespace SGuard.ConfigValidation.Validators;

public sealed class MaxLengthValidator : BaseValidator<object>
{
    public override string ValidatorType => "max_len";
    
    // MaxLength validator primarily works with strings
    public override IReadOnlySet<Common.ValueType> SupportedValueTypes { get; } = 
        new HashSet<Common.ValueType> { Common.ValueType.String, Common.ValueType.JsonElement };

    public override ValidationResult Validate(object? value, ValidatorCondition condition)
    {
        // Use TypedValue for type-safe access
        var typedValue = TypedValue.From(value);
        var stringValue = typedValue.AsString();

        if (string.IsNullOrEmpty(stringValue))
        {
            return CreateSuccess(); // Empty string is not invalid for max_len
        }

        // Get condition value in a type-safe manner
        var conditionTypedValue = condition.GetTypedValue();

        if (!conditionTypedValue.TryGetInt32(out var maxLength))
        {
            // Use JsonElementHelper for backward compatibility (may throw exception)
            maxLength = JsonElement.GetInt32(condition.Value, ValidatorType);
        }

        if (stringValue.Length > maxLength)
        {
            return CreateFailure(
                condition.Message, 
                string.Empty, 
                value, 
                $"maximum length of {maxLength} characters (actual length: {stringValue.Length})");
        }
        return CreateSuccess();
    }
}