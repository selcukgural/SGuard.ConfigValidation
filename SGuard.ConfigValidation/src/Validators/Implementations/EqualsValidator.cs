using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators.Base;

namespace SGuard.ConfigValidation.Validators;

public sealed class EqualsValidator : BaseValidator<object>
{
    public override string ValidatorType => "eq";
    
    // Equals validator supports all value types
    public override IReadOnlySet<Common.ValueType> SupportedValueTypes { get; } = 
        new HashSet<Common.ValueType> { Common.ValueType.String, Common.ValueType.Number, Common.ValueType.Boolean, Common.ValueType.Array, Common.ValueType.Object, Common.ValueType.Null, Common.ValueType.JsonElement };

    public override ValidationResult Validate(object? value, ValidatorCondition condition)
    {
        // Preserve original behavior: use Equals (case-sensitive, null-aware)
        // Return false for null == null (test expectation)
        if (value == null && condition.Value == null)
        {
            return CreateFailure(condition.Message, string.Empty, value, condition.Value);
        }
        
        var isEqual = value?.Equals(condition.Value) == true;
        
        return !isEqual ? CreateFailure(condition.Message, string.Empty, value, condition.Value) : CreateSuccess();
    }
}