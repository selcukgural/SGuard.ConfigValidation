using System.Diagnostics.Metrics;

namespace SGuard.ConfigValidation.Telemetry;

/// <summary>
/// Metrics collector for validation operations.
/// Uses System.Diagnostics.Metrics for .NET 8+ built-in metrics support.
/// </summary>
/// <remarks>
/// This class provides static methods to record various validation and cache metrics.
/// All metrics are registered under the "SGuard.ConfigValidation" meter.
/// Thread-safe and intended for internal use only.
/// </remarks>
internal static class ValidationMetrics
{
    /// <summary>
    /// The meter instance for all SGuard.ConfigValidation metrics.
    /// </summary>
    private static readonly Meter Meter = new("SGuard.ConfigValidation", "0.0.1");

    // Counter metrics

    /// <summary>
    /// Counter for successful validation operations.
    /// </summary>
    private static readonly Counter<long> ValidationSuccessCounter = Meter.CreateCounter<long>(
        "sguard.validation.success", "count", "Number of successful validations");

    /// <summary>
    /// Counter for failed validation operations.
    /// </summary>
    private static readonly Counter<long> ValidationFailureCounter = Meter.CreateCounter<long>(
        "sguard.validation.failure", "count", "Number of failed validations");

    /// <summary>
    /// Counter for environment validation operations.
    /// </summary>
    private static readonly Counter<long> EnvironmentValidationCounter = Meter.CreateCounter<long>(
        "sguard.validation.environment", "count", "Number of environment validations");

    /// <summary>
    /// Counter for cache hit events.
    /// </summary>
    private static readonly Counter<long> CacheHitCounter = Meter.CreateCounter<long>("sguard.cache.hit", "count", "Number of cache hits");

    /// <summary>
    /// Counter for cache miss events.
    /// </summary>
    private static readonly Counter<long> CacheMissCounter = Meter.CreateCounter<long>("sguard.cache.miss", "count", "Number of cache misses");

    // Histogram metrics

    /// <summary>
    /// Histogram for validation operation durations (milliseconds).
    /// </summary>
    private static readonly Histogram<double> ValidationDurationHistogram = Meter.CreateHistogram<double>(
        "sguard.validation.duration", "ms", "Duration of validation operations in milliseconds");

    /// <summary>
    /// Histogram for environment validation operation durations (milliseconds).
    /// </summary>
    private static readonly Histogram<double> EnvironmentValidationDurationHistogram = Meter.CreateHistogram<double>(
        "sguard.validation.environment.duration", "ms", "Duration of environment validation operations in milliseconds");

    /// <summary>
    /// Histogram for file loading operation durations (milliseconds).
    /// </summary>
    private static readonly Histogram<double> FileLoadingDurationHistogram = Meter.CreateHistogram<double>(
        "sguard.file.loading.duration", "ms", "Duration of file loading operations in milliseconds");

    /// <summary>
    /// Records a successful validation.
    /// </summary>
    /// <remarks>
    /// Increments the <see cref="ValidationSuccessCounter"/> by 1.
    /// </remarks>
    public static void RecordValidationSuccess()
    {
        ValidationSuccessCounter.Add(1);
    }

    /// <summary>
    /// Records a failed validation.
    /// </summary>
    /// <remarks>
    /// Increments the <see cref="ValidationFailureCounter"/> by 1.
    /// </remarks>
    public static void RecordValidationFailure()
    {
        ValidationFailureCounter.Add(1);
    }

    /// <summary>
    /// Records an environment validation.
    /// </summary>
    /// <remarks>
    /// Increments the <see cref="EnvironmentValidationCounter"/> by 1.
    /// </remarks>
    public static void RecordEnvironmentValidation()
    {
        EnvironmentValidationCounter.Add(1);
    }

    /// <summary>
    /// Records validation duration.
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds. Must be non-negative.</param>
    /// <remarks>
    /// Records the duration of a validation operation in the <see cref="ValidationDurationHistogram"/>.
    /// </remarks>
    public static void RecordValidationDuration(double durationMs)
    {
        ValidationDurationHistogram.Record(durationMs);
    }

    /// <summary>
    /// Records environment validation duration.
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds. Must be non-negative.</param>
    /// <remarks>
    /// Records the duration of an environment validation operation in the <see cref="EnvironmentValidationDurationHistogram"/>.
    /// </remarks>
    public static void RecordEnvironmentValidationDuration(double durationMs)
    {
        EnvironmentValidationDurationHistogram.Record(durationMs);
    }

    /// <summary>
    /// Records a cache hit.
    /// </summary>
    /// <remarks>
    /// Increments the <see cref="CacheHitCounter"/> by 1.
    /// </remarks>
    public static void RecordCacheHit()
    {
        CacheHitCounter.Add(1);
    }

    /// <summary>
    /// Records a cache miss.
    /// </summary>
    /// <remarks>
    /// Increments the <see cref="CacheMissCounter"/> by 1.
    /// </remarks>
    public static void RecordCacheMiss()
    {
        CacheMissCounter.Add(1);
    }

    /// <summary>
    /// Records file loading duration.
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds. Must be non-negative.</param>
    /// <remarks>
    /// Records the duration of a file loading operation in the <see cref="FileLoadingDurationHistogram"/>.
    /// </remarks>
    public static void RecordFileLoadingDuration(double durationMs)
    {
        FileLoadingDurationHistogram.Record(durationMs);
    }
}