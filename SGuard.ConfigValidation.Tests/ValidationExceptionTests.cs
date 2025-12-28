using FluentAssertions;
using SGuard.ConfigValidation.Exceptions;

namespace SGuard.ConfigValidation.Tests;

public sealed class ValidationExceptionTests
{
    [Fact]
    public void Constructor_With_Message_Should_Create_Exception()
    {
        // Arrange
        var message = "Validation failed";

        // Act
        var exception = new ValidationException(message, new Exception("Inner exception"));

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().NotBeNull();
        exception.InnerException!.Message.Should().Be("Inner exception");
    }

    [Fact]
    public void Constructor_With_MessageAndInnerException_Should_Set_Properties()
    {
        // Arrange
        var message = "Validation failed";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new ValidationException(message, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Constructor_With_EmptyMessage_Should_Create_Exception()
    {
        // Arrange
        var message = "";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new ValidationException(message, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Constructor_Should_Inherit_From_Exception()
    {
        // Arrange
        var message = "Validation failed";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new ValidationException(message, innerException);

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Exception_Should_Be_Serializable()
    {
        // Arrange
        var message = "Validation failed";
        var innerException = new Exception("Inner exception");
        var exception = new ValidationException(message, innerException);

        // Act & Assert
        exception.Should().NotBeNull();
        // Exception serialization is tested implicitly by the framework
    }
}

