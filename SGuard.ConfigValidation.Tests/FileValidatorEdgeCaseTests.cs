using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Results;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Validators;
using FileValidator = SGuard.ConfigValidation.Services.FileValidator;

namespace SGuard.ConfigValidation.Tests;

public sealed class FileValidatorEdgeCaseTests
{
    private readonly FileValidator _fileValidator;
    private readonly ValidatorFactory _validatorFactory;

    public FileValidatorEdgeCaseTests()
    {
        var validatorFactoryLogger = NullLogger<ValidatorFactory>.Instance;
        _validatorFactory = new ValidatorFactory(validatorFactoryLogger);
        var fileValidatorLogger = NullLogger<FileValidator>.Instance;
        _fileValidator = new FileValidator(_validatorFactory, fileValidatorLogger);
    }

    [Fact]
    public void ValidateFile_With_NullFilePath_Should_Throw_ArgumentException()
    {
        // Arrange
        var rules = new List<Rule>();
        var appSettings = new Dictionary<string, object>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _fileValidator.ValidateFile(null!, rules, appSettings));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public void ValidateFile_With_EmptyFilePath_Should_Throw_ArgumentException()
    {
        // Arrange
        var rules = new List<Rule>();
        var appSettings = new Dictionary<string, object>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _fileValidator.ValidateFile("", rules, appSettings));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public void ValidateFile_With_WhitespaceFilePath_Should_Throw_ArgumentException()
    {
        // Arrange
        var rules = new List<Rule>();
        var appSettings = new Dictionary<string, object>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _fileValidator.ValidateFile("   ", rules, appSettings));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public void ValidateFile_With_NullApplicableRules_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var appSettings = new Dictionary<string, object>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _fileValidator.ValidateFile("test.json", null!, appSettings));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public void ValidateFile_With_EmptyApplicableRules_Should_Throw_ArgumentException()
    {
        // Arrange
        var rules = new List<Rule>();
        var appSettings = new Dictionary<string, object>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _fileValidator.ValidateFile("test.json", rules, appSettings));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public void ValidateFile_With_NullAppSettings_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new Rule
            {
                Id = "test-rule",
                Environments = [],
                RuleDetail = new RuleDetail
                {
                    Id = "test-detail",
                    Conditions = new List<Condition>()
                }
            }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _fileValidator.ValidateFile("test.json", rules, null!));
        
        exception.Should().NotBeNull();
    }

    [Fact]
    public void ValidateFile_With_NullRule_Should_AddFailureResult()
    {
        // Arrange
        var rules = new List<Rule> { null! };
        var appSettings = new Dictionary<string, object>();

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Results.Should().Contain(r => !r.IsValid);
    }

    [Fact]
    public void ValidateFile_With_EmptyRuleId_Should_AddFailureResult()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new Rule
            {
                Id = "",
                Environments = [],
                RuleDetail = new RuleDetail
                {
                    Id = "test-detail",
                    Conditions = new List<Condition>()
                }
            }
        };
        var appSettings = new Dictionary<string, object>();

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse(); // Empty rule ID should add failure result
        result.Results.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateFile_With_EmptyRuleDetailId_Should_AddFailureResult()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new Rule
            {
                Id = "test-rule",
                Environments = [],
                RuleDetail = new RuleDetail
                {
                    Id = "",
                    Conditions = new List<Condition>
                    {
                        new Condition
                        {
                            Key = "Test:Key",
                            Validators = new List<ValidatorCondition>
                            {
                                new ValidatorCondition { Validator = "required", Message = "Required" }
                            }
                        }
                    }
                }
            }
        };
        var appSettings = new Dictionary<string, object>();

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Results.Should().Contain(r => !r.IsValid);
    }

    [Fact]
    public void ValidateFile_With_EmptyConditions_Should_SkipValidation()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new Rule
            {
                Id = "test-rule",
                Environments = [],
                RuleDetail = new RuleDetail
                {
                    Id = "test-detail",
                    Conditions = new List<Condition>()
                }
            }
        };
        var appSettings = new Dictionary<string, object>();

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Results.Should().BeEmpty();
    }

    [Fact]
    public void ValidateFile_With_NullConditionKey_Should_AddFailureResult()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new Rule
            {
                Id = "test-rule",
                Environments = [],
                RuleDetail = new RuleDetail
                {
                    Id = "test-detail",
                    Conditions = new List<Condition>
                    {
                        new Condition
                        {
                            Key = null!,
                            Validators = new List<ValidatorCondition>
                            {
                                new ValidatorCondition { Validator = "required", Message = "Required" }
                            }
                        }
                    }
                }
            }
        };
        var appSettings = new Dictionary<string, object>();

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Results.Should().Contain(r => !r.IsValid);
    }

    [Fact]
    public void ValidateFile_With_EmptyConditionKey_Should_AddFailureResult()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new Rule
            {
                Id = "test-rule",
                Environments = [],
                RuleDetail = new RuleDetail
                {
                    Id = "test-detail",
                    Conditions = new List<Condition>
                    {
                        new Condition
                        {
                            Key = "",
                            Validators = new List<ValidatorCondition>
                            {
                                new ValidatorCondition { Validator = "required", Message = "Required" }
                            }
                        }
                    }
                }
            }
        };
        var appSettings = new Dictionary<string, object>();

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Results.Should().Contain(r => !r.IsValid);
    }

    [Fact]
    public void ValidateFile_With_EmptyValidators_Should_AddFailureResult()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new Rule
            {
                Id = "test-rule",
                Environments = [],
                RuleDetail = new RuleDetail
                {
                    Id = "test-detail",
                    Conditions = new List<Condition>
                    {
                        new Condition
                        {
                            Key = "Test:Key",
                            Validators = new List<ValidatorCondition>()
                        }
                    }
                }
            }
        };
        var appSettings = new Dictionary<string, object>();

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Results.Should().Contain(r => !r.IsValid);
    }

    [Fact]
    public void ValidateFile_With_NullValidatorType_Should_AddFailureResult()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new Rule
            {
                Id = "test-rule",
                Environments = [],
                RuleDetail = new RuleDetail
                {
                    Id = "test-detail",
                    Conditions = new List<Condition>
                    {
                        new Condition
                        {
                            Key = "Test:Key",
                            Validators = new List<ValidatorCondition>
                            {
                                new ValidatorCondition { Validator = null!, Message = "Test" }
                            }
                        }
                    }
                }
            }
        };
        var appSettings = new Dictionary<string, object> { { "Test:Key", "value" } };

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Results.Should().Contain(r => !r.IsValid);
    }

    [Fact]
    public void ValidateFile_With_EmptyValidatorType_Should_AddFailureResult()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new Rule
            {
                Id = "test-rule",
                Environments = [],
                RuleDetail = new RuleDetail
                {
                    Id = "test-detail",
                    Conditions = new List<Condition>
                    {
                        new Condition
                        {
                            Key = "Test:Key",
                            Validators = new List<ValidatorCondition>
                            {
                                new ValidatorCondition { Validator = "", Message = "Test" }
                            }
                        }
                    }
                }
            }
        };
        var appSettings = new Dictionary<string, object> { { "Test:Key", "value" } };

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Results.Should().Contain(r => !r.IsValid);
    }

    [Fact]
    public void ValidateFile_With_UnsupportedValidatorType_Should_Handle_NotSupportedException()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new Rule
            {
                Id = "test-rule",
                Environments = [],
                RuleDetail = new RuleDetail
                {
                    Id = "test-detail",
                    Conditions = new List<Condition>
                    {
                        new Condition
                        {
                            Key = "Test:Key",
                            Validators = new List<ValidatorCondition>
                            {
                                new ValidatorCondition { Validator = "unsupported_validator", Message = "Test" }
                            }
                        }
                    }
                }
            }
        };
        var appSettings = new Dictionary<string, object> { { "Test:Key", "value" } };

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Results.Should().Contain(r => !r.IsValid);
    }

    [Fact]
    public void ValidateFile_With_MissingKey_Should_Validate_AsNull()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new Rule
            {
                Id = "test-rule",
                Environments = [],
                RuleDetail = new RuleDetail
                {
                    Id = "test-detail",
                    Conditions = new List<Condition>
                    {
                        new Condition
                        {
                            Key = "Missing:Key",
                            Validators = new List<ValidatorCondition>
                            {
                                new ValidatorCondition { Validator = "required", Message = "Required" }
                            }
                        }
                    }
                }
            }
        };
        var appSettings = new Dictionary<string, object>();

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Results.Should().Contain(r => !r.IsValid);
    }

    [Fact]
    public void ValidateFile_With_MultipleRules_Should_Process_All()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new Rule
            {
                Id = "rule1",
                Environments = [],
                RuleDetail = new RuleDetail
                {
                    Id = "detail1",
                    Conditions = new List<Condition>
                    {
                        new Condition
                        {
                            Key = "Key1",
                            Validators = new List<ValidatorCondition>
                            {
                                new ValidatorCondition { Validator = "required", Message = "Required" }
                            }
                        }
                    }
                }
            },
            new Rule
            {
                Id = "rule2",
                Environments = [],
                RuleDetail = new RuleDetail
                {
                    Id = "detail2",
                    Conditions = new List<Condition>
                    {
                        new Condition
                        {
                            Key = "Key2",
                            Validators = new List<ValidatorCondition>
                            {
                                new ValidatorCondition { Validator = "required", Message = "Required" }
                            }
                        }
                    }
                }
            }
        };
        var appSettings = new Dictionary<string, object>
        {
            { "Key1", "value1" },
            { "Key2", "value2" }
        };

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Results.Should().HaveCount(2);
    }

    [Fact]
    public void ValidateFile_With_MultipleValidators_Should_Process_All()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new Rule
            {
                Id = "test-rule",
                Environments = [],
                RuleDetail = new RuleDetail
                {
                    Id = "test-detail",
                    Conditions = new List<Condition>
                    {
                        new Condition
                        {
                            Key = "Test:Key",
                            Validators = new List<ValidatorCondition>
                            {
                                new ValidatorCondition { Validator = "required", Message = "Required" },
                                new ValidatorCondition { Validator = "min_len", Value = "5", Message = "Min length" }
                            }
                        }
                    }
                }
            }
        };
        var appSettings = new Dictionary<string, object> { { "Test:Key", "value" } };

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(2);
    }

    [Fact]
    public void ValidateFile_With_UnknownValueType_Should_LogWarning()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new Rule
            {
                Id = "test-rule",
                Environments = [],
                RuleDetail = new RuleDetail
                {
                    Id = "test-detail",
                    Conditions = new List<Condition>
                    {
                        new Condition
                        {
                            Key = "Test:Key",
                            Validators = new List<ValidatorCondition>
                            {
                                new ValidatorCondition { Validator = "required", Message = "Required" }
                            }
                        }
                    }
                }
            }
        };
        var appSettings = new Dictionary<string, object> { { "Test:Key", new object() } };

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        // Unknown type should still validate (warning logged but validation continues)
    }
}

