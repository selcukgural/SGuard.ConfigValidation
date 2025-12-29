using System.Text.Json;

namespace SGuard.ConfigValidation.Common;

/// <summary>
/// Internal generic type-safe value wrapper. Provides type-safe access without boxing for value types.
/// </summary>
internal sealed class TypedValue<T>
{
    private readonly T? _value;
    private readonly ValueType _valueType;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypedValue{T}"/> class with the specified value and value type.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="valueType">The type of the value.</param>
    internal TypedValue(T? value, ValueType valueType)
    {
        _value = value;
        _valueType = valueType;
    }

    /// <summary>
    /// Gets the type of the value.
    /// </summary>
    internal ValueType Type => _valueType;

    /// <summary>
    /// Gets the value directly without boxing.
    /// </summary>
    internal T? Value => _value;
}

/// <summary>
/// Type-safe value wrapper. Provides type-safe access for ValidatorCondition.Value.
/// </summary>
public sealed class TypedValue
{
    private readonly object? _wrappedValue;
    private readonly ValueType _valueType;
    private readonly bool _isGeneric;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypedValue"/> class with the specified value and value type.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="valueType">The type of the value.</param>
    private TypedValue(object? value, ValueType valueType)
        : this(value, valueType, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypedValue"/> class from a generic typed value.
    /// </summary>
    /// <param name="typedValue">The generic typed value to wrap.</param>
    /// <param name="valueType">The type of the value.</param>
    /// <param name="isGeneric">Indicates whether the value is wrapped in a generic TypedValue instance.</param>
    private TypedValue(object? typedValue, ValueType valueType, bool isGeneric)
    {
        _wrappedValue = typedValue;
        _valueType = valueType;
        _isGeneric = isGeneric;
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
        if (_isGeneric)
        {
            return _valueType switch
            {
                ValueType.String => GetGenericValue<string>()?.ToString(),
                ValueType.Number => GetGenericNumericAsString(),
                ValueType.Boolean => GetGenericValue<bool>().ToString(),
                ValueType.Null => null,
                _ => _wrappedValue?.ToString()
            };
        }

        return _valueType switch
        {
            ValueType.String      => _wrappedValue as string,
            ValueType.Number      => _wrappedValue?.ToString(),
            ValueType.Boolean     => _wrappedValue?.ToString(),
            ValueType.Null        => null,
            ValueType.Array       => _wrappedValue?.ToString(),
            ValueType.Object      => _wrappedValue?.ToString(),
            ValueType.JsonElement => ExtractStringFromJsonElement(),
            _                     => _wrappedValue?.ToString()
        };
    }

    private string? GetGenericNumericAsString()
    {
        return _wrappedValue switch
        {
            TypedValue<int> tv => tv.Value.ToString(),
            TypedValue<long> tv => tv.Value.ToString(),
            TypedValue<short> tv => tv.Value.ToString(),
            TypedValue<byte> tv => tv.Value.ToString(),
            TypedValue<float> tv => tv.Value.ToString(),
            TypedValue<double> tv => tv.Value.ToString(),
            TypedValue<decimal> tv => tv.Value.ToString(),
            _ => _wrappedValue?.ToString()
        };
    }

    private T? GetGenericValue<T>()
    {
        if (_wrappedValue is TypedValue<T> tv)
        {
            return tv.Value;
        }

        return default;
    }

    /// <summary>
    /// Attempts to return the value as a numeric value.
    /// If the underlying value is of type <c>Number</c>, tries to convert it to <c>double</c>.
    /// If the value is a <see cref="System.Text.Json.JsonElement"/>, tries to extract a numeric value from its kind.
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

        if (_isGeneric && _valueType == ValueType.Number)
        {
            return _wrappedValue switch
            {
                TypedValue<int> tv => TryConvertNumericToDouble(tv.Value, out numericValue),
                TypedValue<long> tv => TryConvertNumericToDouble(tv.Value, out numericValue),
                TypedValue<short> tv => TryConvertNumericToDouble(tv.Value, out numericValue),
                TypedValue<byte> tv => TryConvertNumericToDouble(tv.Value, out numericValue),
                TypedValue<float> tv => TryConvertNumericToDouble(tv.Value, out numericValue),
                TypedValue<double> tv => TryConvertNumericToDouble(tv.Value, out numericValue),
                TypedValue<decimal> tv => TryConvertNumericToDouble(tv.Value, out numericValue),
                _ => false
            };
        }

        if (_isGeneric && _valueType == ValueType.String)
        {
            if (_wrappedValue is TypedValue<string> tv)
            {
                return double.TryParse(tv.Value, out numericValue);
            }
        }

        if (_wrappedValue == null) return false;

        return _valueType switch
        {
            ValueType.Number      => TryConvertToDouble(_wrappedValue, out numericValue),
            ValueType.JsonElement => TryExtractNumericFromJsonElement(out numericValue),
            ValueType.String      => double.TryParse(_wrappedValue.ToString(), out numericValue),
            _                     => false
        };
    }

    private static bool TryConvertNumericToDouble<T>(T value, out double result) where T : struct
    {
        result = value switch
        {
            int i => i,
            long l => l,
            short s => s,
            byte b => b,
            float f => f,
            double d => d,
            decimal dec => (double)dec,
            _ => 0
        };
        return true;
    }

    /// <summary>
    /// Attempts to return the value as an integer.
    /// If the underlying value is of type <c>Number</c>, tries to convert it to <c>int</c>.
    /// If the value is a <see cref="System.Text.Json.JsonElement"/>, tries to extract an integer from its kind.
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

        if (_isGeneric && _valueType == ValueType.Number)
        {
            return _wrappedValue switch
            {
                TypedValue<int> tv => TryConvertNumericToInt32(tv.Value, out intValue),
                TypedValue<long> tv => TryConvertNumericToInt32(tv.Value, out intValue),
                TypedValue<short> tv => TryConvertNumericToInt32(tv.Value, out intValue),
                TypedValue<byte> tv => TryConvertNumericToInt32(tv.Value, out intValue),
                TypedValue<float> tv => TryConvertNumericToInt32(tv.Value, out intValue),
                TypedValue<double> tv => TryConvertNumericToInt32(tv.Value, out intValue),
                TypedValue<decimal> tv => TryConvertNumericToInt32(tv.Value, out intValue),
                _ => false
            };
        }

        if (_isGeneric && _valueType == ValueType.String)
        {
            if (_wrappedValue is TypedValue<string> tv)
            {
                return int.TryParse(tv.Value, out intValue);
            }
        }

        if (_wrappedValue == null) return false;

        return _valueType switch
        {
            ValueType.Number      => TryConvertToInt32(_wrappedValue, out intValue),
            ValueType.JsonElement => TryExtractInt32FromJsonElement(out intValue),
            ValueType.String      => int.TryParse(_wrappedValue.ToString(), out intValue),
            _                     => false
        };
    }

    private static bool TryConvertNumericToInt32<T>(T value, out int result) where T : struct
    {
        result = value switch
        {
            int i => i,
            short s => s,
            byte b => b,
            long l and >= int.MinValue and <= int.MaxValue => (int)l,
            _ => 0
        };

        return value switch
        {
            int => true,
            short => true,
            byte => true,
            long l => l >= int.MinValue && l <= int.MaxValue,
            _ => false
        };
    }

    /// <summary>
    /// Attempts to extract the value as a string array.
    /// If the underlying value is a <see cref="System.Text.Json.JsonElement"/> of array kind, each element is converted to a string.
    /// If the underlying value is already a string array, it is returned directly.
    /// </summary>
    /// <param name="arrayValue">When this method returns, contains the string array if extraction succeeds; otherwise, an empty array.</param>
    /// <returns><c>true</c> if the value can be represented as a string array; otherwise, <c>false</c>.</returns>
    public bool TryGetStringArray(out string[] arrayValue)
    {
        arrayValue = [];

        if (_wrappedValue == null) return false;

        if (_valueType == ValueType.JsonElement && _wrappedValue is System.Text.Json.JsonElement { ValueKind: JsonValueKind.Array } jsonElement)
        {
            var list = new List<string>();

            foreach (var item in jsonElement.EnumerateArray())
            {
                list.Add(item.ToString());
            }

            arrayValue = list.ToArray();
            return true;
        }

        if (_wrappedValue is not string[] strArray)
        {
            return false;
        }

        arrayValue = strArray;
        return true;
    }

    /// <summary>
    /// Creates a <see cref="TypedValue"/> instance from an integer value without boxing.
    /// </summary>
    /// <param name="value">The integer value to wrap.</param>
    /// <returns>A <see cref="TypedValue"/> instance of type <c>Number</c>.</returns>
    public static TypedValue From(int value)
    {
        return new TypedValue(new TypedValue<int>(value, ValueType.Number), ValueType.Number, true);
    }

    /// <summary>
    /// Creates a <see cref="TypedValue"/> instance from a long value without boxing.
    /// </summary>
    /// <param name="value">The long value to wrap.</param>
    /// <returns>A <see cref="TypedValue"/> instance of type <c>Number</c>.</returns>
    public static TypedValue From(long value)
    {
        return new TypedValue(new TypedValue<long>(value, ValueType.Number), ValueType.Number, true);
    }

    /// <summary>
    /// Creates a <see cref="TypedValue"/> instance from a short value without boxing.
    /// </summary>
    /// <param name="value">The short value to wrap.</param>
    /// <returns>A <see cref="TypedValue"/> instance of type <c>Number</c>.</returns>
    public static TypedValue From(short value)
    {
        return new TypedValue(new TypedValue<short>(value, ValueType.Number), ValueType.Number, true);
    }

    /// <summary>
    /// Creates a <see cref="TypedValue"/> instance from a byte value without boxing.
    /// </summary>
    /// <param name="value">The byte value to wrap.</param>
    /// <returns>A <see cref="TypedValue"/> instance of type <c>Number</c>.</returns>
    public static TypedValue From(byte value)
    {
        return new TypedValue(new TypedValue<byte>(value, ValueType.Number), ValueType.Number, true);
    }

    /// <summary>
    /// Creates a <see cref="TypedValue"/> instance from a float value without boxing.
    /// </summary>
    /// <param name="value">The float value to wrap.</param>
    /// <returns>A <see cref="TypedValue"/> instance of type <c>Number</c>.</returns>
    public static TypedValue From(float value)
    {
        return new TypedValue(new TypedValue<float>(value, ValueType.Number), ValueType.Number, true);
    }

    /// <summary>
    /// Creates a <see cref="TypedValue"/> instance from a double value without boxing.
    /// </summary>
    /// <param name="value">The double value to wrap.</param>
    /// <returns>A <see cref="TypedValue"/> instance of type <c>Number</c>.</returns>
    public static TypedValue From(double value)
    {
        return new TypedValue(new TypedValue<double>(value, ValueType.Number), ValueType.Number, true);
    }

    /// <summary>
    /// Creates a <see cref="TypedValue"/> instance from a decimal value without boxing.
    /// </summary>
    /// <param name="value">The decimal value to wrap.</param>
    /// <returns>A <see cref="TypedValue"/> instance of type <c>Number</c>.</returns>
    public static TypedValue From(decimal value)
    {
        return new TypedValue(new TypedValue<decimal>(value, ValueType.Number), ValueType.Number, true);
    }

    /// <summary>
    /// Creates a <see cref="TypedValue"/> instance from a string value.
    /// </summary>
    /// <param name="value">The string value to wrap.</param>
    /// <returns>A <see cref="TypedValue"/> instance of type <c>String</c>.</returns>
    public static TypedValue From(string? value)
    {
        if (value == null)
        {
            return new TypedValue(null, ValueType.Null, false);
        }

        return new TypedValue(new TypedValue<string>(value, ValueType.String), ValueType.String, true);
    }

    /// <summary>
    /// Creates a <see cref="TypedValue"/> instance from a boolean value without boxing.
    /// </summary>
    /// <param name="value">The boolean value to wrap.</param>
    /// <returns>A <see cref="TypedValue"/> instance of type <c>Boolean</c>.</returns>
    public static TypedValue From(bool value)
    {
        return new TypedValue(new TypedValue<bool>(value, ValueType.Boolean), ValueType.Boolean, true);
    }

    /// <summary>
    /// Creates a <see cref="TypedValue"/> instance that wraps the provided value in a type-safe manner.
    /// Determines the <see cref="ValueType"/> based on the runtime type and content of the value:
    /// <list type="bullet">
    ///   <item>If <paramref name="value"/> is <c>null</c> or a <see cref="System.Text.Json.JsonElement"/> with <see cref="JsonValueKind.Null"/>, returns a <see cref="TypedValue"/> of type <c>Null</c>.</item>
    ///   <item>If <paramref name="value"/> is a <see cref="System.Text.Json.JsonElement"/> with <see cref="JsonValueKind"/> of <c>String</c>, <c>Number</c>, <c>True</c>, <c>False</c>, <c>Array</c>, or <c>Object</c>, returns a <see cref="TypedValue"/> of type <c>JsonElement</c>.</item>
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
            System.Text.Json.JsonElement { ValueKind: JsonValueKind.Null } => new TypedValue(null, ValueType.Null),
            System.Text.Json.JsonElement
            {
                ValueKind: JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False or JsonValueKind.Array
                           or JsonValueKind.Object
            } => new TypedValue(value, ValueType.JsonElement),
            int i => From(i),
            long l => From(l),
            short s => From(s),
            byte b => From(b),
            float f => From(f),
            double d => From(d),
            decimal dec => From(dec),
            string str => From(str),
            bool bl => From(bl),
            _ => new TypedValue(value, ValueType.Unknown)
        };
    }

    /// <summary>
    /// Extracts a string representation from the underlying <see cref="System.Text.Json.JsonElement"/>.
    /// Handles different <see cref="JsonValueKind"/>s:
    /// - String: returns the string value.
    /// - Number: returns the raw JSON text.
    /// - True/False: returns "true"/"false" as string.
    /// - Others: returns the raw JSON text.
    /// Returns null if the value is not a <see cref="System.Text.Json.JsonElement"/>.
    /// </summary>
    /// <returns>The string representation, or null if not applicable.</returns>
    private string? ExtractStringFromJsonElement()
    {
        if (_wrappedValue is not System.Text.Json.JsonElement element)
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
    /// Attempts to extract a numeric value from the underlying <see cref="System.Text.Json.JsonElement"/>.
    /// Only succeeds if the element is of <see cref="JsonValueKind.Number"/>.
    /// Tries to get the value as double, falls back to Int64 if necessary.
    /// </summary>
    /// <param name="numericValue">The extracted numeric value, or 0 if extraction fails.</param>
    /// <returns>True if extraction succeeds, otherwise false.</returns>
    private bool TryExtractNumericFromJsonElement(out double numericValue)
    {
        numericValue = 0;

        if (_wrappedValue is not System.Text.Json.JsonElement { ValueKind: JsonValueKind.Number } element)
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
    /// Attempts to extract an <c>int</c> value from the underlying <see cref="System.Text.Json.JsonElement"/>.
    /// Supports both <see cref="JsonValueKind.Number"/> and <see cref="JsonValueKind.String"/> (if the string can be parsed as an integer).
    /// </summary>
    /// <param name="intValue">When this method returns, contains the extracted integer value if successful; otherwise, <c>0</c>.</param>
    /// <returns><c>true</c> if extraction succeeds; otherwise, <c>false</c>.</returns>
    private bool TryExtractInt32FromJsonElement(out int intValue)
    {
        intValue = 0;

        if (_wrappedValue is not System.Text.Json.JsonElement element)
        {
            return false;
        }

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt32(out intValue),
            JsonValueKind.String when int.TryParse(element.GetString(), out intValue) => true,
            _ => false
        };
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
/// Enumerates the possible types of values that can be represented by <see cref="TypedValue"/>.
/// </summary>
public enum ValueType
{
    /// <summary>
    /// The value type is unknown or not recognized.
    /// </summary>
    Unknown,

    /// <summary>
    /// The value is a string.
    /// </summary>
    String,

    /// <summary>
    /// The value is a numeric type (e.g., int, double, decimal).
    /// </summary>
    Number,

    /// <summary>
    /// The value is a boolean (true or false).
    /// </summary>
    Boolean,

    /// <summary>
    /// The value is an array.
    /// </summary>
    Array,

    /// <summary>
    /// The value is an object.
    /// </summary>
    Object,

    /// <summary>
    /// The value is null.
    /// </summary>
    Null,

    /// <summary>
    /// The value is a <see cref="System.Text.Json.JsonElement"/>.
    /// </summary>
    JsonElement
}