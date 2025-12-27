using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Tests;

public sealed class RuleEngineTests : IDisposable
{
    private readonly RuleEngine _ruleEngine;
    private readonly string _testDirectory;

    public RuleEngineTests()
    {
        var configLogger = NullLogger<ConfigLoader>.Instance;
        var securityOptions = Options.Create(new SecurityOptions());
        var configLoader = new ConfigLoader(configLogger, securityOptions);
        var validatorFactoryLogger = NullLogger<ValidatorFactory>.Instance;
        var validatorFactory = new ValidatorFactory(validatorFactoryLogger);
        var fileValidatorLogger = NullLogger<FileValidator>.Instance;
        var fileValidator = new FileValidator(validatorFactory, fileValidatorLogger);
        var ruleEngineLogger = NullLogger<RuleEngine>.Instance;
        _ruleEngine = new RuleEngine(configLoader, fileValidator, validatorFactory, ruleEngineLogger, securityOptions);
        _testDirectory = SafeFileSystem.CreateSafeTempDirectory("sguard-test");
    }

    [Fact]
    public async Task ValidateEnvironment_With_ValidConfig_Should_ReturnSuccess()
    {
        // Arrange
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": ""appsettings.dev.json""
    }
  ],
  ""rules"": [
    {
      ""id"": ""test-rule"",
      ""environments"": [""dev""],
      ""rule"": {
        ""id"": ""test-detail"",
        ""conditions"": [
          {
            ""key"": ""Test:Key"",
            ""condition"": [
              {
                ""validator"": ""required"",
                ""message"": ""Test key is required""
              }
            ]
          }
        ]
      }
    }
  ]
}");
        var appSettingsPath = CreateTestConfigFile("appsettings.dev.json", @"{
  ""Test"": {
    ""Key"": ""value""
  }
}");
        
        // Act - use absolute path to avoid directory change issues
        var configAbsolutePath = Path.Combine(_testDirectory, "sguard.json");
        var result = await _ruleEngine.ValidateEnvironmentAsync(configAbsolutePath, "dev");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.HasValidationErrors.Should().BeFalse();
        result.SingleResult.Should().NotBeNull();
        result.SingleResult!.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateEnvironment_With_EnvironmentNotFound_Should_ReturnError()
    {
        // Arrange
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": ""appsettings.dev.json""
    }
  ],
  ""rules"": [
    {
      ""id"": ""test-rule"",
      ""environments"": [""dev""],
      ""rule"": {
        ""id"": ""test-detail"",
        ""conditions"": [
          {
            ""key"": ""Test:Key"",
            ""condition"": [
              {
                ""validator"": ""required"",
                ""message"": ""Test key is required""
              }
            ]
          }
        ]
      }
    }
  ]
}");
        // Act - use absolute path to avoid directory change issues
        var configAbsolutePath = Path.Combine(_testDirectory, "sguard.json");
        var result = await _ruleEngine.ValidateEnvironmentAsync(configAbsolutePath, "nonexistent");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ValidateEnvironment_With_FileNotFound_Should_ReturnError()
    {
        // Arrange
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": ""nonexistent.json""
    }
  ],
  ""rules"": [
    {
      ""id"": ""test-rule"",
      ""environments"": [""dev""],
      ""rule"": {
        ""id"": ""test-detail"",
        ""conditions"": [
          {
            ""key"": ""Test:Key"",
            ""condition"": [
              {
                ""validator"": ""required"",
                ""message"": ""Test key is required""
              }
            ]
          }
        ]
      }
    }
  ]
}");
        // Act - use absolute path to avoid directory change issues
        var configAbsolutePath = Path.Combine(_testDirectory, "sguard.json");
        var result = await _ruleEngine.ValidateEnvironmentAsync(configAbsolutePath, "dev");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ValidateAllEnvironments_With_ValidConfig_Should_ReturnSuccess()
    {
        // Arrange
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": ""appsettings.dev.json""
    },
    {
      ""id"": ""prod"",
      ""name"": ""Production"",
      ""path"": ""appsettings.prod.json""
    }
  ],
  ""rules"": [
    {
      ""id"": ""test-rule"",
      ""environments"": [""dev"", ""prod""],
      ""rule"": {
        ""id"": ""test-detail"",
        ""conditions"": [
          {
            ""key"": ""Test:Key"",
            ""condition"": [
              {
                ""validator"": ""required"",
                ""message"": ""Test key is required""
              }
            ]
          }
        ]
      }
    }
  ]
}");
        var appSettingsDevPath = CreateTestConfigFile("appsettings.dev.json", @"{
  ""Test"": {
    ""Key"": ""value""
  }
}");
        var appSettingsProdPath = CreateTestConfigFile("appsettings.prod.json", @"{
  ""Test"": {
    ""Key"": ""value""
  }
}");
        // Act - use absolute path to avoid directory change issues
        var configAbsolutePath = Path.Combine(_testDirectory, "sguard.json");
        var result = await _ruleEngine.ValidateAllEnvironmentsAsync(configAbsolutePath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ValidationResults.Should().HaveCount(2);
        result.ValidationResults.All(r => r.IsValid).Should().BeTrue();
    }

    [Fact]
    public async Task ValidateEnvironmentFromJson_With_ValidJson_Should_ReturnSuccess()
    {
        // Arrange
        var configJson = @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": ""appsettings.dev.json""
    }
  ],
  ""rules"": [
    {
      ""id"": ""test-rule"",
      ""environments"": [""dev""],
      ""rule"": {
        ""id"": ""test-detail"",
        ""conditions"": [
          {
            ""key"": ""Test:Key"",
            ""condition"": [
              {
                ""validator"": ""required"",
                ""message"": ""Test key is required""
              }
            ]
          }
        ]
      }
    }
  ]
}";

        // Act
        var result = await _ruleEngine.ValidateEnvironmentFromJsonAsync(configJson, "dev");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.SingleResult.Should().NotBeNull();
        // JSON validation uses empty appSettings, so required validator will fail
        result.SingleResult!.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAllEnvironmentsFromJson_With_ValidJson_Should_ReturnSuccess()
    {
        // Arrange
        var configJson = @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": ""appsettings.dev.json""
    },
    {
      ""id"": ""prod"",
      ""name"": ""Production"",
      ""path"": ""appsettings.prod.json""
    }
  ],
  ""rules"": [
    {
      ""id"": ""test-rule"",
      ""environments"": [""dev"", ""prod""],
      ""rule"": {
        ""id"": ""test-detail"",
        ""conditions"": [
          {
            ""key"": ""Test:Key"",
            ""condition"": [
              {
                ""validator"": ""required"",
                ""message"": ""Test key is required""
              }
            ]
          }
        ]
      }
    }
  ]
}";

        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsFromJsonAsync(configJson);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ValidationResults.Should().HaveCount(2);
    }

    [Fact]
    public async Task ValidateEnvironment_With_NullConfigPath_Should_ReturnError()
    {
        // Act
        var result = await _ruleEngine.ValidateEnvironmentAsync(null!, "dev");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("File path cannot be null or empty");
    }

    [Fact]
    public async Task ValidateEnvironment_With_NullEnvironmentId_Should_ReturnError()
    {
        // Act
        var result = await _ruleEngine.ValidateEnvironmentAsync("sguard.json", null!);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Environment ID is required");
    }

    [Fact]
    public void GetSupportedValidators_Should_ReturnList()
    {
        // Act
        var validators = _ruleEngine.GetSupportedValidators().ToList();

        // Assert
        validators.Should().NotBeEmpty();
        validators.Should().Contain("required");
        validators.Should().Contain("min_len");
        validators.Should().Contain("max_len");
    }

    private string CreateTestConfigFile(string fileName, string content)
    {
        var filePath = Path.Combine(_testDirectory, fileName);
        SafeFileSystem.SafeWriteAllText(filePath, content);
        return filePath;
    }

    public void Dispose()
    {
        SafeFileSystem.SafeDeleteDirectory(_testDirectory, recursive: true);
    }
}

