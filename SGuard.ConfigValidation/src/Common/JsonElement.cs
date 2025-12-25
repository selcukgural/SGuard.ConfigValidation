using System.Text.Json;
using SGuard.ConfigValidation.Resources;

namespace SGuard.ConfigValidation.Common;

/// <summary>
/// Helper class for converting JsonElement and other types to int32.
/// </summary>
public static class JsonElement
{
    /// <summary>
    /// Attempts to convert a value to int32. Supports JsonElement (Number/String), int, and string types.
    /// </summary>
    /// <param name="value">The value to convert (can be JsonElement, int, or string).</param>
    /// <param name="result">The converted int32 value if successful.</param>
    /// <returns>True if conversion was successful, false otherwise.</returns>
    private static bool TryGetInt32(object? value, out int result)
    {
        result = 0;
        if (value == null) return false;

        switch (value)
        {
            case System.Text.Json.JsonElement { ValueKind: JsonValueKind.Number } element:
                return element.TryGetInt32(out result);
            case System.Text.Json.JsonElement { ValueKind: JsonValueKind.String } element when int.TryParse(element.GetString(), out var parsed):
                result = parsed;
                return true;
            case int intValue:
                result = intValue;
                return true;
            case string valueString when int.TryParse(valueString, out var parsed):
                result = parsed;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Converts a value to int32, throwing an exception if conversion fails.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="validatorType">The validator type name for error messages.</param>
    /// <returns>The converted int32 value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when conversion fails.</exception>
    public static int GetInt32(object? value, string validatorType)
    {
        return !TryGetInt32(value, out var result) ? throw This.InvalidOperationException(nameof(SR.InvalidOperationException_CannotConvertToInt), value ?? "null", validatorType) : result;
    }
}

