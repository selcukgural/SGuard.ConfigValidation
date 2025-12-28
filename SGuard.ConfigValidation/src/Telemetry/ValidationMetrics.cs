using System.Diagnostics.Metrics;

namespace SGuard.ConfigValidation.Telemetry;

/// <summary>
/// Metrics collector for validation operations.
/// Uses System.Diagnostics.Metrics for .NET 8+ built-in metrics support.
/// </summary>
public sealed class ValidationMetrics
{
    private static readonly Meter Meter = new("SGuard.ConfigValidation", "1.0.0");
    
    // Counter metrics
    private static readonly Counter<long> ValidationSuccessCounter = Meter.CreateCounter<long>(
        "sguard.validation.success",
        "count",
        "Number of successful validations");
    
    private static readonly Counter<long> ValidationFailureCounter = Meter.CreateCounter<long>(
        "sguard.validation.failure",
        "count",
        "Number of failed validations");
    
    private static readonly Counter<long> EnvironmentValidationCounter = Meter.CreateCounter<long>(
        "sguard.validation.environment",
        "count",
        "Number of environment validations");
    
    private static readonly Counter<long> CacheHitCounter = Meter.CreateCounter<long>(
        "sguard.cache.hit",
        "count",
        "Number of cache hits");
    
    private static readonly Counter<long> CacheMissCounter = Meter.CreateCounter<long>(
        "sguard.cache.miss",
        "count",
        "Number of cache misses");
    
    private static readonly Counter<long> HookExecutionCounter = Meter.CreateCounter<long>(
        "sguard.hook.execution",
        "count",
        "Number of hook executions");
    
    private static readonly Counter<long> HookExecutionFailureCounter = Meter.CreateCounter<long>(
        "sguard.hook.execution.failure",
        "count",
        "Number of failed hook executions");
    
    // Histogram metrics
    private static readonly Histogram<double> ValidationDurationHistogram = Meter.CreateHistogram<double>(
        "sguard.validation.duration",
        "ms",
        "Duration of validation operations in milliseconds");
    
    private static readonly Histogram<double> EnvironmentValidationDurationHistogram = Meter.CreateHistogram<double>(
        "sguard.validation.environment.duration",
        "ms",
        "Duration of environment validation operations in milliseconds");
    
    private static readonly Histogram<double> FileLoadingDurationHistogram = Meter.CreateHistogram<double>(
        "sguard.file.loading.duration",
        "ms",
        "Duration of file loading operations in milliseconds");
    
    private static readonly Histogram<double> HookExecutionDurationHistogram = Meter.CreateHistogram<double>(
        "sguard.hook.execution.duration",
        "ms",
        "Duration of hook execution operations in milliseconds");
    
    private ValidationMetrics()
    {
        // Private constructor to prevent instantiation
        // Use static methods for metrics recording
    }
    
    /// <summary>
    /// Records a successful validation.
    /// </summary>
    public static void RecordValidationSuccess()
    {
        ValidationSuccessCounter.Add(1);
    }
    
    /// <summary>
    /// Records a failed validation.
    /// </summary>
    public static void RecordValidationFailure()
    {
        ValidationFailureCounter.Add(1);
    }
    
    /// <summary>
    /// Records an environment validation.
    /// </summary>
    public static void RecordEnvironmentValidation()
    {
        EnvironmentValidationCounter.Add(1);
    }
    
    /// <summary>
    /// Records validation duration.
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds.</param>
    public static void RecordValidationDuration(double durationMs)
    {
        ValidationDurationHistogram.Record(durationMs);
    }
    
    /// <summary>
    /// Records environment validation duration.
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds.</param>
    public static void RecordEnvironmentValidationDuration(double durationMs)
    {
        EnvironmentValidationDurationHistogram.Record(durationMs);
    }
    
    /// <summary>
    /// Records a cache hit.
    /// </summary>
    public static void RecordCacheHit()
    {
        CacheHitCounter.Add(1);
    }
    
    /// <summary>
    /// Records a cache miss.
    /// </summary>
    public static void RecordCacheMiss()
    {
        CacheMissCounter.Add(1);
    }
    
    /// <summary>
    /// Records file loading duration.
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds.</param>
    public static void RecordFileLoadingDuration(double durationMs)
    {
        FileLoadingDurationHistogram.Record(durationMs);
    }
    
    /// <summary>
    /// Records a hook execution.
    /// </summary>
    public static void RecordHookExecution()
    {
        HookExecutionCounter.Add(1);
    }
    
    /// <summary>
    /// Records a failed hook execution.
    /// </summary>
    public static void RecordHookExecutionFailure()
    {
        HookExecutionFailureCounter.Add(1);
    }
    
    /// <summary>
    /// Records hook execution duration.
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds.</param>
    public static void RecordHookExecutionDuration(double durationMs)
    {
        HookExecutionDurationHistogram.Record(durationMs);
    }
}

