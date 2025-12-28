using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SGuard.ConfigChecker.Console;
using SGuard.ConfigValidation.Security;

namespace SGuard.ConfigValidation.Tests;

public sealed class SGuardCliInputSanitizationTests
{
    private readonly ILogger<SGuardCli> _logger;

    public SGuardCliInputSanitizationTests()
    {
        _logger = NullLogger<SGuardCli>.Instance;
    }

    [Fact]
    public void ValidateAndSanitizePath_With_PathTraversal_Should_ReturnInvalid()
    {
        // Arrange
        var maliciousPath = "../../../etc/passwd";

        // Act
        var result = ValidateAndSanitizePath(maliciousPath, "test", _logger);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Path traversal");
    }

    [Fact]
    public void ValidateAndSanitizePath_With_DoubleSlashes_Should_ReturnInvalid()
    {
        // Arrange - Double slashes are detected as path traversal
        var pathWithDoubleSlashes = "path//to//file.json";

        // Act
        var result = ValidateAndSanitizePath(pathWithDoubleSlashes, "test", _logger);

        // Assert - Double slashes trigger path traversal detection
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Path traversal");
    }

    [Fact]
    public void ValidateAndSanitizePath_With_NullByte_Should_ReturnInvalid()
    {
        // Arrange
        var maliciousPath = "path\0to\0file.json";

        // Act
        var result = ValidateAndSanitizePath(maliciousPath, "test", _logger);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Dangerous characters");
    }

    [Fact]
    public void ValidateAndSanitizePath_With_ControlCharacters_Should_ReturnInvalid()
    {
        // Arrange - Use actual control characters (not \r\n which might be allowed)
        var maliciousPath = "path" + (char)1 + "to" + (char)2 + "file.json";

        // Act
        var result = ValidateAndSanitizePath(maliciousPath, "test", _logger);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Dangerous characters");
    }

    [Fact]
    public void ValidateAndSanitizePath_With_ExcessiveLength_Should_ReturnInvalid()
    {
        // Arrange
        var longPath = new string('a', SecurityConstants.MaxPathLengthHardLimit + 1);

        // Act
        var result = ValidateAndSanitizePath(longPath, "test", _logger);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("exceeds maximum limit");
    }

    [Fact]
    public void ValidateAndSanitizePath_With_ValidPath_Should_ReturnValid()
    {
        // Arrange
        var validPath = "appsettings.json";

        // Act
        var result = ValidateAndSanitizePath(validPath, "test", _logger);

        // Assert
        result.IsValid.Should().BeTrue();
        result.SanitizedPath.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ValidateAndSanitizeEnvironmentId_With_ControlCharacters_Should_ReturnInvalid()
    {
        // Arrange - Use actual control characters (not \r\n which might be allowed)
        var maliciousId = "env" + (char)1 + "id" + (char)2;

        // Act
        var result = ValidateAndSanitizeEnvironmentId(maliciousId, _logger);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Environment ID contains dangerous characters");
    }

    [Fact]
    public void ValidateAndSanitizeEnvironmentId_With_NullByte_Should_ReturnInvalid()
    {
        // Arrange
        var maliciousId = "env\0id";

        // Act
        var result = ValidateAndSanitizeEnvironmentId(maliciousId, _logger);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Environment ID contains dangerous characters");
    }

    [Fact]
    public void ValidateAndSanitizeEnvironmentId_With_ExcessiveLength_Should_ReturnInvalid()
    {
        // Arrange
        var longId = new string('a', 257);

        // Act
        var result = ValidateAndSanitizeEnvironmentId(longId, _logger);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("exceeds maximum limit");
    }

    [Fact]
    public void ValidateAndSanitizeEnvironmentId_With_ValidId_Should_ReturnValid()
    {
        // Arrange
        var validId = "dev-environment";

        // Act
        var result = ValidateAndSanitizeEnvironmentId(validId, _logger);

        // Assert
        result.IsValid.Should().BeTrue();
        result.SanitizedId.Should().Be(validId);
    }

    [Fact]
    public void ValidateAndSanitizeEnvironmentId_With_UnusualCharacters_Should_Sanitize()
    {
        // Arrange
        var idWithUnusualChars = "dev@environment#123";

        // Act
        var result = ValidateAndSanitizeEnvironmentId(idWithUnusualChars, _logger);

        // Assert
        result.IsValid.Should().BeTrue();
        result.SanitizedId.Should().NotContain("@");
        result.SanitizedId.Should().NotContain("#");
    }

    // Helper methods to access private static methods via reflection
    private static (bool IsValid, string SanitizedPath, string ErrorMessage) ValidateAndSanitizePath(string path, string parameterName, ILogger<SGuardCli> logger)
    {
        var method = typeof(SGuardCli).GetMethod("ValidateAndSanitizePath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (method == null)
        {
            throw new InvalidOperationException("ValidateAndSanitizePath method not found");
        }
        var result = method.Invoke(null, [path, parameterName, logger]);
        if (result == null)
        {
            throw new InvalidOperationException("Method returned null");
        }
        return ((bool IsValid, string SanitizedPath, string ErrorMessage))result;
    }

    private static (bool IsValid, string SanitizedId, string ErrorMessage) ValidateAndSanitizeEnvironmentId(string environmentId, ILogger<SGuardCli> logger)
    {
        var method = typeof(SGuardCli).GetMethod("ValidateAndSanitizeEnvironmentId", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (method == null)
        {
            throw new InvalidOperationException("ValidateAndSanitizeEnvironmentId method not found");
        }
        var result = method.Invoke(null, [environmentId, logger]);
        if (result == null)
        {
            throw new InvalidOperationException("Method returned null");
        }
        return ((bool IsValid, string SanitizedId, string ErrorMessage))result;
    }
}

