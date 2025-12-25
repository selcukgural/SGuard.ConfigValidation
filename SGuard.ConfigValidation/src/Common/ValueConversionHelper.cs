namespace SGuard.ConfigValidation.Common;

/// <summary>
/// Helper class for value conversion and comparison operations used by validators.
/// </summary>
public static class ValueConversionHelper
{
    /// <summary>
    /// Attempts to convert a value to double for numeric comparison.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted double value if successful.</param>
    /// <returns>True if conversion was successful, false otherwise.</returns>
    private static bool TryConvertToDouble(object? value, out double result)
    {
        result = 0;
        if (value == null) return false;

        switch (value)
        {
            case double d:
                result = d;
                return true;
            case float f:
                result = f;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case short s:
                result = s;
                return true;
            case byte b:
                result = b;
                return true;
            case decimal dec:
                result = (double)dec;
                return true;
            case string str when double.TryParse(str, out var parsed):
                result = parsed;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Compares two values for ordering. Attempts numeric comparison first, falls back to IComparable.
    /// </summary>
    /// <param name="value">The first value to compare.</param>
    /// <param name="conditionValue">The second value to compare.</param>
    /// <returns>
    /// A signed integer that indicates the relative values:
    /// - Less than zero: value is less than conditionValue
    /// - Zero: value equals conditionValue
    /// - Greater than zero: value is greater than conditionValue
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when values cannot be compared.</exception>
    public static int CompareValues(object value, object? conditionValue)
    {
        if (conditionValue == null)
        {
            throw new ArgumentException("Condition value cannot be null for comparison");
        }

        // Try to convert both to double for numeric comparison
        if (TryConvertToDouble(value, out var valueDouble) && TryConvertToDouble(conditionValue, out var conditionDouble))
        {
            return valueDouble.CompareTo(conditionDouble);
        }

        // Fall back to IComparable if both are comparable
        if (value is IComparable comparableValue && conditionValue is IComparable comparableCondition)
        {
            return comparableValue.CompareTo(comparableCondition);
        }

        throw new ArgumentException("Values are not comparable");
    }
}

