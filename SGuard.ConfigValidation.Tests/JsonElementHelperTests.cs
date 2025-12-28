using System.Text.Json;
using FluentAssertions;
using SGuard.ConfigValidation.Utils;

namespace SGuard.ConfigValidation.Tests;

public sealed class JsonElementHelperTests
{
    [Fact]
    public void GetInt32_With_JsonElementNumber_Should_Return_Int32()
    {
        // Arrange
        var json = JsonDocument.Parse("42");
        var element = json.RootElement;

        // Act
        var result = JsonElementHelper.GetInt32(element, "test");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void GetInt32_With_JsonElementString_ValidInt_Should_Return_Int32()
    {
        // Arrange
        var json = JsonDocument.Parse("\"123\"");
        var element = json.RootElement;

        // Act
        var result = JsonElementHelper.GetInt32(element, "test");

        // Assert
        result.Should().Be(123);
    }

    [Fact]
    public void GetInt32_With_JsonElementString_InvalidInt_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var json = JsonDocument.Parse("\"not-a-number\"");
        var element = json.RootElement;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            JsonElementHelper.GetInt32(element, "test"));
        
        exception.Message.Should().Contain("not-a-number");
        exception.Message.Should().Contain("test");
    }

    [Fact]
    public void GetInt32_With_IntValue_Should_Return_Int32()
    {
        // Arrange
        int value = 456;

        // Act
        var result = JsonElementHelper.GetInt32(value, "test");

        // Assert
        result.Should().Be(456);
    }

    [Fact]
    public void GetInt32_With_StringValue_ValidInt_Should_Return_Int32()
    {
        // Arrange
        string value = "789";

        // Act
        var result = JsonElementHelper.GetInt32(value, "test");

        // Assert
        result.Should().Be(789);
    }

    [Fact]
    public void GetInt32_With_StringValue_InvalidInt_Should_Throw_InvalidOperationException()
    {
        // Arrange
        string value = "invalid";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            JsonElementHelper.GetInt32(value, "test"));
        
        exception.Message.Should().Contain("invalid");
        exception.Message.Should().Contain("test");
    }

    [Fact]
    public void GetInt32_With_Null_Should_Throw_InvalidOperationException()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            JsonElementHelper.GetInt32(null, "test"));
        
        exception.Message.Should().Contain("null");
        exception.Message.Should().Contain("test");
    }

    [Fact]
    public void GetInt32_With_InvalidType_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var value = new object();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            JsonElementHelper.GetInt32(value, "test"));
        
        exception.Message.Should().Contain("test");
    }

    [Fact]
    public void GetInt32_With_JsonElementBoolean_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var json = JsonDocument.Parse("true");
        var element = json.RootElement;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            JsonElementHelper.GetInt32(element, "test"));
        
        exception.Message.Should().Contain("test");
    }

    [Fact]
    public void GetInt32_With_JsonElementArray_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var json = JsonDocument.Parse("[1, 2, 3]");
        var element = json.RootElement;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            JsonElementHelper.GetInt32(element, "test"));
        
        exception.Message.Should().Contain("test");
    }

    [Fact]
    public void GetInt32_With_JsonElementObject_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var json = JsonDocument.Parse("{\"key\": \"value\"}");
        var element = json.RootElement;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            JsonElementHelper.GetInt32(element, "test"));
        
        exception.Message.Should().Contain("test");
    }

    [Fact]
    public void GetInt32_With_JsonElementNull_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var json = JsonDocument.Parse("null");
        var element = json.RootElement;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            JsonElementHelper.GetInt32(element, "test"));
        
        exception.Message.Should().Contain("test");
        exception.Message.Should().Contain("Cannot convert");
    }

    [Fact]
    public void GetInt32_With_NegativeInt_Should_Return_NegativeInt32()
    {
        // Arrange
        var json = JsonDocument.Parse("-42");
        var element = json.RootElement;

        // Act
        var result = JsonElementHelper.GetInt32(element, "test");

        // Assert
        result.Should().Be(-42);
    }

    [Fact]
    public void GetInt32_With_Zero_Should_Return_Zero()
    {
        // Arrange
        var json = JsonDocument.Parse("0");
        var element = json.RootElement;

        // Act
        var result = JsonElementHelper.GetInt32(element, "test");

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void GetInt32_With_StringNegativeInt_Should_Return_NegativeInt32()
    {
        // Arrange
        string value = "-123";

        // Act
        var result = JsonElementHelper.GetInt32(value, "test");

        // Assert
        result.Should().Be(-123);
    }
}

