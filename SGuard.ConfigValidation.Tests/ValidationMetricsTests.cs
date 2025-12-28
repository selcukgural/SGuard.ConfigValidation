using System.Diagnostics.Metrics;
using FluentAssertions;
using SGuard.ConfigValidation.Telemetry;

namespace SGuard.ConfigValidation.Tests;

public sealed class ValidationMetricsTests
{
    [Fact]
    public void RecordValidationSuccess_Should_NotThrow()
    {
        // Act
        var act = () => ValidationMetrics.RecordValidationSuccess();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordValidationFailure_Should_NotThrow()
    {
        // Act
        var act = () => ValidationMetrics.RecordValidationFailure();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordEnvironmentValidation_Should_NotThrow()
    {
        // Act
        var act = () => ValidationMetrics.RecordEnvironmentValidation();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordValidationDuration_With_ValidDuration_Should_NotThrow()
    {
        // Act
        var act = () => ValidationMetrics.RecordValidationDuration(100.0);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordValidationDuration_With_Zero_Should_NotThrow()
    {
        // Act
        var act = () => ValidationMetrics.RecordValidationDuration(0.0);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordValidationDuration_With_NegativeDuration_Should_NotThrow()
    {
        // Act - Negative values should be handled gracefully by the histogram
        var act = () => ValidationMetrics.RecordValidationDuration(-10.0);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordEnvironmentValidationDuration_With_ValidDuration_Should_NotThrow()
    {
        // Act
        var act = () => ValidationMetrics.RecordEnvironmentValidationDuration(50.0);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordEnvironmentValidationDuration_With_Zero_Should_NotThrow()
    {
        // Act
        var act = () => ValidationMetrics.RecordEnvironmentValidationDuration(0.0);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordEnvironmentValidationDuration_With_NegativeDuration_Should_NotThrow()
    {
        // Act - Negative values should be handled gracefully by the histogram
        var act = () => ValidationMetrics.RecordEnvironmentValidationDuration(-5.0);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordCacheHit_Should_NotThrow()
    {
        // Act
        var act = () => ValidationMetrics.RecordCacheHit();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordCacheMiss_Should_NotThrow()
    {
        // Act
        var act = () => ValidationMetrics.RecordCacheMiss();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordFileLoadingDuration_With_ValidDuration_Should_NotThrow()
    {
        // Act
        var act = () => ValidationMetrics.RecordFileLoadingDuration(25.0);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordFileLoadingDuration_With_Zero_Should_NotThrow()
    {
        // Act
        var act = () => ValidationMetrics.RecordFileLoadingDuration(0.0);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordFileLoadingDuration_With_NegativeDuration_Should_NotThrow()
    {
        // Act - Negative values should be handled gracefully by the histogram
        var act = () => ValidationMetrics.RecordFileLoadingDuration(-1.0);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordValidationDuration_With_LargeValue_Should_NotThrow()
    {
        // Act
        var act = () => ValidationMetrics.RecordValidationDuration(double.MaxValue);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordEnvironmentValidationDuration_With_LargeValue_Should_NotThrow()
    {
        // Act
        var act = () => ValidationMetrics.RecordEnvironmentValidationDuration(double.MaxValue);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordFileLoadingDuration_With_LargeValue_Should_NotThrow()
    {
        // Act
        var act = () => ValidationMetrics.RecordFileLoadingDuration(double.MaxValue);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void MultipleMetricsCalls_Should_NotThrow()
    {
        // Act - Call multiple metrics methods in sequence
        var act = () =>
        {
            ValidationMetrics.RecordValidationSuccess();
            ValidationMetrics.RecordValidationFailure();
            ValidationMetrics.RecordEnvironmentValidation();
            ValidationMetrics.RecordValidationDuration(100.0);
            ValidationMetrics.RecordEnvironmentValidationDuration(50.0);
            ValidationMetrics.RecordCacheHit();
            ValidationMetrics.RecordCacheMiss();
            ValidationMetrics.RecordFileLoadingDuration(25.0);
        };

        // Assert
        act.Should().NotThrow();
    }
}

