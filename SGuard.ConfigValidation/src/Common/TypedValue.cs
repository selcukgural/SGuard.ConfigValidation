using System.Text.Json;

namespace SGuard.ConfigValidation.Common;

/// <summary>
/// Type-safe value wrapper. Provides type-safe access for ValidatorCondition.Value.
/// </summary>
public sealed class TypedValue
{
    private readonly object? _value;
    private readonly ValueType _valueType;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypedValue"/> class with the specified value and value type.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="valueType">The type of the value.</param>
    private TypedValue(object? value, ValueType valueType)
    {
        _value = value;
        _valueType = valueType;
    }

    /// <summary>
    /// Gets the type of the value.
    /// </summary>
    public ValueType Type => _valueType;

    /// <summary>
    /// Returns the value as a string.
    /// </summary>
    /// <returns>The string representation of the value, or null if not applicable.</returns>
    public string? AsString()
    {
        return _valueType switch
        {
            ValueType.String      => _value as string,
            ValueType.Number      => _value?.ToString(),
            ValueType.Boolean     => _value?.ToString(),
            ValueType.Null        => null,
            ValueType.Array       => _value?.ToString(),
            ValueType.Object      => _value?.ToString(),
            ValueType.JsonElement => ExtractStringFromJsonElement(),
            _                     => _value?.ToString()
        };
    }

    /// <summary>
    /// Attempts to return the value as a numeric value.
    /// If the underlying value is of type <c>Number</c>, tries to convert it to <c>double</c>.
    /// If the value is a <see cref="JsonElement"/>, tries to extract a numeric value from its kind.
    /// If the value is a string, parses it as a <c>double</c>.
    /// </summary>
    /// <param name="numericValue">
    /// When this method returns, contains the numeric value if extraction succeeds; otherwise, <c>0</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value can be represented as a numeric value; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetNumeric(out double numericValue)
    {
        numericValue = 0;

        if (_value == null) return false;

        return _valueType switch
        {
            ValueType.Number      => TryConvertToDouble(_value, out numericValue),
            ValueType.JsonElement => TryExtractNumericFromJsonElement(out numericValue),
            ValueType.String      => double.TryParse(_value.ToString(), out numericValue),
            _                     => false
        };
    }

    /// <summary>
    /// Attempts to return the value as an integer.
    /// If the underlying value is of type <c>Number</c>, tries to convert it to <c>int</c>.
    /// If the value is a <see cref="JsonElement"/>, tries to extract an integer from its kind.
    /// If the value is a string, parse it as an integer.
    /// </summary>
    /// <param name="intValue">
    /// When this method returns, contains the integer value if extraction succeeds; otherwise, <c>0</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value can be represented as an integer; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetInt32(out int intValue)
    {
        intValue = 0;

        if (_value == null) return false;

        return _valueType switch
        {
            ValueType.Number      => TryConvertToInt32(_value, out intValue),
            ValueType.JsonElement => TryExtractInt32FromJsonElement(out intValue),
            ValueType.String      => int.TryParse(_value.ToString(), out intValue),
            _                     => false
        };
    }

    /// <summary>
    /// Attempts to extract the value as a string array.
    /// If the underlying value is a <see cref="JsonElement"/> of array kind, each element is converted to a string.
    /// If the underlying value is already a string array, it is returned directly.
    /// </summary>
    /// <param name="arrayValue">When this method returns, contains the string array if extraction succeeds; otherwise, an empty array.</param>
    /// <returns><c>true</c> if the value can be represented as a string array; otherwise, <c>false</c>.</returns>
    public bool TryGetStringArray(out string[] arrayValue)
    {
        arrayValue = [];

        if (_value == null) return false;

        if (_valueType == ValueType.JsonElement && _value is JsonElement { ValueKind: JsonValueKind.Array } jsonElement)
        {
            var list = new List<string>();

            foreach (var item in jsonElement.EnumerateArray())
            {
                list.Add(item.ToString());
            }

            arrayValue = list.ToArray();
            return true;
        }

        if (_value is not string[] strArray)
        {
            return false;
        }

        arrayValue = strArray;
        return true;
    }

    /// <summary>
    /// Creates a <see cref="TypedValue"/> instance that wraps the provided value in a type-safe manner.
    /// Determines the <see cref="ValueType"/> based on the runtime type and content of the value:
    /// <list type="bullet">
    ///   <item>If <paramref name="value"/> is <c>null</c> or a <see cref="JsonElement"/> with <see cref="JsonValueKind.Null"/>, returns a <see cref="TypedValue"/> of type <c>Null</c>.</item>
    ///   <item>If <paramref name="value"/> is a <see cref="JsonElement"/> with <see cref="JsonValueKind"/> of <c>String</c>, <c>Number</c>, <c>True</c>, <c>False</c>, <c>Array</c>, or <c>Object</c>, returns a <see cref="TypedValue"/> of type <c>JsonElement</c>.</item>
    ///   <item>If <paramref name="value"/> is a numeric type (<c>int</c>, <c>long</c>, <c>short</c>, <c>byte</c>, <c>float</c>, <c>double</c>, <c>decimal</c>), returns a <see cref="TypedValue"/> of type <c>Number</c>.</item>
    ///   <item>If <paramref name="value"/> is a <c>string</c>, returns a <see cref="TypedValue"/> of type <c>String</c>.</item>
    ///   <item>If <paramref name="value"/> is a <c>bool</c>, returns a <see cref="TypedValue"/> of type <c>Boolean</c>.</item>
    ///   <item>Otherwise, returns a <see cref="TypedValue"/> of type <c>Unknown</c>.</item>
    /// </list>
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A <see cref="TypedValue"/> instance representing the value and its type.</returns>
    public static TypedValue From(object? value)
    {
        if (value == null)
        {
            return new TypedValue(null, ValueType.Null);
        }

        return value switch
        {
            JsonElement { ValueKind: JsonValueKind.Null } => new TypedValue(null, ValueType.Null),
            JsonElement
            {
                ValueKind: JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False or JsonValueKind.Array
                           or JsonValueKind.Object
            } => new TypedValue(value, ValueType.JsonElement),
            int or long or short or byte or float or double or decimal => new TypedValue(value, ValueType.Number),
            string                                                     => new TypedValue(value, ValueType.String),
            bool                                                       => new TypedValue(value, ValueType.Boolean),
            _                                                          => new TypedValue(value, ValueType.Unknown)
        };
    }

    /// <summary>
    /// Extracts a string representation from the underlying <see cref="JsonElement"/>.
    /// Handles different <see cref="JsonValueKind"/>s:
    /// - String: returns the string value.
    /// - Number: returns the raw JSON text.
    /// - True/False: returns "true"/"false" as string.
    /// - Others: returns the raw JSON text.
    /// Returns null if the value is not a <see cref="JsonElement"/>.
    /// </summary>
    /// <returns>The string representation, or null if not applicable.</returns>
    private string? ExtractStringFromJsonElement()
    {
        if (_value is not JsonElement element)
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True   => "true",
            JsonValueKind.False  => "false",
            _                    => element.GetRawText()
        };
    }

    /// <summary>
    /// Attempts to extract a numeric value from the underlying <see cref="JsonElement"/>.
    /// Only succeeds if the element is of <see cref="JsonValueKind.Number"/>.
    /// Tries to get the value as double, falls back to Int64 if necessary.
    /// </summary>
    /// <param name="numericValue">The extracted numeric value, or 0 if extraction fails.</param>
    /// <returns>True if extraction succeeds, otherwise false.</returns>
    private bool TryExtractNumericFromJsonElement(out double numericValue)
    {
        numericValue = 0;

        if (_value is not JsonElement { ValueKind: JsonValueKind.Number } element)
        {
            return false;
        }

        if (element.TryGetDouble(out numericValue))
        {
            return true;
        }

        if (!element.TryGetInt64(out var intVal))
        {
            return false;
        }

        numericValue = intVal;
        return true;
    }

    /// <summary>
    /// Attempts to extract an <c>int</c> value from the underlying <see cref="JsonElement"/>.
    /// Only succeeds if the element is of <see cref="JsonValueKind.Number"/>.
    /// </summary>
    /// <param name="intValue">When this method returns, contains the extracted integer value if successful; otherwise, <c>0</c>.</param>
    /// <returns><c>true</c> if extraction succeeds; otherwise, <c>false</c>.</returns>
    private bool TryExtractInt32FromJsonElement(out int intValue)
    {
        intValue = 0;

        if (_value is JsonElement { ValueKind: JsonValueKind.Number } element)
        {
            return element.TryGetInt32(out intValue);
        }

        return false;
    }
    
    /// <summary>
    /// Attempts to convert the provided <paramref name="value"/> to a <c>double</c>.
    /// Supports numeric types: <c>double</c>, <c>float</c>, <c>int</c>, <c>long</c>, <c>short</c>, <c>byte</c>, and <c>decimal</c>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">
    /// When this method returns, contains the converted <c>double</c> value if conversion succeeds; otherwise, <c>0</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the conversion succeeds; otherwise, <c>false</c>.
    /// </returns>
    private static bool TryConvertToDouble(object value, out double result)
    {
        result = 0;

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
            default:
                return false;
        }
    }

    /// <summary>
    /// Attempts to convert the provided <paramref name="value"/> to an <c>int</c>.
    /// Supports numeric types: <c>int</c>, <c>short</c>, <c>byte</c>, and <c>long</c> (if within <c>int</c> range).
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">
    /// When this method returns, contains the converted <c>int</c> value if conversion succeeds; otherwise, <c>0</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the conversion succeeds; otherwise, <c>false</c>.
    /// </returns>
    private static bool TryConvertToInt32(object value, out int result)
    {
        result = 0;

        switch (value)
        {
            case int i:
                result = i;
                return true;
            case short s:
                result = s;
                return true;
            case byte b:
                result = b;
                return true;
            case long l and >= int.MinValue and <= int.MaxValue:
                result = (int)l;
                return true;
            default:
                return false;
        }
    }
}

/// <summary>
/// Represents value types.
/// </summary>
public enum ValueType
{
    Unknown,
    String,
    Number,
    Boolean,
    Array,
    Object,
    Null,
    JsonElement
}