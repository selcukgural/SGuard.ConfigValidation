namespace SGuard.ConfigValidation.Hooks;

/// <summary>
/// Interface for post-validation hooks that can be executed after validation completes.
/// Hooks are executed asynchronously and non-blocking - failures do not affect validation results.
/// </summary>
public interface IHook
{
    /// <summary>
    /// Executes the hook asynchronously with the validation result context.
    /// </summary>
    /// <param name="context">The hook execution context containing validation result and environment information.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(HookContext context, CancellationToken cancellationToken = default);
}

