namespace SGuard.ConfigValidation.Common;

/// <summary>
/// Shared constants and static instances for performance optimization.
/// These are immutable and thread-safe, can be safely reused across the application.
/// </summary>
public static class SharedConstants
{
    /// <summary>
    /// Default capacity for dictionaries when size is unknown.
    /// </summary>
    public const int DefaultDictionaryCapacity = 16;

    /// <summary>
    /// Maximum reasonable capacity for dictionaries to prevent excessive memory allocation.
    /// </summary>
    public const int MaxDictionaryCapacity = 10000;

    /// <summary>
    /// Buffer size for file streaming operations (64KB).
    /// </summary>
    public const int FileStreamBufferSize = 65536;

    /// <summary>
    /// File size threshold for using streaming (1MB).
    /// Files larger than this will use streaming for better memory efficiency.
    /// </summary>
    public const long StreamingThresholdBytes = 1024 * 1024;

    /// <summary>
    /// Estimated keys per KB of JSON (for dictionary pre-allocation).
    /// </summary>
    public const int EstimatedKeysPerKilobyte = 50;

    /// <summary>
    /// Estimated keys per MB of JSON for large files (for dictionary pre-allocation).
    /// </summary>
    public const int EstimatedKeysPerMegabyte = 100;
}

