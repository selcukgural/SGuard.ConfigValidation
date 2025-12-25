using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SGuard.ConfigValidation.Common;

namespace SGuard.ConfigValidation.Tests;

public sealed class SecurityOptionsHardLimitTests
{
    [Fact]
    public void SecurityOptions_ValidateAndClamp_With_ValuesExceedingHardLimits_Should_ClampToHardLimits()
    {
        // Arrange
        var options = new SecurityOptions
        {
            MaxFileSizeBytes = SecurityConstants.MaxFileSizeBytesHardLimit + 1000000, // Exceeds hard limit
            MaxEnvironmentsCount = SecurityConstants.MaxEnvironmentsCountHardLimit + 1000,
            MaxRulesCount = SecurityConstants.MaxRulesCountHardLimit + 10000,
            MaxConditionsPerRule = SecurityConstants.MaxConditionsPerRuleHardLimit + 1000,
            MaxValidatorsPerCondition = SecurityConstants.MaxValidatorsPerConditionHardLimit + 100,
            MaxPathCacheSize = SecurityConstants.MaxPathCacheSizeHardLimit + 10000,
            MaxPathLength = SecurityConstants.MaxPathLengthHardLimit + 1000,
            MaxJsonDepth = SecurityConstants.MaxJsonDepthHardLimit + 100
        };
        var logger = NullLogger<SecurityOptions>.Instance;

        // Act
        var clamped = options.ValidateAndClamp(logger);

        // Assert
        clamped.Should().BeTrue();
        options.MaxFileSizeBytes.Should().Be(SecurityConstants.MaxFileSizeBytesHardLimit);
        options.MaxEnvironmentsCount.Should().Be(SecurityConstants.MaxEnvironmentsCountHardLimit);
        options.MaxRulesCount.Should().Be(SecurityConstants.MaxRulesCountHardLimit);
        options.MaxConditionsPerRule.Should().Be(SecurityConstants.MaxConditionsPerRuleHardLimit);
        options.MaxValidatorsPerCondition.Should().Be(SecurityConstants.MaxValidatorsPerConditionHardLimit);
        options.MaxPathCacheSize.Should().Be(SecurityConstants.MaxPathCacheSizeHardLimit);
        options.MaxPathLength.Should().Be(SecurityConstants.MaxPathLengthHardLimit);
        options.MaxJsonDepth.Should().Be(SecurityConstants.MaxJsonDepthHardLimit);
    }

    [Fact]
    public void SecurityOptions_ValidateAndClamp_With_ValuesWithinHardLimits_Should_NotClamp()
    {
        // Arrange
        var options = new SecurityOptions
        {
            MaxFileSizeBytes = SecurityConstants.MaxFileSizeBytesHardLimit - 1000000,
            MaxEnvironmentsCount = SecurityConstants.MaxEnvironmentsCountHardLimit - 100,
            MaxRulesCount = SecurityConstants.MaxRulesCountHardLimit - 1000
        };
        var logger = NullLogger<SecurityOptions>.Instance;

        // Act
        var clamped = options.ValidateAndClamp(logger);

        // Assert
        clamped.Should().BeFalse();
        options.MaxFileSizeBytes.Should().Be(SecurityConstants.MaxFileSizeBytesHardLimit - 1000000);
        options.MaxEnvironmentsCount.Should().Be(SecurityConstants.MaxEnvironmentsCountHardLimit - 100);
        options.MaxRulesCount.Should().Be(SecurityConstants.MaxRulesCountHardLimit - 1000);
    }

    [Fact]
    public void SecurityOptions_ValidateAndClamp_With_ValuesAtHardLimits_Should_NotClamp()
    {
        // Arrange
        var options = new SecurityOptions
        {
            MaxFileSizeBytes = SecurityConstants.MaxFileSizeBytesHardLimit,
            MaxEnvironmentsCount = SecurityConstants.MaxEnvironmentsCountHardLimit,
            MaxRulesCount = SecurityConstants.MaxRulesCountHardLimit
        };
        var logger = NullLogger<SecurityOptions>.Instance;

        // Act
        var clamped = options.ValidateAndClamp(logger);

        // Assert
        clamped.Should().BeFalse();
        options.MaxFileSizeBytes.Should().Be(SecurityConstants.MaxFileSizeBytesHardLimit);
        options.MaxEnvironmentsCount.Should().Be(SecurityConstants.MaxEnvironmentsCountHardLimit);
        options.MaxRulesCount.Should().Be(SecurityConstants.MaxRulesCountHardLimit);
    }

    [Fact]
    public void SecurityOptions_ValidateAndClamp_With_NullLogger_Should_StillClamp()
    {
        // Arrange
        var options = new SecurityOptions
        {
            MaxFileSizeBytes = SecurityConstants.MaxFileSizeBytesHardLimit + 1000000
        };

        // Act
        var clamped = options.ValidateAndClamp(null);

        // Assert
        clamped.Should().BeTrue();
        options.MaxFileSizeBytes.Should().Be(SecurityConstants.MaxFileSizeBytesHardLimit);
    }

    [Fact]
    public void SecurityOptions_FromConfiguration_ExceedingHardLimits_Should_BeClamped()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Security:MaxFileSizeBytes", (SecurityConstants.MaxFileSizeBytesHardLimit + 1000000).ToString() },
                { "Security:MaxEnvironmentsCount", (SecurityConstants.MaxEnvironmentsCountHardLimit + 1000).ToString() },
                { "Security:MaxRulesCount", (SecurityConstants.MaxRulesCountHardLimit + 10000).ToString() }
            })
            .Build();

        // Act
        var options = new SecurityOptions();
        configuration.GetSection("Security").Bind(options);
        options.ValidateAndClamp(NullLogger<SecurityOptions>.Instance);

        // Assert
        options.MaxFileSizeBytes.Should().Be(SecurityConstants.MaxFileSizeBytesHardLimit);
        options.MaxEnvironmentsCount.Should().Be(SecurityConstants.MaxEnvironmentsCountHardLimit);
        options.MaxRulesCount.Should().Be(SecurityConstants.MaxRulesCountHardLimit);
    }
}

