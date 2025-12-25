using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Validators;
using Environment = SGuard.ConfigValidation.Models.Environment;

namespace SGuard.ConfigValidation.Tests;

public sealed class ConfigValidatorTests
{
    private readonly ConfigValidator _validator;
    private readonly IValidatorFactory _validatorFactory;

    public ConfigValidatorTests()
    {
        var validatorFactoryLogger = NullLogger<ValidatorFactory>.Instance;
        _validatorFactory = new ValidatorFactory(validatorFactoryLogger);
        var configValidatorLogger = NullLogger<ConfigValidator>.Instance;
        _validator = new ConfigValidator(_validatorFactory, configValidatorLogger);
    }

    [Fact]
    public void Validate_With_ValidConfig_Should_Return_EmptyErrors()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "1",
            Environments = new List<Environment>
            {
                new() { Id = "dev", Name = "Development", Path = "appsettings.Development.json" },
                new() { Id = "prod", Name = "Production", Path = "appsettings.Production.json" }
            },
            Rules = new List<Rule>
            {
                new()
                {
                    Id = "rule1",
                    Environments = new List<string> { "dev", "prod" },
                    RuleDetail = new RuleDetail
                    {
                        Id = "rule-detail-1",
                        Conditions = new List<Condition>
                        {
                            new()
                            {
                                Key = "ConnectionStrings:DefaultConnection",
                                Validators = new List<ValidatorCondition>
                                {
                                    new() { Validator = "required", Message = "Connection string is required" }
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_With_DuplicateEnvironmentIds_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "1",
            Environments = new List<Environment>
            {
                new() { Id = "dev", Name = "Development", Path = "appsettings.Development.json" },
                new() { Id = "dev", Name = "Development2", Path = "appsettings.Development2.json" }
            },
            Rules = new List<Rule>()
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("Duplicate environment ID") && e.Contains("dev"));
    }

    [Fact]
    public void Validate_With_DuplicateRuleIds_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "1",
            Environments = new List<Environment>
            {
                new() { Id = "dev", Name = "Development", Path = "appsettings.Development.json" }
            },
            Rules = new List<Rule>
            {
                new() { Id = "rule1", Environments = new List<string> { "dev" }, RuleDetail = new RuleDetail { Id = "detail1", Conditions = new List<Condition>() } },
                new() { Id = "rule1", Environments = new List<string> { "dev" }, RuleDetail = new RuleDetail { Id = "detail2", Conditions = new List<Condition>() } }
            }
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("Duplicate rule ID") && e.Contains("rule1"));
    }

    [Fact]
    public void Validate_With_EmptyEnvironmentId_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "1",
            Environments = new List<Environment>
            {
                new() { Id = "", Name = "Development", Path = "appsettings.Development.json" }
            },
            Rules = new List<Rule>()
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("'id' is required") && e.Contains("Environment"));
    }

    [Fact]
    public void Validate_With_EmptyEnvironmentPath_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "1",
            Environments = new List<Environment>
            {
                new() { Id = "dev", Name = "Development", Path = "" }
            },
            Rules = new List<Rule>()
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("'path' is required") && e.Contains("Environment"));
    }

    [Fact]
    public void Validate_With_EmptyEnvironmentName_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "1",
            Environments = new List<Environment>
            {
                new() { Id = "dev", Name = "", Path = "appsettings.Development.json" }
            },
            Rules = new List<Rule>()
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("'name' is required") && e.Contains("Environment"));
    }

    [Fact]
    public void Validate_With_RuleReferencingNonExistentEnvironment_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "1",
            Environments = new List<Environment>
            {
                new() { Id = "dev", Name = "Development", Path = "appsettings.Development.json" }
            },
            Rules = new List<Rule>
            {
                new()
                {
                    Id = "rule1",
                    Environments = new List<string> { "nonexistent" },
                    RuleDetail = new RuleDetail
                    {
                        Id = "detail1",
                        Conditions = new List<Condition>()
                    }
                }
            }
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("references environment ID 'nonexistent'") && e.Contains("does not exist"));
    }

    [Fact]
    public void Validate_With_EmptyRuleEnvironments_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "1",
            Environments = new List<Environment>
            {
                new() { Id = "dev", Name = "Development", Path = "appsettings.Development.json" }
            },
            Rules = new List<Rule>
            {
                new()
                {
                    Id = "rule1",
                    Environments = new List<string>(),
                    RuleDetail = new RuleDetail
                    {
                        Id = "detail1",
                        Conditions = new List<Condition>()
                    }
                }
            }
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("'environments' array is required") && e.Contains("at least one environment ID"));
    }

    [Fact]
    public void Validate_With_UnsupportedValidator_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "1",
            Environments = new List<Environment>
            {
                new() { Id = "dev", Name = "Development", Path = "appsettings.Development.json" }
            },
            Rules = new List<Rule>
            {
                new()
                {
                    Id = "rule1",
                    Environments = new List<string> { "dev" },
                    RuleDetail = new RuleDetail
                    {
                        Id = "detail1",
                        Conditions = new List<Condition>
                        {
                            new()
                            {
                                Key = "Test:Key",
                                Validators = new List<ValidatorCondition>
                                {
                                    new() { Validator = "unsupported_validator", Message = "Test message" }
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("'unsupported_validator' is not supported"));
    }

    [Fact]
    public void Validate_With_MissingValidatorValue_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "1",
            Environments = new List<Environment>
            {
                new() { Id = "dev", Name = "Development", Path = "appsettings.Development.json" }
            },
            Rules = new List<Rule>
            {
                new()
                {
                    Id = "rule1",
                    Environments = new List<string> { "dev" },
                    RuleDetail = new RuleDetail
                    {
                        Id = "detail1",
                        Conditions = new List<Condition>
                        {
                            new()
                            {
                                Key = "Test:Key",
                                Validators = new List<ValidatorCondition>
                                {
                                    new() { Validator = "min_len", Message = "Test message", Value = null }
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("'value' is required") && e.Contains("min_len"));
    }

    [Fact]
    public void Validate_With_EmptyValidatorMessage_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "1",
            Environments = new List<Environment>
            {
                new() { Id = "dev", Name = "Development", Path = "appsettings.Development.json" }
            },
            Rules = new List<Rule>
            {
                new()
                {
                    Id = "rule1",
                    Environments = new List<string> { "dev" },
                    RuleDetail = new RuleDetail
                    {
                        Id = "detail1",
                        Conditions = new List<Condition>
                        {
                            new()
                            {
                                Key = "Test:Key",
                                Validators = new List<ValidatorCondition>
                                {
                                    new() { Validator = "required", Message = "" }
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("'message' is required"));
    }

    [Fact]
    public void Validate_With_EmptyConditionKey_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "1",
            Environments = new List<Environment>
            {
                new() { Id = "dev", Name = "Development", Path = "appsettings.Development.json" }
            },
            Rules = new List<Rule>
            {
                new()
                {
                    Id = "rule1",
                    Environments = new List<string> { "dev" },
                    RuleDetail = new RuleDetail
                    {
                        Id = "detail1",
                        Conditions = new List<Condition>
                        {
                            new()
                            {
                                Key = "",
                                Validators = new List<ValidatorCondition>
                                {
                                    new() { Validator = "required", Message = "Test message" }
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("'key' is required") && e.Contains("condition"));
    }

    [Fact]
    public void Validate_With_EmptyConditions_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "1",
            Environments = new List<Environment>
            {
                new() { Id = "dev", Name = "Development", Path = "appsettings.Development.json" }
            },
            Rules = new List<Rule>
            {
                new()
                {
                    Id = "rule1",
                    Environments = new List<string> { "dev" },
                    RuleDetail = new RuleDetail
                    {
                        Id = "detail1",
                        Conditions = new List<Condition>()
                    }
                }
            }
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("'conditions' array is required") && e.Contains("at least one condition"));
    }

    [Fact]
    public void Validate_With_EmptyRuleDetailId_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "1",
            Environments = new List<Environment>
            {
                new() { Id = "dev", Name = "Development", Path = "appsettings.Development.json" }
            },
            Rules = new List<Rule>
            {
                new()
                {
                    Id = "rule1",
                    Environments = new List<string> { "dev" },
                    RuleDetail = new RuleDetail
                    {
                        Id = "",
                        Conditions = new List<Condition>
                        {
                            new()
                            {
                                Key = "Test:Key",
                                Validators = new List<ValidatorCondition>
                                {
                                    new() { Validator = "required", Message = "Test message" }
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("'id' is required") && e.Contains("rule"));
    }

    [Fact]
    public void Validate_With_NullRuleDetail_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "1",
            Environments = new List<Environment>
            {
                new() { Id = "dev", Name = "Development", Path = "appsettings.Development.json" }
            },
            Rules = new List<Rule>
            {
                new()
                {
                    Id = "rule1",
                    Environments = new List<string> { "dev" },
                    RuleDetail = null!
                }
            }
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("'rule' object is required"));
    }

    [Fact]
    public void Validate_With_NoEnvironments_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "1",
            Environments = new List<Environment>(),
            Rules = new List<Rule>()
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("at least one environment"));
    }

    [Fact]
    public void Validate_With_EmptyVersion_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = "",
            Environments = new List<Environment>
            {
                new() { Id = "dev", Name = "Development", Path = "appsettings.Development.json" }
            },
            Rules = new List<Rule>()
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("version") && e.Contains("required"));
    }

    [Fact]
    public void Validate_With_NullVersion_Should_Return_Error()
    {
        // Arrange
        var config = new SGuardConfig
        {
            Version = null!,
            Environments = new List<Environment>
            {
                new() { Id = "dev", Name = "Development", Path = "appsettings.Development.json" }
            },
            Rules = new List<Rule>()
        };

        // Act
        var errors = _validator.Validate(config, _validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("version") && e.Contains("required"));
    }
}

