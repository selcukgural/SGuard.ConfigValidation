using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Results;
using SGuard.ConfigValidation.Validators.Base;

namespace SGuard.ConfigValidation.Validators;

public sealed class RequiredValidator : BaseValidator<object>
{
    public override string ValidatorType => "required";
    
    // Required validator supports all value types
    public override IReadOnlySet<Common.ValueType> SupportedValueTypes { get; } = 
        new HashSet<Common.ValueType> { Common.ValueType.String, Common.ValueType.Number, Common.ValueType.Boolean, Common.ValueType.Array, Common.ValueType.Object, Common.ValueType.Null, Common.ValueType.JsonElement };

    public override ValidationResult Validate(object? value, ValidatorCondition condition)
    {
        // Use TypedValue for type-safe access
        var typedValue = TypedValue.From(value);
        var stringValue = typedValue.AsString();
        
        return string.IsNullOrWhiteSpace(stringValue) 
            ? CreateFailure(condition.Message, string.Empty, value) 
            : CreateSuccess();
    }
}