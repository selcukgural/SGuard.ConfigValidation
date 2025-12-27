namespace SGuard.ConfigValidation.Validators;

/// <summary>
/// Constants for validator type names and related collections.
/// </summary>
public static class ValidatorConstants
{
    /// <summary>
    /// All built-in validator type names.
    /// </summary>
    public static readonly IReadOnlyList<string> AllValidatorTypes =
    [
        Required,
        MinLength,
        MaxLength,
        EqualsValidator,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        In
    ];

    /// <summary>
    /// Validator types that require a value to be specified.
    /// </summary>
    public static readonly IReadOnlyList<string> ValidatorsRequiringValue =
    [
        MinLength,
        MaxLength,
        EqualsValidator,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        In
    ];

    // Individual validator type constants
    public const string Required = "required";
    public const string MinLength = "min_len";
    public const string MaxLength = "max_len";
    public const string EqualsValidator = "eq";
    public const string NotEqual = "ne";
    public const string GreaterThan = "gt";
    public const string GreaterThanOrEqual = "gte";
    public const string LessThan = "lt";
    public const string LessThanOrEqual = "lte";
    public const string In = "in";
}

