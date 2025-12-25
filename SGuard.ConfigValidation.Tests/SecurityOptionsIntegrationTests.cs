using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Exceptions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Validators;
using Environment = SGuard.ConfigValidation.Models.Environment;

namespace SGuard.ConfigValidation.Tests;

public sealed class SecurityOptionsIntegrationTests : IDisposable
{
    private readonly string _tempDirectory;

    public SecurityOptionsIntegrationTests()
    {
        _tempDirectory = SafeFileSystem.CreateSafeTempDirectory("SecurityOptionsIntegrationTests");
    }

    [Fact]
    public void ConfigLoader_With_CustomSecurityOptions_Should_Enforce_MaxFileSizeLimit()
    {
        // Arrange
        var securityOptions = Options.Create(new SecurityOptions
        {
            MaxFileSizeBytes = 1024 // 1 KB - very small limit for testing
        });
        var logger = NullLogger<ConfigLoader>.Instance;
        var loader = new ConfigLoader(logger, securityOptions);

        // Create a file larger than the limit
        var configPath = Path.Combine(_tempDirectory, "large-config.json");
        var largeContent = new string('A', 2048); // 2 KB
        File.WriteAllText(configPath, $@"{{""version"": ""1"", ""environments"": [], ""rules"": [], ""extra"": ""{largeContent}""}}");

        // Act & Assert
        var action = () => loader.LoadConfig(configPath);
        action.Should().Throw<ConfigurationException>()
            .WithMessage("*exceeds security limit*");
    }

    [Fact]
    public void ConfigLoader_With_CustomSecurityOptions_Should_Enforce_MaxEnvironmentsLimit()
    {
        // Arrange
        var securityOptions = Options.Create(new SecurityOptions
        {
            MaxEnvironmentsCount = 2 // Very small limit for testing
        });
        var logger = NullLogger<ConfigLoader>.Instance;
        var loader = new ConfigLoader(logger, securityOptions);

        var configPath = Path.Combine(_tempDirectory, "many-environments.json");
        var configJson = @"{
            ""version"": ""1"",
            ""environments"": [
                {""id"": ""env1"", ""name"": ""Environment 1"", ""path"": ""appsettings.env1.json""},
                {""id"": ""env2"", ""name"": ""Environment 2"", ""path"": ""appsettings.env2.json""},
                {""id"": ""env3"", ""name"": ""Environment 3"", ""path"": ""appsettings.env3.json""}
            ],
            ""rules"": []
        }";
        File.WriteAllText(configPath, configJson);

        // Act & Assert
        var action = () => loader.LoadConfig(configPath);
        action.Should().Throw<ConfigurationException>()
            .WithMessage("*exceeds security limit*");
    }

    [Fact]
    public void ConfigLoader_With_CustomSecurityOptions_Should_Enforce_MaxRulesLimit()
    {
        // Arrange
        var securityOptions = Options.Create(new SecurityOptions
        {
            MaxRulesCount = 2 // Very small limit for testing
        });
        var logger = NullLogger<ConfigLoader>.Instance;
        var loader = new ConfigLoader(logger, securityOptions);

        var configPath = Path.Combine(_tempDirectory, "many-rules.json");
        var rules = string.Join(",", Enumerable.Range(1, 3).Select(i => $@"{{
            ""id"": ""rule{i}"",
            ""environments"": [""dev""],
            ""rule"": {{
                ""id"": ""rule-detail-{i}"",
                ""conditions"": [{{
                    ""key"": ""Test:Key{i}"",
                    ""condition"": [{{
                        ""validator"": ""required"",
                        ""message"": ""Test message""
                    }}]
                }}]
            }}
        }}"));
        var configJson = $@"{{
            ""version"": ""1"",
            ""environments"": [{{""id"": ""dev"", ""name"": ""Development"", ""path"": ""appsettings.Development.json""}}],
            ""rules"": [{rules}]
        }}";
        File.WriteAllText(configPath, configJson);

        // Act & Assert
        var action = () => loader.LoadConfig(configPath);
        action.Should().Throw<ConfigurationException>()
            .WithMessage("*exceeds security limit*");
    }

    [Fact]
    public void ConfigValidator_With_CustomSecurityOptions_Should_Enforce_MaxConditionsPerRuleLimit()
    {
        // Arrange
        var securityOptions = Options.Create(new SecurityOptions
        {
            MaxConditionsPerRule = 2 // Very small limit for testing
        });
        var logger = NullLogger<ConfigValidator>.Instance;
        var validatorFactoryLogger = NullLogger<ValidatorFactory>.Instance;
        var validatorFactory = new ValidatorFactory(validatorFactoryLogger);
        var validator = new ConfigValidator(logger, securityOptions, validatorFactory);

        var conditions = string.Join(",", Enumerable.Range(1, 3).Select(i => $@"{{
            ""key"": ""Test:Key{i}"",
            ""condition"": [{{
                ""validator"": ""required"",
                ""message"": ""Test message""
            }}]
        }}"));
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
                        Id = "rule-detail-1",
                        Conditions = new List<Condition>
                        {
                            new() { Key = "Test:Key1", Validators = new List<ValidatorCondition> { new() { Validator = "required", Message = "Test" } } },
                            new() { Key = "Test:Key2", Validators = new List<ValidatorCondition> { new() { Validator = "required", Message = "Test" } } },
                            new() { Key = "Test:Key3", Validators = new List<ValidatorCondition> { new() { Validator = "required", Message = "Test" } } }
                        }
                    }
                }
            }
        };

        // Act
        var errors = validator.Validate(config, validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("exceeds security limit"));
    }

    [Fact]
    public void ConfigValidator_With_CustomSecurityOptions_Should_Enforce_MaxValidatorsPerConditionLimit()
    {
        // Arrange
        var securityOptions = Options.Create(new SecurityOptions
        {
            MaxValidatorsPerCondition = 2 // Very small limit for testing
        });
        var logger = NullLogger<ConfigValidator>.Instance;
        var validatorFactoryLogger = NullLogger<ValidatorFactory>.Instance;
        var validatorFactory = new ValidatorFactory(validatorFactoryLogger);
        var validator = new ConfigValidator(logger, securityOptions, validatorFactory);

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
                        Id = "rule-detail-1",
                        Conditions = new List<Condition>
                        {
                            new()
                            {
                                Key = "Test:Key1",
                                Validators = new List<ValidatorCondition>
                                {
                                    new() { Validator = "required", Message = "Test" },
                                    new() { Validator = "min_len", Value = "5", Message = "Test" },
                                    new() { Validator = "max_len", Value = "10", Message = "Test" }
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var errors = validator.Validate(config, validatorFactory.GetSupportedValidators());

        // Assert
        errors.Should().Contain(e => e.Contains("exceeds security limit"));
    }

    [Fact]
    public void PathResolver_With_CustomSecurityOptions_Should_Enforce_MaxPathCacheSizeLimit()
    {
        // Arrange
        var securityOptions = Options.Create(new SecurityOptions
        {
            MaxPathCacheSize = 2 // Very small limit for testing
        });
        var resolver = new PathResolver(securityOptions);
        var basePath = Path.Combine(_tempDirectory, "base.json");
        File.WriteAllText(basePath, "{}");

        // Act - Add more paths than the cache limit
        var path1 = resolver.ResolvePath("path1.json", basePath);
        var path2 = resolver.ResolvePath("path2.json", basePath);
        var path3 = resolver.ResolvePath("path3.json", basePath); // Should trigger eviction

        // Assert - All paths should resolve successfully (cache eviction should work)
        path1.Should().NotBeNullOrEmpty();
        path2.Should().NotBeNullOrEmpty();
        path3.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void YamlLoader_With_CustomSecurityOptions_Should_Enforce_MaxFileSizeLimit()
    {
        // Arrange
        var securityOptions = Options.Create(new SecurityOptions
        {
            MaxFileSizeBytes = 1024 // 1 KB - very small limit for testing
        });
        var logger = NullLogger<YamlLoader>.Instance;
        var loader = new YamlLoader(logger, securityOptions);

        // Create a YAML file larger than the limit
        var yamlPath = Path.Combine(_tempDirectory, "large-config.yaml");
        var largeContent = new string('A', 2048); // 2 KB
        var yamlContent = $@"version: '1'
environments: []
rules: []
extra: '{largeContent}'";
        File.WriteAllText(yamlPath, yamlContent);

        // Act & Assert
        var action = () => loader.LoadConfig(yamlPath);
        action.Should().Throw<ConfigurationException>()
            .WithMessage("*exceeds security limit*");
    }

    public void Dispose()
    {
        SafeFileSystem.SafeDeleteDirectory(_tempDirectory);
    }
}

