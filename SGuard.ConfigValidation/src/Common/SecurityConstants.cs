namespace SGuard.ConfigValidation.Common;

/// <summary>
/// Security-related constants for resource limits and DoS protection.
/// These limits prevent denial-of-service attacks by restricting resource consumption.
/// </summary>
public static class SecurityConstants
{
    // Default values (can be overridden via configuration)
    
    /// <summary>
    /// Default maximum allowed file size in bytes (100 MB).
    /// Files larger than this will be rejected to prevent DoS attacks.
    /// </summary>
    public const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB

    /// <summary>
    /// Default maximum number of environments allowed in a configuration file.
    /// Prevents DoS attacks through excessive environment definitions.
    /// </summary>
    public const int MaxEnvironmentsCount = 1000;

    /// <summary>
    /// Default maximum number of rules allowed in a configuration file.
    /// Prevents DoS attacks through excessive rule definitions.
    /// </summary>
    public const int MaxRulesCount = 10000;

    /// <summary>
    /// Default maximum number of conditions allowed per rule.
    /// Prevents DoS attacks through excessive condition definitions in a single rule.
    /// </summary>
    public const int MaxConditionsPerRule = 1000;

    /// <summary>
    /// Default maximum number of validators allowed per condition.
    /// Prevents DoS attacks through excessive validator definitions in a single condition.
    /// </summary>
    public const int MaxValidatorsPerCondition = 100;

    /// <summary>
    /// Default maximum number of entries allowed in the path resolver cache.
    /// Prevents memory exhaustion through cache growth.
    /// </summary>
    public const int MaxPathCacheSize = 10000;

    /// <summary>
    /// Default maximum length for a single path string (characters).
    /// Prevents DoS attacks through extremely long path strings.
    /// </summary>
    public const int MaxPathLength = 4096; // Common filesystem limit

    /// <summary>
    /// Default maximum depth for nested JSON/YAML structures.
    /// Prevents stack overflow attacks through deeply nested structures.
    /// </summary>
    public const int MaxJsonDepth = 64;

    // Hard limits (absolute maximums cannot be exceeded even via configuration)
    
    /// <summary>
    /// Hard limit for maximum allowed file size in bytes (500 MB).
    /// This is the absolute maximum that cannot be exceeded even if configured higher.
    /// Prevents DoS attacks by ensuring files cannot exceed this size regardless of configuration.
    /// </summary>
    public const long MaxFileSizeBytesHardLimit = 500 * 1024 * 1024; // 500 MB

    /// <summary>
    /// Hard limit for the maximum number of environments allowed in a configuration file (5000).
    /// This is the absolute maximum that cannot be exceeded even if configured higher.
    /// </summary>
    public const int MaxEnvironmentsCountHardLimit = 5000;

    /// <summary>
    /// Hard limit for the maximum number of rules allowed in a configuration file (50,000).
    /// This is the absolute maximum that cannot be exceeded even if configured higher.
    /// </summary>
    public const int MaxRulesCountHardLimit = 50000;

    /// <summary>
    /// Hard limit for the maximum number of conditions allowed per rule (5000).
    /// This is the absolute maximum that cannot be exceeded even if configured higher.
    /// </summary>
    public const int MaxConditionsPerRuleHardLimit = 5000;

    /// <summary>
    /// Hard limit for the maximum number of validators allowed per condition (500).
    /// This is the absolute maximum that cannot be exceeded even if configured higher.
    /// </summary>
    public const int MaxValidatorsPerConditionHardLimit = 500;

    /// <summary>
    /// Hard limit for the maximum number of entries allowed in the path resolver cache (100,000).
    /// This is the absolute maximum that cannot be exceeded even if configured higher.
    /// </summary>
    public const int MaxPathCacheSizeHardLimit = 100000;

    /// <summary>
    /// Hard limit for the maximum length for a single path string (16,384 characters).
    /// This is the absolute maximum that cannot be exceeded even if configured higher.
    /// Based on common filesystem limits (Windows MAX_PATH is 260, but extended paths can be up to 32,767).
    /// </summary>
    public const int MaxPathLengthHardLimit = 16384;

    /// <summary>
    /// Hard limit for maximum depth for nested JSON/YAML structures (256).
    /// This is the absolute maximum that cannot be exceeded even if configured higher.
    /// Prevents stack overflow attacks through extremely deeply nested structures.
    /// </summary>
    public const int MaxJsonDepthHardLimit = 256;
}

