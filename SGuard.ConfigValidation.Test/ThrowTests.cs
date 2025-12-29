using FluentAssertions;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Exceptions;

namespace SGuard.ConfigValidation.Test;

public sealed class ThrowTests
{
    [Fact]
    public void ArgumentNullException_With_ResourceKey_Should_Create_Exception()
    {
        // Act
        var exception = Throw.ArgumentNullException("TestResourceKey", "paramName");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<ArgumentNullException>();
        exception.ParamName.Should().Be("paramName");
    }

    [Fact]
    public void ArgumentNullException_With_ResourceKey_And_Args_Should_Create_Exception()
    {
        // Act
        var exception = Throw.ArgumentNullException("TestResourceKey", "paramName", "arg1", "arg2");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<ArgumentNullException>();
        exception.ParamName.Should().Be("paramName");
    }

    [Fact]
    public void ArgumentException_With_ResourceKey_Should_Create_Exception()
    {
        // Act
        var exception = Throw.ArgumentException("TestResourceKey", "paramName");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<ArgumentException>();
        exception.ParamName.Should().Be("paramName");
    }

    [Fact]
    public void ArgumentException_With_ResourceKey_And_Args_Should_Create_Exception()
    {
        // Act
        var exception = Throw.ArgumentException("TestResourceKey", "paramName", "arg1", "arg2");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<ArgumentException>();
        exception.ParamName.Should().Be("paramName");
    }

    [Fact]
    public void ConfigurationException_With_ResourceKey_Should_Create_Exception()
    {
        // Act
        var exception = Throw.ConfigurationException("TestResourceKey");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public void ConfigurationException_With_ResourceKey_And_Args_Should_Create_Exception()
    {
        // Act
        var exception = Throw.ConfigurationException("TestResourceKey", "arg1", "arg2");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public void ConfigurationException_With_InnerException_Should_Create_Exception()
    {
        // Arrange
        var innerException = new Exception("Inner exception");

        // Act
        var exception = Throw.ConfigurationException("TestResourceKey", innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<ConfigurationException>();
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void ConfigurationException_With_InnerException_And_Args_Should_Create_Exception()
    {
        // Arrange
        var innerException = new Exception("Inner exception");

        // Act
        var exception = Throw.ConfigurationException("TestResourceKey", innerException, "arg1", "arg2");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<ConfigurationException>();
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void FileNotFoundException_With_FileName_And_ResourceKey_Should_Create_Exception()
    {
        // Act
        var exception = Throw.FileNotFoundException("test.txt", "TestResourceKey");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<FileNotFoundException>();
        exception.FileName.Should().Be("test.txt");
    }

    [Fact]
    public void FileNotFoundException_With_FileName_ResourceKey_And_Args_Should_Create_Exception()
    {
        // Act
        var exception = Throw.FileNotFoundException("test.txt", "TestResourceKey", "arg1", "arg2");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<FileNotFoundException>();
        exception.FileName.Should().Be("test.txt");
    }

    [Fact]
    public void InvalidOperationException_With_ResourceKey_Should_Create_Exception()
    {
        // Act
        var exception = Throw.InvalidOperationException("TestResourceKey");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void InvalidOperationException_With_ResourceKey_And_Args_Should_Create_Exception()
    {
        // Act
        var exception = Throw.InvalidOperationException("TestResourceKey", "arg1", "arg2");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void InvalidOperationException_With_InnerException_Should_Create_Exception()
    {
        // Arrange
        var innerException = new Exception("Inner exception");

        // Act
        var exception = Throw.InvalidOperationException("TestResourceKey", innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<InvalidOperationException>();
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void InvalidOperationException_With_InnerException_And_Args_Should_Create_Exception()
    {
        // Arrange
        var innerException = new Exception("Inner exception");

        // Act
        var exception = Throw.InvalidOperationException("TestResourceKey", innerException, "arg1", "arg2");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<InvalidOperationException>();
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void UnauthorizedAccessException_With_ResourceKey_Should_Create_Exception()
    {
        // Act
        var exception = Throw.UnauthorizedAccessException("TestResourceKey");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<UnauthorizedAccessException>();
    }

    [Fact]
    public void UnauthorizedAccessException_With_ResourceKey_And_Args_Should_Create_Exception()
    {
        // Act
        var exception = Throw.UnauthorizedAccessException("TestResourceKey", "arg1", "arg2");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<UnauthorizedAccessException>();
    }

    [Fact]
    public void UnauthorizedAccessException_With_InnerException_Should_Create_Exception()
    {
        // Arrange
        var innerException = new Exception("Inner exception");

        // Act
        var exception = Throw.UnauthorizedAccessException("TestResourceKey", innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<UnauthorizedAccessException>();
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void UnauthorizedAccessException_With_InnerException_And_Args_Should_Create_Exception()
    {
        // Arrange
        var innerException = new Exception("Inner exception");

        // Act
        var exception = Throw.UnauthorizedAccessException("TestResourceKey", innerException, "arg1", "arg2");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<UnauthorizedAccessException>();
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void NotSupportedException_With_ResourceKey_Should_Create_Exception()
    {
        // Act
        var exception = Throw.NotSupportedException("TestResourceKey");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<NotSupportedException>();
    }

    [Fact]
    public void NotSupportedException_With_ResourceKey_And_Args_Should_Create_Exception()
    {
        // Act
        var exception = Throw.NotSupportedException("TestResourceKey", "arg1", "arg2");

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<NotSupportedException>();
    }

    [Fact]
    public void IsCriticalException_With_OutOfMemoryException_Should_ReturnTrue()
    {
        // Arrange
        var exception = new OutOfMemoryException();

        // Act
        var result = Throw.IsCriticalException(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCriticalException_With_StackOverflowException_Should_ReturnTrue()
    {
        // Arrange
        var exception = new StackOverflowException();

        // Act
        var result = Throw.IsCriticalException(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCriticalException_With_AccessViolationException_Should_ReturnTrue()
    {
        // Arrange
        var exception = new AccessViolationException();

        // Act
        var result = Throw.IsCriticalException(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCriticalException_With_BadImageFormatException_Should_ReturnTrue()
    {
        // Arrange
        var exception = new BadImageFormatException();

        // Act
        var result = Throw.IsCriticalException(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCriticalException_With_InvalidProgramException_Should_ReturnTrue()
    {
        // Arrange
        var exception = new InvalidProgramException();

        // Act
        var result = Throw.IsCriticalException(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCriticalException_With_NormalException_Should_ReturnFalse()
    {
        // Arrange
        var exception = new Exception("Normal exception");

        // Act
        var result = Throw.IsCriticalException(exception);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCriticalException_With_ArgumentException_Should_ReturnFalse()
    {
        // Arrange
        var exception = new ArgumentException("Argument exception");

        // Act
        var result = Throw.IsCriticalException(exception);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCriticalException_With_Null_Should_ReturnFalse()
    {
        // Act
        var result = Throw.IsCriticalException(null!);

        // Assert
        result.Should().BeFalse();
    }
}

