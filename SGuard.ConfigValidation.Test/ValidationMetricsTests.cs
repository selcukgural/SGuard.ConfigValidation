using System.Collections.Concurrent;
using FluentAssertions;
using SGuard.ConfigValidation.Telemetry;

namespace SGuard.ConfigValidation.Test;

public sealed class ValidationMetricsTests
{
    [Fact]
    public void RecordValidationSuccess_Should_NotThrow()
    {
        // Act
        var act = ValidationMetrics.RecordValidationSuccess;

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordValidationFailure_Should_NotThrow()
    {
        // Act
        var act = ValidationMetrics.RecordValidationFailure;

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordEnvironmentValidation_Should_NotThrow()
    {
        // Act
        var act = ValidationMetrics.RecordEnvironmentValidation;

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
        var act = ValidationMetrics.RecordCacheMiss;

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

    [Fact]
    public async Task ConcurrentAccess_Should_BeThreadSafe()
    {
        // Arrange
        const int threadCount = 10;
        const int iterationsPerThread = 100;
        var exceptions = new ConcurrentBag<Exception>();

        // Act - Execute all metrics methods concurrently from multiple threads
        var tasks = Enumerable.Range(0, threadCount).Select(threadIndex => Task.Run(() =>
        {
            try
            {
                for (var i = 0; i < iterationsPerThread; i++)
                {
                    ValidationMetrics.RecordValidationSuccess();
                    ValidationMetrics.RecordValidationFailure();
                    ValidationMetrics.RecordEnvironmentValidation();
                    ValidationMetrics.RecordValidationDuration(100.0 + i);
                    ValidationMetrics.RecordEnvironmentValidationDuration(50.0 + i);
                    ValidationMetrics.RecordCacheHit();
                    ValidationMetrics.RecordCacheMiss();
                    ValidationMetrics.RecordFileLoadingDuration(25.0 + i);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }));

        await Task.WhenAll(tasks.ToArray());

        // Assert - No exceptions should occur during concurrent access
        exceptions.Should().BeEmpty("concurrent access to ValidationMetrics should be thread-safe");
    }

    [Fact]
    public async Task ConcurrentAccess_WithHighContention_Should_BeThreadSafe()
    {
        // Arrange - High contention scenario with many threads calling the same method
        const int threadCount = 20;
        const int iterationsPerThread = 500;
        var exceptions = new ConcurrentBag<Exception>();

        // Act - All threads call the same metrics method concurrently
        var tasks = Enumerable.Range(0, threadCount).Select(threadIndex => Task.Run(() =>
        {
            try
            {
                for (var i = 0; i < iterationsPerThread; i++)
                {
                    // All threads call the same method to create high contention
                    ValidationMetrics.RecordValidationSuccess();
                    ValidationMetrics.RecordCacheHit();
                    ValidationMetrics.RecordValidationDuration(100.0);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }));

        await Task.WhenAll(tasks.ToArray());

        // Assert - No exceptions should occur even under high contention
        exceptions.Should().BeEmpty("concurrent access to ValidationMetrics should be thread-safe even under high contention");
    }
}

