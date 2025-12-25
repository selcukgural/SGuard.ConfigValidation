using FluentAssertions;
using SGuard.ConfigValidation.Common;

namespace SGuard.ConfigValidation.Tests;

public sealed class ValueConversionHelperTests
{
    [Fact]
    public void CompareValues_With_IntegerValues_Should_Compare_Correctly()
    {
        // Arrange
        var value1 = 10;
        var value2 = 20;

        // Act
        var result = ValueConversionHelper.CompareValues(value1, value2);

        // Assert
        result.Should().BeNegative("10 should be less than 20");
    }

    [Fact]
    public void CompareValues_With_EqualIntegerValues_Should_Return_Zero()
    {
        // Arrange
        var value1 = 10;
        var value2 = 10;

        // Act
        var result = ValueConversionHelper.CompareValues(value1, value2);

        // Assert
        result.Should().Be(0, "Equal values should return 0");
    }

    [Fact]
    public void CompareValues_With_DoubleValues_Should_Compare_Correctly()
    {
        // Arrange
        var value1 = 10.5;
        var value2 = 20.3;

        // Act
        var result = ValueConversionHelper.CompareValues(value1, value2);

        // Assert
        result.Should().BeNegative("10.5 should be less than 20.3");
    }

    [Fact]
    public void CompareValues_With_StringNumericValues_Should_Compare_Correctly()
    {
        // Arrange
        var value1 = "10";
        var value2 = "20";

        // Act
        var result = ValueConversionHelper.CompareValues(value1, value2);

        // Assert
        result.Should().BeNegative("'10' should be less than '20' when parsed as numbers");
    }

    [Fact]
    public void CompareValues_With_MixedNumericTypes_Should_Compare_Correctly()
    {
        // Arrange
        var value1 = 10; // int
        var value2 = 20.5; // double

        // Act
        var result = ValueConversionHelper.CompareValues(value1, value2);

        // Assert
        result.Should().BeNegative("10 should be less than 20.5");
    }

    [Fact]
    public void CompareValues_With_StringValues_Should_Compare_UsingIComparable()
    {
        // Arrange
        var value1 = "apple";
        var value2 = "banana";

        // Act
        var result = ValueConversionHelper.CompareValues(value1, value2);

        // Assert
        result.Should().BeNegative("'apple' should be less than 'banana' alphabetically");
    }

    [Fact]
    public void CompareValues_With_NullConditionValue_Should_Throw_ArgumentException()
    {
        // Arrange
        var value1 = 10;
        object? value2 = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => ValueConversionHelper.CompareValues(value1, value2));
        exception.Message.ToLowerInvariant().Should().Contain("cannot be null");
    }

    [Fact]
    public void CompareValues_With_NonComparableValues_Should_Throw_ArgumentException()
    {
        // Arrange
        var value1 = new { Name = "test" };
        var value2 = new { Name = "test" };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => ValueConversionHelper.CompareValues(value1, value2));
        exception.Message.ToLowerInvariant().Should().Contain("not comparable");
    }

    [Fact]
    public void CompareValues_With_DecimalValues_Should_Compare_Correctly()
    {
        // Arrange
        var value1 = 10.5m;
        var value2 = 20.3m;

        // Act
        var result = ValueConversionHelper.CompareValues(value1, value2);

        // Assert
        result.Should().BeNegative("10.5m should be less than 20.3m");
    }

    [Fact]
    public void CompareValues_With_FloatValues_Should_Compare_Correctly()
    {
        // Arrange
        var value1 = 10.5f;
        var value2 = 20.3f;

        // Act
        var result = ValueConversionHelper.CompareValues(value1, value2);

        // Assert
        result.Should().BeNegative("10.5f should be less than 20.3f");
    }

    [Fact]
    public void CompareValues_With_LongValues_Should_Compare_Correctly()
    {
        // Arrange
        var value1 = 10L;
        var value2 = 20L;

        // Act
        var result = ValueConversionHelper.CompareValues(value1, value2);

        // Assert
        result.Should().BeNegative("10L should be less than 20L");
    }

    [Fact]
    public void CompareValues_With_ByteValues_Should_Compare_Correctly()
    {
        // Arrange
        byte value1 = 10;
        byte value2 = 20;

        // Act
        var result = ValueConversionHelper.CompareValues(value1, value2);

        // Assert
        result.Should().BeNegative("10 should be less than 20");
    }

    [Fact]
    public void CompareValues_With_ShortValues_Should_Compare_Correctly()
    {
        // Arrange
        short value1 = 10;
        short value2 = 20;

        // Act
        var result = ValueConversionHelper.CompareValues(value1, value2);

        // Assert
        result.Should().BeNegative("10 should be less than 20");
    }
}

