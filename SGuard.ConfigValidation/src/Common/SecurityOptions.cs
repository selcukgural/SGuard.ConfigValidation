using Microsoft.Extensions.Logging;

namespace SGuard.ConfigValidation.Common;

/// <summary>
/// Configuration options for security limits and DoS protection.
/// These values can be configured via appsettings.json or environment variables.
/// Default values are defined in <see cref="SecurityConstants"/>.
/// Values exceeding hard limits will be clamped to the hard limits.
/// </summary>
public sealed class SecurityOptions
{
    /// <summary>
    /// Maximum allowed file size in bytes.
    /// Default: 100 MB (104,857,600 bytes)
    /// Hard limit: 500 MB (524,288,000 bytes)
    /// Files larger than this will be rejected to prevent DoS attacks.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = SecurityConstants.MaxFileSizeBytes;

    /// <summary>
    /// Maximum number of environments allowed in a configuration file.
    /// Default: 1000
    /// Hard limit: 5000
    /// Prevents DoS attacks through excessive environment definitions.
    /// </summary>
    public int MaxEnvironmentsCount { get; set; } = SecurityConstants.MaxEnvironmentsCount;

    /// <summary>
    /// Maximum number of rules allowed in a configuration file.
    /// Default: 10,000
    /// Hard limit: 50000
    /// Prevents DoS attacks through excessive rule definitions.
    /// </summary>
    public int MaxRulesCount { get; set; } = SecurityConstants.MaxRulesCount;

    /// <summary>
    /// Maximum number of conditions allowed per rule.
    /// Default: 1000
    /// Hard limit: 5000
    /// Prevents DoS attacks through excessive condition definitions in a single rule.
    /// </summary>
    public int MaxConditionsPerRule { get; set; } = SecurityConstants.MaxConditionsPerRule;

    /// <summary>
    /// Maximum number of validators allowed per condition.
    /// Default: 100
    /// Hard limit: 500
    /// Prevents DoS attacks through excessive validator definitions in a single condition.
    /// </summary>
    public int MaxValidatorsPerCondition { get; set; } = SecurityConstants.MaxValidatorsPerCondition;

    /// <summary>
    /// Maximum number of entries allowed in the path resolver cache.
    /// Default: 10,000
    /// Hard limit: 100 thousand
    /// Prevents memory exhaustion through cache growth.
    /// </summary>
    public int MaxPathCacheSize { get; set; } = SecurityConstants.MaxPathCacheSize;

    /// <summary>
    /// Maximum length for a single path string (characters).
    /// Default: 4096
    /// Hard limit: 16384
    /// Prevents DoS attacks through extremely long path strings.
    /// </summary>
    public int MaxPathLength { get; set; } = SecurityConstants.MaxPathLength;

    /// <summary>
    /// Maximum depth for nested JSON/YAML structures.
    /// Default: 64
    /// Hard limit: 256
    /// Prevents stack overflow attacks through deeply nested structures.
    /// </summary>
    public int MaxJsonDepth { get; set; } = SecurityConstants.MaxJsonDepth;

    /// <summary>
    /// Maximum number of environments that can be validated in parallel.
    /// Default: Number of processor cores (Environment.ProcessorCount)
    /// Hard limit: 100
    /// Prevents resource exhaustion from excessive parallelization.
    /// </summary>
    public int MaxParallelEnvironments { get; set; } = SecurityConstants.MaxParallelEnvironments;

    /// <summary>
    /// Maximum script output size in bytes.
    /// Default: 1 MB (1,048,576 bytes)
    /// Hard limit: 10 MB (10,485,760 bytes)
    /// Script hooks output exceeding this limit will be truncated to prevent DoS attacks.
    /// </summary>
    public long MaxScriptOutputSizeBytes { get; set; } = SecurityConstants.MaxScriptOutputSizeBytes;

    /// <summary>
    /// Validates and clamps security options to ensure they do not exceed hard limits.
    /// Logs warnings if values are clamped.
    /// </summary>
    /// <param name="logger">Optional logger instance for logging warnings when values are clamped. If null, no warnings will be logged.</param>
    /// <returns>True if any values were clamped; otherwise, false.</returns>
    public bool ValidateAndClamp(ILogger? logger = null)
    {
        var clamped = false;

        if (MaxFileSizeBytes > SecurityConstants.MaxFileSizeBytesHardLimit)
        {
            logger?.LogWarning(
                "Security option 'MaxFileSizeBytes' ({Value} bytes) exceeds hard limit ({HardLimit} bytes). Clamping to hard limit.",
                MaxFileSizeBytes, SecurityConstants.MaxFileSizeBytesHardLimit);
            MaxFileSizeBytes = SecurityConstants.MaxFileSizeBytesHardLimit;
            clamped = true;
        }

        if (MaxEnvironmentsCount > SecurityConstants.MaxEnvironmentsCountHardLimit)
        {
            logger?.LogWarning(
                "Security option 'MaxEnvironmentsCount' ({Value}) exceeds hard limit ({HardLimit}). Clamping to hard limit.",
                MaxEnvironmentsCount, SecurityConstants.MaxEnvironmentsCountHardLimit);
            MaxEnvironmentsCount = SecurityConstants.MaxEnvironmentsCountHardLimit;
            clamped = true;
        }

        if (MaxRulesCount > SecurityConstants.MaxRulesCountHardLimit)
        {
            logger?.LogWarning(
                "Security option 'MaxRulesCount' ({Value}) exceeds hard limit ({HardLimit}). Clamping to hard limit.",
                MaxRulesCount, SecurityConstants.MaxRulesCountHardLimit);
            MaxRulesCount = SecurityConstants.MaxRulesCountHardLimit;
            clamped = true;
        }

        if (MaxConditionsPerRule > SecurityConstants.MaxConditionsPerRuleHardLimit)
        {
            logger?.LogWarning(
                "Security option 'MaxConditionsPerRule' ({Value}) exceeds hard limit ({HardLimit}). Clamping to hard limit.",
                MaxConditionsPerRule, SecurityConstants.MaxConditionsPerRuleHardLimit);
            MaxConditionsPerRule = SecurityConstants.MaxConditionsPerRuleHardLimit;
            clamped = true;
        }

        if (MaxValidatorsPerCondition > SecurityConstants.MaxValidatorsPerConditionHardLimit)
        {
            logger?.LogWarning(
                "Security option 'MaxValidatorsPerCondition' ({Value}) exceeds hard limit ({HardLimit}). Clamping to hard limit.",
                MaxValidatorsPerCondition, SecurityConstants.MaxValidatorsPerConditionHardLimit);
            MaxValidatorsPerCondition = SecurityConstants.MaxValidatorsPerConditionHardLimit;
            clamped = true;
        }

        if (MaxPathCacheSize > SecurityConstants.MaxPathCacheSizeHardLimit)
        {
            logger?.LogWarning(
                "Security option 'MaxPathCacheSize' ({Value}) exceeds hard limit ({HardLimit}). Clamping to hard limit.",
                MaxPathCacheSize, SecurityConstants.MaxPathCacheSizeHardLimit);
            MaxPathCacheSize = SecurityConstants.MaxPathCacheSizeHardLimit;
            clamped = true;
        }

        if (MaxPathLength > SecurityConstants.MaxPathLengthHardLimit)
        {
            logger?.LogWarning(
                "Security option 'MaxPathLength' ({Value}) exceeds hard limit ({HardLimit}). Clamping to hard limit.",
                MaxPathLength, SecurityConstants.MaxPathLengthHardLimit);
            MaxPathLength = SecurityConstants.MaxPathLengthHardLimit;
            clamped = true;
        }

        if (MaxJsonDepth > SecurityConstants.MaxJsonDepthHardLimit)
        {
            logger?.LogWarning(
                "Security option 'MaxJsonDepth' ({Value}) exceeds hard limit ({HardLimit}). Clamping to hard limit.",
                MaxJsonDepth, SecurityConstants.MaxJsonDepthHardLimit);
            MaxJsonDepth = SecurityConstants.MaxJsonDepthHardLimit;
            clamped = true;
        }

        if (MaxParallelEnvironments > SecurityConstants.MaxParallelEnvironmentsHardLimit)
        {
            logger?.LogWarning(
                "Security option 'MaxParallelEnvironments' ({Value}) exceeds hard limit ({HardLimit}). Clamping to hard limit.",
                MaxParallelEnvironments, SecurityConstants.MaxParallelEnvironmentsHardLimit);
            MaxParallelEnvironments = SecurityConstants.MaxParallelEnvironmentsHardLimit;
            clamped = true;
        }

        if (MaxScriptOutputSizeBytes > SecurityConstants.MaxScriptOutputSizeBytesHardLimit)
        {
            logger?.LogWarning(
                "Security option 'MaxScriptOutputSizeBytes' ({Value} bytes) exceeds hard limit ({HardLimit} bytes). Clamping to hard limit.",
                MaxScriptOutputSizeBytes, SecurityConstants.MaxScriptOutputSizeBytesHardLimit);
            MaxScriptOutputSizeBytes = SecurityConstants.MaxScriptOutputSizeBytesHardLimit;
            clamped = true;
        }

        return clamped;
    }
}

