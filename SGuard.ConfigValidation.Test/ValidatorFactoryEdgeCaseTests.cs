using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SGuard.ConfigValidation.Validators;
using SGuard.ConfigValidation.Validators.Plugin;

namespace SGuard.ConfigValidation.Test;

public sealed class ValidatorFactoryEdgeCaseTests
{
    [Fact]
    public void Constructor_With_NullLogger_Should_Throw_ArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ValidatorFactory(null!));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_With_PluginDiscovery_And_PluginDirectories_Should_Discover_Plugins()
    {
        // Arrange
        var logger = NullLogger<ValidatorFactory>.Instance;
        var pluginDiscoveryLogger = NullLogger<ValidatorPluginDiscovery>.Instance;
        var pluginDiscovery = new ValidatorPluginDiscovery(pluginDiscoveryLogger);
        var pluginDirectories = new[] { "/nonexistent/path" }; // Empty directory, no plugins

        // Act
        var factory = new ValidatorFactory(logger, pluginDiscovery, pluginDirectories);

        // Assert
        factory.Should().NotBeNull();
        var supportedValidators = factory.GetSupportedValidators().ToList();
        supportedValidators.Should().HaveCount(10); // Built-in validators only
    }

    [Fact]
    public void GetValidator_With_NullValidatorType_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<ValidatorFactory>.Instance;
        var factory = new ValidatorFactory(logger);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            factory.GetValidator(null!));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public void GetValidator_With_EmptyValidatorType_Should_Throw_NotSupportedException()
    {
        // Arrange
        var logger = NullLogger<ValidatorFactory>.Instance;
        var factory = new ValidatorFactory(logger);

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
            factory.GetValidator(""));
        
        exception.Should().NotBeNull();
        exception.Message.Should().Contain("Supported validators:");
    }

    [Fact]
    public void GetValidator_With_CaseInsensitiveType_Should_Return_Validator()
    {
        // Arrange
        var logger = NullLogger<ValidatorFactory>.Instance;
        var factory = new ValidatorFactory(logger);

        // Act
        var validator1 = factory.GetValidator("REQUIRED");
        var validator2 = factory.GetValidator("required");
        var validator3 = factory.GetValidator("Required");

        // Assert
        validator1.Should().NotBeNull();
        validator2.Should().NotBeNull();
        validator3.Should().NotBeNull();
        validator1.ValidatorType.Should().Be("required");
        validator2.ValidatorType.Should().Be("required");
        validator3.ValidatorType.Should().Be("required");
    }

    [Fact]
    public void GetSupportedValidators_Should_Return_CaseInsensitive_Keys()
    {
        // Arrange
        var logger = NullLogger<ValidatorFactory>.Instance;
        var factory = new ValidatorFactory(logger);

        // Act
        var supportedValidators = factory.GetSupportedValidators().ToList();

        // Assert
        supportedValidators.Should().NotBeNull();
        supportedValidators.Should().HaveCount(10);
        supportedValidators.Should().Contain("required");
        supportedValidators.Should().Contain("eq");
        supportedValidators.Should().Contain("gt");
    }

    [Fact]
    public void GetValidator_With_AllBuiltInTypes_Should_Return_Validators()
    {
        // Arrange
        var logger = NullLogger<ValidatorFactory>.Instance;
        var factory = new ValidatorFactory(logger);
        var builtInTypes = new[]
        {
            "required", "min_len", "max_len", "eq", "ne",
            "gt", "gte", "lt", "lte", "in"
        };

        // Act & Assert
        foreach (var validatorType in builtInTypes)
        {
            var validator = factory.GetValidator(validatorType);
            validator.Should().NotBeNull();
            validator.ValidatorType.Should().Be(validatorType);
        }
    }

    [Fact]
    public void Constructor_With_PluginDiscovery_But_NullPluginDirectories_Should_NotDiscover()
    {
        // Arrange
        var logger = NullLogger<ValidatorFactory>.Instance;
        var pluginDiscoveryLogger = NullLogger<ValidatorPluginDiscovery>.Instance;
        var pluginDiscovery = new ValidatorPluginDiscovery(pluginDiscoveryLogger);

        // Act
        var factory = new ValidatorFactory(logger, pluginDiscovery, null);

        // Assert
        factory.Should().NotBeNull();
        var supportedValidators = factory.GetSupportedValidators().ToList();
        supportedValidators.Should().HaveCount(10); // Only built-in validators
    }

    [Fact]
    public void Constructor_With_NullPluginDiscovery_But_PluginDirectories_Should_NotDiscover()
    {
        // Arrange
        var logger = NullLogger<ValidatorFactory>.Instance;
        var pluginDirectories = new[] { "/some/path" };

        // Act
        var factory = new ValidatorFactory(logger, null, pluginDirectories);

        // Assert
        factory.Should().NotBeNull();
        var supportedValidators = factory.GetSupportedValidators().ToList();
        supportedValidators.Should().HaveCount(10); // Only built-in validators
    }
}

