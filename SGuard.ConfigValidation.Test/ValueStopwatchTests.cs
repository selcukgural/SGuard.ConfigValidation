using FluentAssertions;
using SGuard.ConfigValidation.Utilities;
using System.Diagnostics;

namespace SGuard.ConfigValidation.Test;

/// <summary>
/// Tests for <see cref="ValueStopwatch"/>.
/// </summary>
public sealed class ValueStopwatchTests
{
    [Fact]
    public void StartNew_Should_Create_Started_Stopwatch()
    {
        // Act
        var stopwatch = ValueStopwatch.StartNew();

        // Assert
        stopwatch.ElapsedTicks.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void ElapsedTicks_Should_Increase_OverTime()
    {
        // Arrange
        var stopwatch = ValueStopwatch.StartNew();

        // Act
        Thread.Sleep(10); // Small delay to ensure time passes
        var ticks = stopwatch.ElapsedTicks;

        // Assert
        ticks.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ElapsedMilliseconds_Should_Return_Valid_Time()
    {
        // Arrange
        var stopwatch = ValueStopwatch.StartNew();

        // Act
        Thread.Sleep(50); // Sleep for 50ms
        var milliseconds = stopwatch.ElapsedMilliseconds;

        // Assert
        milliseconds.Should().BeGreaterThanOrEqualTo(40); // Allow some tolerance
        milliseconds.Should().BeLessThan(200); // Should not be too large
    }

    [Fact]
    public void Elapsed_Should_Return_Valid_TimeSpan()
    {
        // Arrange
        var stopwatch = ValueStopwatch.StartNew();

        // Act
        Thread.Sleep(50);
        var elapsed = stopwatch.Elapsed;

        // Assert
        elapsed.Should().BeGreaterThan(TimeSpan.Zero);
        elapsed.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(40);
        elapsed.TotalMilliseconds.Should().BeLessThan(200);
    }

    [Fact]
    public void Elapsed_Should_Be_Consistent_With_ElapsedMilliseconds()
    {
        // Arrange
        var stopwatch = ValueStopwatch.StartNew();

        // Act
        Thread.Sleep(50);
        var elapsedMs = stopwatch.ElapsedMilliseconds;
        var elapsed = stopwatch.Elapsed;

        // Assert
        var elapsedMsFromTimeSpan = (long)elapsed.TotalMilliseconds;
        Math.Abs(elapsedMs - elapsedMsFromTimeSpan).Should().BeLessThan(10); // Allow small difference due to rounding
    }

    [Fact]
    public void ToString_Should_Return_Elapsed_Time_String()
    {
        // Arrange
        var stopwatch = ValueStopwatch.StartNew();

        // Act
        Thread.Sleep(10);
        var result = stopwatch.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain(".");
    }

    [Fact]
    public void Multiple_Stopwatches_Should_Be_Independent()
    {
        // Arrange
        var stopwatch1 = ValueStopwatch.StartNew();
        Thread.Sleep(20);
        var stopwatch2 = ValueStopwatch.StartNew();

        // Act
        Thread.Sleep(20);
        var elapsed1 = stopwatch1.ElapsedMilliseconds;
        var elapsed2 = stopwatch2.ElapsedMilliseconds;

        // Assert
        elapsed1.Should().BeGreaterThan(elapsed2);
    }

    [Fact]
    public void ElapsedTicks_Should_Use_Stopwatch_Frequency()
    {
        // Arrange
        var stopwatch = ValueStopwatch.StartNew();

        // Act
        Thread.Sleep(50);
        var ticks = stopwatch.ElapsedTicks;
        var frequency = Stopwatch.Frequency;

        // Assert
        ticks.Should().BeGreaterThan(0);
        frequency.Should().BeGreaterThan(0);
        // ElapsedTicks should be in Stopwatch ticks, not DateTime ticks
        var expectedMinTicks = (long)(frequency * 0.04); // At least 40ms worth
        ticks.Should().BeGreaterThanOrEqualTo(expectedMinTicks);
    }
}

