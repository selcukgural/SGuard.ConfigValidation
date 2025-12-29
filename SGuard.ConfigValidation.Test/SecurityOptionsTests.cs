using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SGuard.ConfigValidation.Security;

namespace SGuard.ConfigValidation.Test;

public sealed class SecurityOptionsTests
{
    [Fact]
    public void SecurityOptions_With_DefaultValues_Should_Use_SecurityConstants()
    {
        // Arrange & Act
        var options = new SecurityOptions();

        // Assert
        options.MaxFileSizeBytes.Should().Be(SecurityConstants.MaxFileSizeBytes);
        options.MaxEnvironmentsCount.Should().Be(SecurityConstants.MaxEnvironmentsCount);
        options.MaxRulesCount.Should().Be(SecurityConstants.MaxRulesCount);
        options.MaxConditionsPerRule.Should().Be(SecurityConstants.MaxConditionsPerRule);
        options.MaxValidatorsPerCondition.Should().Be(SecurityConstants.MaxValidatorsPerCondition);
        options.MaxPathCacheSize.Should().Be(SecurityConstants.MaxPathCacheSize);
        options.MaxPathLength.Should().Be(SecurityConstants.MaxPathLength);
        options.MaxJsonDepth.Should().Be(SecurityConstants.MaxJsonDepth);
    }

    [Fact]
    public void SecurityOptions_Can_Be_Configured()
    {
        // Arrange
        var options = new SecurityOptions
        {
            MaxFileSizeBytes = 200 * 1024 * 1024, // 200 MB
            MaxEnvironmentsCount = 2000,
            MaxRulesCount = 20000
        };

        // Act & Assert
        options.MaxFileSizeBytes.Should().Be(200 * 1024 * 1024);
        options.MaxEnvironmentsCount.Should().Be(2000);
        options.MaxRulesCount.Should().Be(20000);
    }

    [Fact]
    public void SecurityOptions_Can_Be_Bound_From_Configuration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Security:MaxFileSizeBytes", "209715200" }, // 200 MB
                { "Security:MaxEnvironmentsCount", "2000" },
                { "Security:MaxRulesCount", "20000" },
                { "Security:MaxConditionsPerRule", "2000" },
                { "Security:MaxValidatorsPerCondition", "200" },
                { "Security:MaxPathCacheSize", "20000" },
                { "Security:MaxPathLength", "8192" },
                { "Security:MaxJsonDepth", "128" }
            })
            .Build();

        // Act
        var options = new SecurityOptions();
        configuration.GetSection("Security").Bind(options);

        // Assert
        options.MaxFileSizeBytes.Should().Be(209715200);
        options.MaxEnvironmentsCount.Should().Be(2000);
        options.MaxRulesCount.Should().Be(20000);
        options.MaxConditionsPerRule.Should().Be(2000);
        options.MaxValidatorsPerCondition.Should().Be(200);
        options.MaxPathCacheSize.Should().Be(20000);
        options.MaxPathLength.Should().Be(8192);
        options.MaxJsonDepth.Should().Be(128);
    }

    [Fact]
    public void SecurityOptions_With_PartialConfiguration_Should_Use_Defaults_For_Missing_Values()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Security:MaxFileSizeBytes", "209715200" } // Only set one value
            })
            .Build();

        // Act
        var options = new SecurityOptions();
        configuration.GetSection("Security").Bind(options);

        // Assert
        options.MaxFileSizeBytes.Should().Be(209715200); // Overridden
        options.MaxEnvironmentsCount.Should().Be(SecurityConstants.MaxEnvironmentsCount); // Default
        options.MaxRulesCount.Should().Be(SecurityConstants.MaxRulesCount); // Default
    }
}

