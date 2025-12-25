namespace SGuard.ConfigValidation.Services;

/// <summary>
/// Interface for resolving relative paths to absolute paths.
/// </summary>
public interface IPathResolver
{
    /// <summary>
    /// Resolves a relative path to an absolute path based on the base path.
    /// If the path is already absolute, it is returned as-is.
    /// </summary>
    /// <param name="path">The path to resolve (can be relative or absolute).</param>
    /// <param name="basePath">The base path used for resolving relative paths.</param>
    /// <returns>An absolute path.</returns>
    string ResolvePath(string path, string basePath);
}

