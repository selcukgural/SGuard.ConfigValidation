using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Validators.Plugin;

namespace SGuard.ConfigValidation.Validators;

public sealed class ValidatorFactory : IValidatorFactory
{
    // Built-in validators (always available)
    private static readonly Dictionary<string, IValidator<object>> BuiltInValidators = 
        new Dictionary<string, IValidator<object>>(10, StringComparer.OrdinalIgnoreCase)
        {
            [ValidatorConstants.Required] = new RequiredValidator(),
            [ValidatorConstants.MinLength] = new MinLengthValidator(),
            [ValidatorConstants.MaxLength] = new MaxLengthValidator(),
            [ValidatorConstants.EqualsValidator] = new EqualsValidator(),
            [ValidatorConstants.GreaterThan] = new GreaterThanValidator(),
            [ValidatorConstants.GreaterThanOrEqual] = new GreaterThanOrEqualValidator(),
            [ValidatorConstants.In] = new InValidator(),
            [ValidatorConstants.LessThan] = new LessThanValidator(),
            [ValidatorConstants.LessThanOrEqual] = new LessThanOrEqualValidator(),
            [ValidatorConstants.NotEqual] = new NotEqualValidator()
        };

    private readonly ReadOnlyDictionary<string, IValidator<object>> _validators;
    private readonly IReadOnlyList<string> _supportedValidatorsList;
    private readonly string _supportedValidatorsString;
    private readonly ILogger<ValidatorFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the ValidatorFactory with built-in validators and optional plugin discovery.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="pluginDiscovery">Optional plugin discovery service. If provided, plugins will be discovered.</param>
    /// <param name="pluginDirectories">Optional list of directories to scan for validator plugins.</param>
    public ValidatorFactory(ILogger<ValidatorFactory> logger, ValidatorPluginDiscovery? pluginDiscovery = null, IEnumerable<string>? pluginDirectories = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Start with built-in validators
        var validators = new Dictionary<string, IValidator<object>>(BuiltInValidators, StringComparer.OrdinalIgnoreCase);

        // Discover and add plugin validators
        if (pluginDiscovery != null && pluginDirectories != null)
        {
            var pluginValidators = pluginDiscovery.DiscoverValidators(pluginDirectories);
            
            foreach (var pluginValidator in pluginValidators)
            {
                if (validators.ContainsKey(pluginValidator.Key))
                {
                    _logger.LogWarning("Plugin validator '{ValidatorType}' conflicts with built-in validator, using built-in", pluginValidator.Key);
                }
                else
                {
                    validators[pluginValidator.Key] = pluginValidator.Value;
                    _logger.LogDebug("Registered plugin validator: {ValidatorType}", pluginValidator.Key);
                }
            }
        }

        _validators = new ReadOnlyDictionary<string, IValidator<object>>(validators);
        _supportedValidatorsList = _validators.Keys.ToArray();
        _supportedValidatorsString = string.Join(", ", _supportedValidatorsList);
    }

    /// <summary>
    /// Gets a validator instance for the specified validator type.
    /// </summary>
    /// <param name="validatorType">The validator type name (e.g., "required", "eq", "gt"). Case-insensitive.</param>
    /// <returns>An <see cref="IValidator{T}"/> instance for the specified validator type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validatorType"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the validator type is not supported.</exception>
    public IValidator<object> GetValidator(string validatorType)
    {
        // Fast path: direct lookup with optimized exception message
        if (_validators.TryGetValue(validatorType, out var validator))
        {
            return validator;
        }
        
        // Slow path: throw exception with cached message
        throw new NotSupportedException($"Validator type '{validatorType}' is not supported. Supported validators: {_supportedValidatorsString}");
    }

    /// <summary>
    /// Gets the list of supported validator types.
    /// </summary>
    /// <returns>An enumerable of supported validator type names (e.g., "required", "eq", "gt").</returns>
    public IEnumerable<string> GetSupportedValidators() => _supportedValidatorsList;
}