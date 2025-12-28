using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Utils;
using SGuard.ConfigValidation.Validators;
using FileValidator = SGuard.ConfigValidation.Services.FileValidator;

namespace SGuard.ConfigValidation.Tests.Validators;

public sealed class FileValidatorTests : IDisposable
{
    private readonly FileValidator _fileValidator;
    private readonly string _testDirectory;

    public FileValidatorTests()
    {
        var validatorFactoryLogger = NullLogger<ValidatorFactory>.Instance;
        var validatorFactory = new ValidatorFactory(validatorFactoryLogger);
        var fileValidatorLogger = NullLogger<FileValidator>.Instance;
        _fileValidator = new FileValidator(validatorFactory, fileValidatorLogger);
        _testDirectory = DirectoryUtility.CreateTempDirectory("filevalidator-test");
    }

    [Fact]
    public void ValidateFile_With_ValidFile_Should_ReturnSuccess()
    {
        // Arrange
        var appSettings = new Dictionary<string, object>
        {
            { "ConnectionStrings:DefaultConnection", "Server=localhost;" },
            { "Logging:LogLevel", "Information" }
        };

        var rules = new List<Rule>
        {
            new()
            {
                Id = "test-rule",
                Environments = ["dev"],
                RuleDetail = new RuleDetail
                {
                    Id = "test-detail",
                    Conditions =
                    [
                        new Condition
                        {
                            Key = "ConnectionStrings:DefaultConnection",
                            Validators =
                            [
                                new ValidatorCondition
                                {
                                    Validator = "required",
                                    Message = "Connection string is required"
                                }
                            ]
                        }
                    ]
                }
            }
        };

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ErrorCount.Should().Be(0);
        result.Results.Should().HaveCount(1);
    }

    [Fact]
    public void ValidateFile_With_MissingKey_Should_ReturnFailure()
    {
        // Arrange
        var appSettings = new Dictionary<string, object>
        {
            { "Logging:LogLevel", "Information" }
        };

        var rules = new List<Rule>
        {
            new()
            {
                Id = "test-rule",
                Environments = ["dev"],
                RuleDetail = new RuleDetail
                {
                    Id = "test-detail",
                    Conditions =
                    [
                        new Condition
                        {
                            Key = "ConnectionStrings:DefaultConnection",
                            Validators =
                            [
                                new ValidatorCondition
                                {
                                    Validator = "required",
                                    Message = "Connection string is required"
                                }
                            ]
                        }
                    ]
                }
            }
        };

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Key.Should().Be("ConnectionStrings:DefaultConnection");
        result.Errors[0].ValidatorType.Should().Be("required");
    }

    [Fact]
    public void ValidateFile_With_MultipleRules_Should_ValidateAll()
    {
        // Arrange
        var appSettings = new Dictionary<string, object>
        {
            { "ConnectionStrings:DefaultConnection", "Server=localhost;" },
            { "Logging:LogLevel", "Information" }
        };

        var rules = new List<Rule>
        {
            new()
            {
                Id = "rule1",
                Environments = ["dev"],
                RuleDetail = new RuleDetail
                {
                    Id = "detail1",
                    Conditions =
                    [
                        new Condition
                        {
                            Key = "ConnectionStrings:DefaultConnection",
                            Validators =
                            [
                                new ValidatorCondition
                                {
                                    Validator = "required",
                                    Message = "Connection string is required"
                                }
                            ]
                        }
                    ]
                }
            },
            new()
            {
                Id = "rule2",
                Environments = ["dev"],
                RuleDetail = new RuleDetail
                {
                    Id = "detail2",
                    Conditions =
                    [
                        new Condition
                        {
                            Key = "Logging:LogLevel",
                            Validators =
                            [
                                new ValidatorCondition
                                {
                                    Validator = "required",
                                    Message = "Log level is required"
                                }
                            ]
                        }
                    ]
                }
            }
        };

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Results.Should().HaveCount(2);
    }

    [Fact]
    public void ValidateFile_With_MultipleValidatorsPerCondition_Should_ValidateAll()
    {
        // Arrange
        var appSettings = new Dictionary<string, object>
        {
            { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=Test;" }
        };

        var rules = new List<Rule>
        {
            new()
            {
                Id = "test-rule",
                Environments = ["dev"],
                RuleDetail = new RuleDetail
                {
                    Id = "test-detail",
                    Conditions =
                    [
                        new Condition
                        {
                            Key = "ConnectionStrings:DefaultConnection",
                            Validators =
                            [
                                new ValidatorCondition
                                {
                                    Validator = "required",
                                    Message = "Connection string is required"
                                },
                                new ValidatorCondition
                                {
                                    Validator = "min_len",
                                    Value = 10,
                                    Message = "Connection string must be at least 10 characters"
                                }
                            ]
                        }
                    ]
                }
            }
        };

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Results.Should().HaveCount(2);
    }

    [Fact]
    public void ValidateFile_With_NullFilePath_Should_ThrowArgumentException()
    {
        // Arrange
        var rules = new List<Rule>();
        var appSettings = new Dictionary<string, object>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            _fileValidator.ValidateFile(null!, rules, appSettings));
        exception.ParamName.Should().Be("filePath");
    }

    [Fact]
    public void ValidateFile_With_NullRules_Should_ThrowArgumentNullException()
    {
        // Arrange
        var appSettings = new Dictionary<string, object>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            _fileValidator.ValidateFile("test.json", null!, appSettings));
        exception.ParamName.Should().Be("applicableRules");
    }

    [Fact]
    public void ValidateFile_With_NullAppSettings_Should_ThrowArgumentNullException()
    {
        // Arrange
        var rules = new List<Rule> { new Rule { Id = "test", Environments = ["dev"], RuleDetail = new RuleDetail { Id = "test-detail", Conditions = [] } } };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            _fileValidator.ValidateFile("test.json", rules, null!));
        exception.ParamName.Should().Be("appSettings");
    }

    [Fact]
    public void ValidateFile_With_NullRule_Should_AddError()
    {
        // Arrange
        var appSettings = new Dictionary<string, object>();
        var rules = new List<Rule> { null! };

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorCount.Should().Be(1);
        result.Errors[0].Message.Should().Contain("Rule cannot be null");
    }


    [Fact]
    public void ValidateFile_With_UnsupportedValidator_Should_AddError()
    {
        // Arrange
        var appSettings = new Dictionary<string, object>
        {
            { "Test:Key", "value" }
        };

        var rules = new List<Rule>
        {
            new()
            {
                Id = "test-rule",
                Environments = ["dev"],
                RuleDetail = new RuleDetail
                {
                    Id = "test-detail",
                    Conditions =
                    [
                        new Condition
                        {
                            Key = "Test:Key",
                            Validators =
                            [
                                new ValidatorCondition
                                {
                                    Validator = "unsupported_validator",
                                    Message = "Test message"
                                }
                            ]
                        }
                    ]
                }
            }
        };

        // Act
        var result = _fileValidator.ValidateFile("test.json", rules, appSettings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorCount.Should().Be(1);
        result.Errors[0].Message.Should().Contain("Unsupported validator type");
    }

    public void Dispose()
    {
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
    }
}

