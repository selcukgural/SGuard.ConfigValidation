using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace SGuard.ConfigValidation.Validators.Plugin;

/// <summary>
/// Discovers and loads validator plugins from assemblies.
/// </summary>
public sealed class ValidatorPluginDiscovery
{
    private readonly ILogger<ValidatorPluginDiscovery> _logger;
    
    // Cache for assembly -> plugin types to avoid repeated reflection
    private readonly ConcurrentDictionary<Assembly, Type[]> _assemblyTypesCache = new();

    /// <summary>
    /// Initializes a new instance of the ValidatorPluginDiscovery class.
    /// </summary>
    /// <param name="logger">Logger instance for logging discovery operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public ValidatorPluginDiscovery(ILogger<ValidatorPluginDiscovery> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Discovers validator plugins from the current assembly and optionally from additional plugin directories.
    /// Uses reflection caching to improve performance when discovering from the same assemblies multiple times.
    /// </summary>
    /// <param name="pluginDirectories">Optional list of directories to scan for plugin assemblies (.dll files).</param>
    /// <returns>A dictionary mapping validator type names to validator instances. Dictionary keys are case-insensitive.</returns>
    /// <exception cref="ReflectionTypeLoadException">Thrown when some types in an assembly cannot be loaded (handled internally, only successfully loaded types are used).</exception>
    public Dictionary<string, IValidator<object>> DiscoverValidators(IEnumerable<string>? pluginDirectories = null)
    {
        var validators = new Dictionary<string, IValidator<object>>(StringComparer.OrdinalIgnoreCase);
        
        // Discover from the current assembly
        DiscoverFromAssembly(Assembly.GetExecutingAssembly(), validators);

        // Discover from plugin directories if provided
        if (pluginDirectories != null)
        {
            foreach (var directory in pluginDirectories)
            {
                if (Directory.Exists(directory))
                {
                    DiscoverFromDirectory(directory, validators);
                }
                else
                {
                    _logger.LogWarning("Plugin directory does not exist: {Directory}", directory);
                }
            }
        }

        _logger.LogInformation("Discovered {Count} validator plugin(s)", validators.Count);
        return validators;
    }

    /// <summary>
    /// Discovers validators from a specific assembly.
    /// </summary>
    private void DiscoverFromAssembly(Assembly assembly, Dictionary<string, IValidator<object>> validators)
    {
        try
        {
            // Get types from the cache or load them
            var allTypes = _assemblyTypesCache.GetOrAdd(assembly, asm =>
            {
                try
                {
                    return asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Return only successfully loaded types
                    return ex.Types.Where(t => t != null).ToArray()!;
                }
            });

            // Filter plugin types (cached reflection result)
            var pluginTypes = allTypes
                .Where(t => typeof(IValidatorPlugin).IsAssignableFrom(t) 
                            && t is { IsInterface: false, IsAbstract: false });

            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    if (Activator.CreateInstance(pluginType) is IValidatorPlugin plugin)
                    {
                        var validatorType = plugin.ValidatorType;
                        if (string.IsNullOrWhiteSpace(validatorType))
                        {
                            _logger.LogWarning("Plugin {PluginType} has empty ValidatorType, skipping", pluginType.Name);
                            continue;
                        }

                        if (validators.ContainsKey(validatorType))
                        {
                            _logger.LogWarning("Validator type '{ValidatorType}' is already registered, skipping plugin {PluginType}", 
                                validatorType, pluginType.Name);
                            continue;
                        }

                        validators[validatorType] = plugin.Validator;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to instantiate validator plugin {PluginType}", pluginType.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering validators from assembly {AssemblyName}", assembly.FullName);
        }
    }

    /// <summary>
    /// Discovers validators from all assemblies in a directory.
    /// </summary>
    private void DiscoverFromDirectory(string directory, Dictionary<string, IValidator<object>> validators)
    {
        try
        {
            var assemblyFiles = Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly);
            
            foreach (var assemblyFile in assemblyFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(assemblyFile);
                    DiscoverFromAssembly(assembly, validators);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load assembly {AssemblyFile}", assemblyFile);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering validators from directory {Directory}", directory);
        }
    }
}

