using System.Text.Json;
using FluentAssertions;
using SGuard.ConfigValidation.Common;
using ValueType = SGuard.ConfigValidation.Common.ValueType;

namespace SGuard.ConfigValidation.Test;

public sealed class TypedValueTests
{
    [Fact]
    public void From_With_Null_Should_Return_NullType()
    {
        // Act
        var typedValue = TypedValue.From(null);

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.Null);
    }

    [Fact]
    public void From_With_JsonElementNull_Should_Return_NullType()
    {
        // Arrange
        var json = JsonDocument.Parse("null");
        var element = json.RootElement;

        // Act
        var typedValue = TypedValue.From(element);

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.Null);
    }

    [Fact]
    public void From_With_JsonElementString_Should_Return_JsonElementType()
    {
        // Arrange
        var json = JsonDocument.Parse("\"test\"");
        var element = json.RootElement;

        // Act
        var typedValue = TypedValue.From(element);

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.JsonElement);
    }

    [Fact]
    public void From_With_JsonElementNumber_Should_Return_JsonElementType()
    {
        // Arrange
        var json = JsonDocument.Parse("42");
        var element = json.RootElement;

        // Act
        var typedValue = TypedValue.From(element);

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.JsonElement);
    }

    [Fact]
    public void From_With_JsonElementBoolean_Should_Return_JsonElementType()
    {
        // Arrange
        var json = JsonDocument.Parse("true");
        var element = json.RootElement;

        // Act
        var typedValue = TypedValue.From(element);

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.JsonElement);
    }

    [Fact]
    public void From_With_JsonElementArray_Should_Return_JsonElementType()
    {
        // Arrange
        var json = JsonDocument.Parse("[1, 2, 3]");
        var element = json.RootElement;

        // Act
        var typedValue = TypedValue.From(element);

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.JsonElement);
    }

    [Fact]
    public void From_With_JsonElementObject_Should_Return_JsonElementType()
    {
        // Arrange
        var json = JsonDocument.Parse("{\"key\": \"value\"}");
        var element = json.RootElement;

        // Act
        var typedValue = TypedValue.From(element);

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.JsonElement);
    }

    [Fact]
    public void From_With_Int_Should_Return_NumberType()
    {
        // Act
        var typedValue = TypedValue.From(42);

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.Number);
    }

    [Fact]
    public void From_With_Long_Should_Return_NumberType()
    {
        // Act
        var typedValue = TypedValue.From(42L);

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.Number);
    }

    [Fact]
    public void From_With_Short_Should_Return_NumberType()
    {
        // Act
        var typedValue = TypedValue.From((short)42);

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.Number);
    }

    [Fact]
    public void From_With_Byte_Should_Return_NumberType()
    {
        // Act
        var typedValue = TypedValue.From((byte)42);

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.Number);
    }

    [Fact]
    public void From_With_Float_Should_Return_NumberType()
    {
        // Act
        var typedValue = TypedValue.From(42.5f);

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.Number);
    }

    [Fact]
    public void From_With_Double_Should_Return_NumberType()
    {
        // Act
        var typedValue = TypedValue.From(42.5);

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.Number);
    }

    [Fact]
    public void From_With_Decimal_Should_Return_NumberType()
    {
        // Act
        var typedValue = TypedValue.From(42.5m);

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.Number);
    }

    [Fact]
    public void From_With_String_Should_Return_StringType()
    {
        // Act
        var typedValue = TypedValue.From("test");

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.String);
    }

    [Fact]
    public void From_With_Bool_Should_Return_BooleanType()
    {
        // Act
        var typedValue = TypedValue.From(true);

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.Boolean);
    }

    [Fact]
    public void From_With_UnknownType_Should_Return_UnknownType()
    {
        // Act
        var typedValue = TypedValue.From(new object());

        // Assert
        typedValue.Should().NotBeNull();
        typedValue.Type.Should().Be(ValueType.Unknown);
    }

    [Fact]
    public void AsString_With_StringType_Should_Return_String()
    {
        // Arrange
        var typedValue = TypedValue.From("test");

        // Act
        var result = typedValue.AsString();

        // Assert
        result.Should().Be("test");
    }

    [Fact]
    public void AsString_With_NumberType_Should_Return_StringRepresentation()
    {
        // Arrange
        var typedValue = TypedValue.From(42);

        // Act
        var result = typedValue.AsString();

        // Assert
        result.Should().Be("42");
    }

    [Fact]
    public void AsString_With_BooleanType_Should_Return_StringRepresentation()
    {
        // Arrange
        var typedValue = TypedValue.From(true);

        // Act
        var result = typedValue.AsString();

        // Assert
        result.Should().Be("True");
    }

    [Fact]
    public void AsString_With_NullType_Should_Return_Null()
    {
        // Arrange
        var typedValue = TypedValue.From(null);

        // Act
        var result = typedValue.AsString();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AsString_With_JsonElementString_Should_Return_String()
    {
        // Arrange
        var json = JsonDocument.Parse("\"test\"");
        var typedValue = TypedValue.From(json.RootElement);

        // Act
        var result = typedValue.AsString();

        // Assert
        result.Should().Be("test");
    }

    [Fact]
    public void AsString_With_JsonElementNumber_Should_Return_RawText()
    {
        // Arrange
        var json = JsonDocument.Parse("42");
        var typedValue = TypedValue.From(json.RootElement);

        // Act
        var result = typedValue.AsString();

        // Assert
        result.Should().Be("42");
    }

    [Fact]
    public void AsString_With_JsonElementTrue_Should_Return_True()
    {
        // Arrange
        var json = JsonDocument.Parse("true");
        var typedValue = TypedValue.From(json.RootElement);

        // Act
        var result = typedValue.AsString();

        // Assert
        result.Should().Be("true");
    }

    [Fact]
    public void AsString_With_JsonElementFalse_Should_Return_False()
    {
        // Arrange
        var json = JsonDocument.Parse("false");
        var typedValue = TypedValue.From(json.RootElement);

        // Act
        var result = typedValue.AsString();

        // Assert
        result.Should().Be("false");
    }

    [Fact]
    public void AsString_With_JsonElementArray_Should_Return_RawText()
    {
        // Arrange
        var json = JsonDocument.Parse("[1, 2, 3]");
        var typedValue = TypedValue.From(json.RootElement);

        // Act
        var result = typedValue.AsString();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("1");
        result.Should().Contain("2");
        result.Should().Contain("3");
        result.Should().StartWith("[");
        result.Should().EndWith("]");
    }

    [Fact]
    public void AsString_With_JsonElementObject_Should_Return_RawText()
    {
        // Arrange
        var json = JsonDocument.Parse("{\"key\": \"value\"}");
        var typedValue = TypedValue.From(json.RootElement);

        // Act
        var result = typedValue.AsString();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("key");
    }

    [Fact]
    public void TryGetNumeric_With_NumberType_Int_Should_ReturnTrue()
    {
        // Arrange
        var typedValue = TypedValue.From(42);

        // Act
        var result = typedValue.TryGetNumeric(out var numericValue);

        // Assert
        result.Should().BeTrue();
        numericValue.Should().Be(42.0);
    }

    [Fact]
    public void TryGetNumeric_With_NumberType_Double_Should_ReturnTrue()
    {
        // Arrange
        var typedValue = TypedValue.From(42.5);

        // Act
        var result = typedValue.TryGetNumeric(out var numericValue);

        // Assert
        result.Should().BeTrue();
        numericValue.Should().Be(42.5);
    }

    [Fact]
    public void TryGetNumeric_With_JsonElementNumber_Should_ReturnTrue()
    {
        // Arrange
        var json = JsonDocument.Parse("42.5");
        var typedValue = TypedValue.From(json.RootElement);

        // Act
        var result = typedValue.TryGetNumeric(out var numericValue);

        // Assert
        result.Should().BeTrue();
        numericValue.Should().Be(42.5);
    }

    [Fact]
    public void TryGetNumeric_With_JsonElementNumber_Int64_Should_ReturnTrue()
    {
        // Arrange
        var json = JsonDocument.Parse("9223372036854775807");
        var typedValue = TypedValue.From(json.RootElement);

        // Act
        var result = typedValue.TryGetNumeric(out var numericValue);

        // Assert
        result.Should().BeTrue();
        numericValue.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TryGetNumeric_With_StringType_ValidNumber_Should_ReturnTrue()
    {
        // Arrange
        // Use InvariantCulture format to avoid locale-specific parsing issues
        var typedValue = TypedValue.From("42");

        // Act
        var result = typedValue.TryGetNumeric(out var numericValue);

        // Assert
        result.Should().BeTrue();
        numericValue.Should().Be(42.0);
    }

    [Fact]
    public void TryGetNumeric_With_StringType_InvalidNumber_Should_ReturnFalse()
    {
        // Arrange
        var typedValue = TypedValue.From("not-a-number");

        // Act
        var result = typedValue.TryGetNumeric(out var numericValue);

        // Assert
        result.Should().BeFalse();
        numericValue.Should().Be(0);
    }

    [Fact]
    public void TryGetNumeric_With_NullType_Should_ReturnFalse()
    {
        // Arrange
        var typedValue = TypedValue.From(null);

        // Act
        var result = typedValue.TryGetNumeric(out var numericValue);

        // Assert
        result.Should().BeFalse();
        numericValue.Should().Be(0);
    }

    [Fact]
    public void TryGetNumeric_With_InvalidType_Should_ReturnFalse()
    {
        // Arrange
        var typedValue = TypedValue.From(true);

        // Act
        var result = typedValue.TryGetNumeric(out var numericValue);

        // Assert
        result.Should().BeFalse();
        numericValue.Should().Be(0);
    }

    [Fact]
    public void TryGetInt32_With_NumberType_Int_Should_ReturnTrue()
    {
        // Arrange
        var typedValue = TypedValue.From(42);

        // Act
        var result = typedValue.TryGetInt32(out var intValue);

        // Assert
        result.Should().BeTrue();
        intValue.Should().Be(42);
    }

    [Fact]
    public void TryGetInt32_With_NumberType_Short_Should_ReturnTrue()
    {
        // Arrange
        var typedValue = TypedValue.From((short)42);

        // Act
        var result = typedValue.TryGetInt32(out var intValue);

        // Assert
        result.Should().BeTrue();
        intValue.Should().Be(42);
    }

    [Fact]
    public void TryGetInt32_With_NumberType_Byte_Should_ReturnTrue()
    {
        // Arrange
        var typedValue = TypedValue.From((byte)42);

        // Act
        var result = typedValue.TryGetInt32(out var intValue);

        // Assert
        result.Should().BeTrue();
        intValue.Should().Be(42);
    }

    [Fact]
    public void TryGetInt32_With_NumberType_Long_WithinRange_Should_ReturnTrue()
    {
        // Arrange
        var typedValue = TypedValue.From(42L);

        // Act
        var result = typedValue.TryGetInt32(out var intValue);

        // Assert
        result.Should().BeTrue();
        intValue.Should().Be(42);
    }

    [Fact]
    public void TryGetInt32_With_NumberType_Long_OutOfRange_Should_ReturnFalse()
    {
        // Arrange
        var typedValue = TypedValue.From(long.MaxValue);

        // Act
        var result = typedValue.TryGetInt32(out var intValue);

        // Assert
        result.Should().BeFalse();
        intValue.Should().Be(0);
    }

    [Fact]
    public void TryGetInt32_With_JsonElementNumber_Should_ReturnTrue()
    {
        // Arrange
        var json = JsonDocument.Parse("42");
        var typedValue = TypedValue.From(json.RootElement);

        // Act
        var result = typedValue.TryGetInt32(out var intValue);

        // Assert
        result.Should().BeTrue();
        intValue.Should().Be(42);
    }

    [Fact]
    public void TryGetInt32_With_JsonElementNumber_Double_Should_ReturnFalse()
    {
        // Arrange
        var json = JsonDocument.Parse("42.5");
        var typedValue = TypedValue.From(json.RootElement);

        // Act
        var result = typedValue.TryGetInt32(out var intValue);

        // Assert
        result.Should().BeFalse();
        intValue.Should().Be(0);
    }

    [Fact]
    public void TryGetInt32_With_StringType_ValidInt_Should_ReturnTrue()
    {
        // Arrange
        var typedValue = TypedValue.From("42");

        // Act
        var result = typedValue.TryGetInt32(out var intValue);

        // Assert
        result.Should().BeTrue();
        intValue.Should().Be(42);
    }

    [Fact]
    public void TryGetInt32_With_StringType_InvalidInt_Should_ReturnFalse()
    {
        // Arrange
        var typedValue = TypedValue.From("not-an-int");

        // Act
        var result = typedValue.TryGetInt32(out var intValue);

        // Assert
        result.Should().BeFalse();
        intValue.Should().Be(0);
    }

    [Fact]
    public void TryGetInt32_With_NullType_Should_ReturnFalse()
    {
        // Arrange
        var typedValue = TypedValue.From(null);

        // Act
        var result = typedValue.TryGetInt32(out var intValue);

        // Assert
        result.Should().BeFalse();
        intValue.Should().Be(0);
    }

    [Fact]
    public void TryGetInt32_With_InvalidType_Should_ReturnFalse()
    {
        // Arrange
        var typedValue = TypedValue.From(true);

        // Act
        var result = typedValue.TryGetInt32(out var intValue);

        // Assert
        result.Should().BeFalse();
        intValue.Should().Be(0);
    }

    [Fact]
    public void TryGetStringArray_With_JsonElementArray_Should_ReturnTrue()
    {
        // Arrange
        var json = JsonDocument.Parse("[\"a\", \"b\", \"c\"]");
        var typedValue = TypedValue.From(json.RootElement);

        // Act
        var result = typedValue.TryGetStringArray(out var arrayValue);

        // Assert
        result.Should().BeTrue();
        arrayValue.Should().NotBeNull();
        arrayValue.Should().HaveCount(3);
        arrayValue.Should().BeEquivalentTo(["a", "b", "c"]);
    }

    [Fact]
    public void TryGetStringArray_With_JsonElementArray_Numbers_Should_ReturnTrue()
    {
        // Arrange
        var json = JsonDocument.Parse("[1, 2, 3]");
        var typedValue = TypedValue.From(json.RootElement);

        // Act
        var result = typedValue.TryGetStringArray(out var arrayValue);

        // Assert
        result.Should().BeTrue();
        arrayValue.Should().NotBeNull();
        arrayValue.Should().HaveCount(3);
    }

    [Fact]
    public void TryGetStringArray_With_StringArrayType_Should_ReturnTrue()
    {
        // Arrange
        var stringArray = new[] { "a", "b", "c" };
        var typedValue = TypedValue.From(stringArray);

        // Act
        var result = typedValue.TryGetStringArray(out var arrayValue);

        // Assert
        result.Should().BeTrue();
        arrayValue.Should().NotBeNull();
        arrayValue.Should().BeEquivalentTo(stringArray);
    }

    [Fact]
    public void TryGetStringArray_With_JsonElementNonArray_Should_ReturnFalse()
    {
        // Arrange
        var json = JsonDocument.Parse("\"not-an-array\"");
        var typedValue = TypedValue.From(json.RootElement);

        // Act
        var result = typedValue.TryGetStringArray(out var arrayValue);

        // Assert
        result.Should().BeFalse();
        arrayValue.Should().BeEmpty();
    }

    [Fact]
    public void TryGetStringArray_With_NullType_Should_ReturnFalse()
    {
        // Arrange
        var typedValue = TypedValue.From(null);

        // Act
        var result = typedValue.TryGetStringArray(out var arrayValue);

        // Assert
        result.Should().BeFalse();
        arrayValue.Should().BeEmpty();
    }

    [Fact]
    public void TryGetStringArray_With_InvalidType_Should_ReturnFalse()
    {
        // Arrange
        var typedValue = TypedValue.From("not-an-array");

        // Act
        var result = typedValue.TryGetStringArray(out var arrayValue);

        // Assert
        result.Should().BeFalse();
        arrayValue.Should().BeEmpty();
    }

    [Fact]
    public void TryGetStringArray_With_JsonElementEmptyArray_Should_ReturnTrue()
    {
        // Arrange
        var json = JsonDocument.Parse("[]");
        var typedValue = TypedValue.From(json.RootElement);

        // Act
        var result = typedValue.TryGetStringArray(out var arrayValue);

        // Assert
        result.Should().BeTrue();
        arrayValue.Should().NotBeNull();
        arrayValue.Should().BeEmpty();
    }
}

