using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Resources;
using SGuard.ConfigValidation.Validators.Plugin;
using static SGuard.ConfigValidation.Common.Throw;

namespace SGuard.ConfigValidation.Validators;

public sealed class ValidatorFactory : IValidatorFactory
{
    /// <summary>
    /// Dictionary containing all built-in validators that are always available in the system.
    /// The key is the validator type name (case-insensitive), and the value is the corresponding <see cref="IValidator{T}"/> instance.
    /// </summary>
    /// <remarks>
    /// This dictionary is initialized with a fixed set of validators covering common validation scenarios such as required, length, equality, comparison, and set membership.
    /// The dictionary uses <see cref="StringComparer.OrdinalIgnoreCase"/> to allow case-insensitive lookups.
    /// </remarks>
    private static readonly Dictionary<string, IValidator<object>> BuiltInValidators = new(10, StringComparer.OrdinalIgnoreCase)
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
    private readonly string _supportedValidatorsString;

    /// <summary>
    /// Initializes a new instance of the ValidatorFactory with built-in validators and optional plugin discovery.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="pluginDiscovery">Optional plugin discovery service. If provided, plugins will be discovered.</param>
    /// <param name="pluginDirectories">Optional list of directories to scan for validator plugins.</param>
    public ValidatorFactory(ILogger<ValidatorFactory> logger, ValidatorPluginDiscovery? pluginDiscovery = null,
                            IEnumerable<string>? pluginDirectories = null)
    {
        System.ArgumentNullException.ThrowIfNull(logger);

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
                    logger.LogWarning("Plugin validator '{ValidatorType}' conflicts with built-in validator, using built-in", pluginValidator.Key);
                }
                else
                {
                    validators[pluginValidator.Key] = pluginValidator.Value;
                }
            }
        }

        _validators = new ReadOnlyDictionary<string, IValidator<object>>(validators);
        _supportedValidatorsString = string.Join(", ", _validators.Keys);
    }

    /// <summary>
    /// Gets a validator instance for the specified validator type.
    /// </summary>
    /// <param name="validatorType">The validator type name (e.g., "required", "eq", "gt"). Case-insensitive.</param>
    /// <returns>An <see cref="IValidator{T}"/> instance for the specified validator type.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="validatorType"/> is null.</exception>
    /// <exception cref="System.NotSupportedException">Thrown when the validator type is not supported.</exception>
    /// <example>
    /// <code>
    /// using Microsoft.Extensions.Logging;
    /// using Microsoft.Extensions.Logging.Abstractions;
    /// using SGuard.ConfigValidation.Models;
    /// using SGuard.ConfigValidation.Validators;
    /// 
    /// var logger = NullLogger&lt;ValidatorFactory&gt;.Instance;
    /// var validatorFactory = new ValidatorFactory(logger);
    /// 
    /// // Get a validator instance
    /// var requiredValidator = validatorFactory.GetValidator("required");
    /// 
    /// // Use the validator
    /// var condition = new ValidatorCondition
    /// {
    ///     Validator = "required",
    ///     Message = "Value is required"
    /// };
    /// 
    /// var result = requiredValidator.Validate("some value", condition);
    /// if (!result.IsValid)
    /// {
    ///     Console.WriteLine($"Validation failed: {result.Message}");
    /// }
    /// </code>
    /// </example>
    public IValidator<object> GetValidator(string validatorType)
    {
        System.ArgumentNullException.ThrowIfNull(validatorType);

        // Fast path: direct lookup with an optimized exception message
        return _validators.TryGetValue(validatorType, out var validator)
                   ? validator
                   // Slow path: throw an exception with a cached message
                   : throw NotSupportedException(nameof(SR.NotSupportedException_ValidatorTypeNotSupported), validatorType, _supportedValidatorsString);
    }

    /// <summary>
    /// Gets the list of supported validator types.
    /// </summary>
    /// <returns>An enumerable of supported validator type names (e.g., "required", "eq", "gt").</returns>
    public IEnumerable<string> GetSupportedValidators() => _validators.Keys;
}