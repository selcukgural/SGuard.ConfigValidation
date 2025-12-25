using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Tests;

public sealed class ValidatorFactoryTests
{
    private readonly ValidatorFactory _factory;

    public ValidatorFactoryTests()
    {
        var logger = NullLogger<ValidatorFactory>.Instance;
        _factory = new ValidatorFactory(logger);
    }

    [Fact]
    public void GetValidator_With_SupportedType_Should_Return_Validator()
    {
        // Arrange
        var supportedTypes = ValidatorConstants.AllValidatorTypes;

        foreach (var validatorType in supportedTypes)
        {
            // Act
            var validator = _factory.GetValidator(validatorType);

            // Assert
            validator.Should().NotBeNull();
            validator.ValidatorType.Should().Be(validatorType);
        }
    }

    [Fact]
    public void GetValidator_With_UnsupportedType_Should_Throw_Exception()
    {
        // Arrange
        var unsupportedType = "unsupported_validator";

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() => _factory.GetValidator(unsupportedType));
        exception.Message.Should().Contain("unsupported_validator");
        exception.Message.Should().Contain("Supported validators:");
    }

    [Fact]
    public void GetSupportedValidators_Should_Return_All_Supported_Types()
    {
        // Act
        var supportedValidators = _factory.GetSupportedValidators().ToList();

        // Assert
        supportedValidators.Should().HaveCount(10);
        supportedValidators.Should().BeEquivalentTo(ValidatorConstants.AllValidatorTypes);
    }
}