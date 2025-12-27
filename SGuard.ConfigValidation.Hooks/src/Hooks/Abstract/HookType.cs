namespace SGuard.ConfigValidation.Hooks;

/// <summary>
/// Represents the types of hooks available in the system.
/// </summary>
internal static class HookType
{
    /// <summary>
    /// Script-based hook.
    /// </summary>
    public const string Script = "script";

    /// <summary>
    /// Webhook type.
    /// </summary>
    public const string Web = "webhook";
}