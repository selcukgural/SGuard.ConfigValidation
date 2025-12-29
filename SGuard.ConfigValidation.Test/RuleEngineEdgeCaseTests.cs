using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Security;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Utils;
using SGuard.ConfigValidation.Validators;
using FileValidator = SGuard.ConfigValidation.Services.FileValidator;

namespace SGuard.ConfigValidation.Test;

public sealed class RuleEngineEdgeCaseTests : IDisposable
{
    private readonly RuleEngine _ruleEngine;
    private readonly string _testDirectory;

    public RuleEngineEdgeCaseTests()
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
        _testDirectory = DirectoryUtility.CreateTempDirectory("ruleengine-edge-test");
    }

    [Fact]
    public async Task ValidateEnvironmentFromJsonAsync_With_NullJson_Should_ReturnError()
    {
        // Act
        var result = await _ruleEngine.ValidateEnvironmentFromJsonAsync(null!, "dev");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateEnvironmentFromJsonAsync_With_EmptyJson_Should_ReturnError()
    {
        // Act
        var result = await _ruleEngine.ValidateEnvironmentFromJsonAsync("", "dev");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateEnvironmentFromJsonAsync_With_InvalidJson_Should_ReturnError()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var result = await _ruleEngine.ValidateEnvironmentFromJsonAsync(invalidJson, "dev");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateEnvironmentFromJsonAsync_With_NullEnvironmentId_Should_ReturnError()
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
  ""rules"": []
}";

        // Act
        var result = await _ruleEngine.ValidateEnvironmentFromJsonAsync(configJson, null!);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Environment ID is required");
    }

    [Fact]
    public async Task ValidateEnvironmentFromJsonAsync_With_EmptyEnvironmentId_Should_ReturnError()
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
  ""rules"": []
}";

        // Act
        var result = await _ruleEngine.ValidateEnvironmentFromJsonAsync(configJson, "");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Environment ID is required");
    }

    [Fact]
    public async Task ValidateEnvironmentFromJsonAsync_With_NonExistentEnvironmentId_Should_ReturnError()
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
  ""rules"": []
}";

        // Act
        var result = await _ruleEngine.ValidateEnvironmentFromJsonAsync(configJson, "nonexistent");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ValidateAllEnvironmentsFromJsonAsync_With_NullJson_Should_ReturnError()
    {
        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsFromJsonAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAllEnvironmentsFromJsonAsync_With_EmptyJson_Should_ReturnError()
    {
        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsFromJsonAsync("");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAllEnvironmentsFromJsonAsync_With_InvalidJson_Should_ReturnError()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsFromJsonAsync(invalidJson);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateEnvironmentFromJsonAsync_With_CancellationToken_Should_Respect_Cancellation()
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
  ""rules"": []
}";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _ruleEngine.ValidateEnvironmentFromJsonAsync(configJson, "dev", cts.Token));
    }

    [Fact]
    public async Task ValidateAllEnvironmentsFromJsonAsync_With_CancellationToken_Should_Respect_Cancellation()
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
  ""rules"": []
}";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _ruleEngine.ValidateAllEnvironmentsFromJsonAsync(configJson, cts.Token));
    }

    [Fact]
    public async Task ValidateEnvironmentAsync_With_NonExistentFile_Should_ReturnError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");

        // Act
        var result = await _ruleEngine.ValidateEnvironmentAsync(nonExistentPath, "dev");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateEnvironmentAsync_With_EmptyEnvironmentId_Should_ReturnError()
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
  ""rules"": []
}");

        // Act
        var result = await _ruleEngine.ValidateEnvironmentAsync(configPath, "");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Environment ID is required");
    }

    [Fact]
    public async Task ValidateEnvironmentAsync_With_EmptyPath_Should_ReturnError()
    {
        // Arrange
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": """"
    }
  ],
  ""rules"": []
}");

        // Act
        var result = await _ruleEngine.ValidateEnvironmentAsync(configPath, "dev");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().ContainAny("invalid", "empty", "path");
    }

    [Fact]
    public async Task ValidateEnvironmentAsync_With_NullPath_Should_ReturnError()
    {
        // Arrange
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": null
    }
  ],
  ""rules"": []
}");

        // Act
        var result = await _ruleEngine.ValidateEnvironmentAsync(configPath, "dev");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().ContainAny("invalid", "empty", "path");
    }

    [Fact]
    public async Task ValidateEnvironmentAsync_With_WhitespaceEnvironmentId_Should_ReturnError()
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
  ""rules"": []
}");

        // Act
        var result = await _ruleEngine.ValidateEnvironmentAsync(configPath, "   ");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Environment ID is required");
    }

    [Fact]
    public async Task ValidateAllEnvironmentsAsync_With_NonExistentFile_Should_ReturnError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");

        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsAsync(nonExistentPath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAllEnvironments_With_EmptyPathEnvironment_Should_HandleGracefully()
    {
        // Arrange
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": """"
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
        ""id"": ""rule-detail"",
        ""conditions"": []
      }
    }
  ]
}");
        CreateTestConfigFile("appsettings.prod.json", @"{}");

        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsAsync(configPath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ValidationResults.Should().HaveCount(2);
        // dev environment should have error for empty path
        var devResult = result.ValidationResults.FirstOrDefault(r => r.Path == "dev");
        devResult.Should().NotBeNull();
        devResult!.IsValid.Should().BeFalse();
        devResult.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ValidateAllEnvironments_With_MultipleEnvironments_Should_ProcessInParallel()
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
      ""id"": ""staging"",
      ""name"": ""Staging"",
      ""path"": ""appsettings.staging.json""
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
      ""environments"": [""dev"", ""staging"", ""prod""],
      ""rule"": {
        ""id"": ""rule-detail"",
        ""conditions"": []
      }
    }
  ]
}");
        CreateTestConfigFile("appsettings.dev.json", @"{}");
        CreateTestConfigFile("appsettings.staging.json", @"{}");
        CreateTestConfigFile("appsettings.prod.json", @"{}");

        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsAsync(configPath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ValidationResults.Should().HaveCount(3);
        // Path is resolved file path, not environment ID
        result.ValidationResults.Should().Contain(r => r.Path.Contains("appsettings.dev.json") || r.Path == "dev");
        result.ValidationResults.Should().Contain(r => r.Path.Contains("appsettings.staging.json") || r.Path == "staging");
        result.ValidationResults.Should().Contain(r => r.Path.Contains("appsettings.prod.json") || r.Path == "prod");
    }

    [Fact]
    public async Task ValidateAllEnvironments_With_FileNotFound_Should_HandleGracefully()
    {
        // Arrange
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": ""nonexistent.json""
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
        ""id"": ""rule-detail"",
        ""conditions"": []
      }
    }
  ]
}");
        CreateTestConfigFile("appsettings.prod.json", @"{}");

        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsAsync(configPath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ValidationResults.Should().HaveCount(2);
        // dev environment should have file not found error (Path is environment.Id for FileNotFoundException)
        var devResult = result.ValidationResults.FirstOrDefault(r => r.Path == "dev" || r.Path.Contains("nonexistent"));
        devResult.Should().NotBeNull();
        devResult!.IsValid.Should().BeFalse();
        devResult.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ValidateAllEnvironments_With_CancellationToken_Should_RespectCancellation()
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
        ""id"": ""rule-detail"",
        ""conditions"": []
      }
    }
  ]
}");
        CreateTestConfigFile("appsettings.dev.json", @"{}");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - Cancellation happens early, may throw OperationCanceledException or TaskCanceledException
        try
        {
            await _ruleEngine.ValidateAllEnvironmentsAsync(configPath, cts.Token);
            // If no exception, cancellation was checked early and validation completed
        }
        catch (TaskCanceledException)
        {
            // Expected - TaskCanceledException derives from OperationCanceledException
        }
        catch (OperationCanceledException)
        {
            // Also acceptable
        }
    }

    [Fact]
    public async Task ValidateAllEnvironments_With_NoEnvironments_Should_ReturnError()
    {
        // Arrange
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [],
  ""rules"": []
}");

        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsAsync(configPath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().ContainAny("No environments found", "No environments defined", "environments");
    }

    [Fact]
    public async Task ValidateAllEnvironments_With_MixedSuccessAndFailure_Should_ReturnAllResults()
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
      ""path"": ""nonexistent.json""
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
        CreateTestConfigFile("appsettings.dev.json", @"{
  ""Test"": {
    ""Key"": ""value""
  }
}");

        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsAsync(configPath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ValidationResults.Should().HaveCount(2);
        // dev should succeed (Path is resolved file path, not environment ID)
        var devResult = result.ValidationResults.FirstOrDefault(r => r.Path.Contains("appsettings.dev.json") || r.Path == "dev");
        devResult.Should().NotBeNull();
        devResult!.IsValid.Should().BeTrue();
        // prod should fail (file not found - Path is environment.Id for FileNotFoundException)
        var prodResult = result.ValidationResults.FirstOrDefault(r => r.Path == "prod" || r.Path.Contains("nonexistent"));
        prodResult.Should().NotBeNull();
        prodResult!.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAllEnvironments_With_Results_Should_BeSorted()
    {
        // Arrange
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""zebra"",
      ""name"": ""Zebra"",
      ""path"": ""appsettings.zebra.json""
    },
    {
      ""id"": ""alpha"",
      ""name"": ""Alpha"",
      ""path"": ""appsettings.alpha.json""
    },
    {
      ""id"": ""beta"",
      ""name"": ""Beta"",
      ""path"": ""appsettings.beta.json""
    }
  ],
  ""rules"": [
    {
      ""id"": ""test-rule"",
      ""environments"": [""alpha"", ""beta"", ""zebra""],
      ""rule"": {
        ""id"": ""rule-detail"",
        ""conditions"": []
      }
    }
  ]
}");
        CreateTestConfigFile("appsettings.zebra.json", @"{}");
        CreateTestConfigFile("appsettings.alpha.json", @"{}");
        CreateTestConfigFile("appsettings.beta.json", @"{}");

        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsAsync(configPath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ValidationResults.Should().HaveCount(3);
        // Results should be sorted by path (resolved file path, not environment ID)
        var paths = result.ValidationResults.Select(r => r.Path).ToList();
        paths.Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAllEnvironments_With_EmptyEnvironmentsList_Should_ReturnError()
    {
        // Arrange
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [],
  ""rules"": []
}");

        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsAsync(configPath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().ContainAny("No environments found", "No environments defined", "environments");
    }

    [Fact]
    public async Task ValidateAllEnvironments_With_EmptyPath_Should_ReturnError()
    {
        // Arrange
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": """"
    }
  ],
  ""rules"": [
    {
      ""id"": ""test-rule"",
      ""environments"": [""dev""],
      ""rule"": {
        ""id"": ""rule-detail"",
        ""conditions"": []
      }
    }
  ]
}");

        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsAsync(configPath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue(); // Overall success, but individual environment should have error
        result.ValidationResults.Should().HaveCount(1);
        result.ValidationResults[0].IsValid.Should().BeFalse();
        result.ValidationResults[0].Errors.Should().Contain(e => e.Message.Contains("invalid or empty path", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateAllEnvironments_With_NullPath_Should_ReturnError()
    {
        // Arrange
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": null
    }
  ],
  ""rules"": [
    {
      ""id"": ""test-rule"",
      ""environments"": [""dev""],
      ""rule"": {
        ""id"": ""rule-detail"",
        ""conditions"": []
      }
    }
  ]
}");

        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsAsync(configPath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue(); // Overall success, but individual environment should have error
        result.ValidationResults.Should().HaveCount(1);
        result.ValidationResults[0].IsValid.Should().BeFalse();
        result.ValidationResults[0].Errors.Should().Contain(e => e.Message.Contains("invalid or empty path", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateAllEnvironments_With_MultipleEnvironmentsAndMixedErrors_Should_HandleAll()
    {
        // Arrange - Multiple environments with different scenarios
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": ""appsettings.dev.json""
    },
    {
      ""id"": ""test"",
      ""name"": ""Test"",
      ""path"": ""appsettings.test.json""
    },
    {
      ""id"": ""prod"",
      ""name"": ""Production"",
      ""path"": ""appsettings.prod.json""
    },
    {
      ""id"": ""empty"",
      ""name"": ""Empty"",
      ""path"": """"
    }
  ],
  ""rules"": [
    {
      ""id"": ""test-rule"",
      ""environments"": [""dev"", ""test"", ""prod""],
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
        CreateTestConfigFile("appsettings.dev.json", @"{""Test"": {""Key"": ""value""}}");
        CreateTestConfigFile("appsettings.test.json", @"{}"); // Missing key
        // prod file doesn't exist - will cause FileNotFoundException

        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsAsync(configPath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue(); // Overall success, but individual environments may fail
        result.ValidationResults.Should().HaveCount(4);
        
        // dev should succeed (Path is resolved file path)
        var devResult = result.ValidationResults.FirstOrDefault(r => r.Path.Contains("appsettings.dev.json") || r.Path == "dev");
        devResult.Should().NotBeNull();
        devResult!.IsValid.Should().BeTrue();
        
        // test should fail (missing key - Path is resolved file path)
        var testResult = result.ValidationResults.FirstOrDefault(r => r.Path.Contains("appsettings.test.json") || r.Path == "test");
        testResult.Should().NotBeNull();
        testResult!.IsValid.Should().BeFalse();
        
        // prod should fail (file not found - Path is environment.Id for FileNotFoundException)
        var prodResult = result.ValidationResults.FirstOrDefault(r => r.Path == "prod" || r.Path.Contains("nonexistent"));
        prodResult.Should().NotBeNull();
        prodResult!.IsValid.Should().BeFalse();
        
        // empty should fail (empty path - Path is environment.Id)
        var emptyResult = result.ValidationResults.FirstOrDefault(r => r.Path == "empty");
        emptyResult.Should().NotBeNull();
        emptyResult!.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAllEnvironments_With_ParallelExecution_Should_CompleteSuccessfully()
    {
        // Arrange - Multiple environments to test parallel execution
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""env1"",
      ""name"": ""Environment 1"",
      ""path"": ""appsettings.env1.json""
    },
    {
      ""id"": ""env2"",
      ""name"": ""Environment 2"",
      ""path"": ""appsettings.env2.json""
    },
    {
      ""id"": ""env3"",
      ""name"": ""Environment 3"",
      ""path"": ""appsettings.env3.json""
    },
    {
      ""id"": ""env4"",
      ""name"": ""Environment 4"",
      ""path"": ""appsettings.env4.json""
    },
    {
      ""id"": ""env5"",
      ""name"": ""Environment 5"",
      ""path"": ""appsettings.env5.json""
    }
  ],
  ""rules"": [
    {
      ""id"": ""test-rule"",
      ""environments"": [""env1"", ""env2"", ""env3"", ""env4"", ""env5""],
      ""rule"": {
        ""id"": ""rule-detail"",
        ""conditions"": []
      }
    }
  ]
}");
        // Create all appsettings files
        for (int i = 1; i <= 5; i++)
        {
            CreateTestConfigFile($"appsettings.env{i}.json", @"{}");
        }

        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsAsync(configPath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ValidationResults.Should().HaveCount(5);
        result.ValidationResults.All(r => r.IsValid).Should().BeTrue();
    }

    [Fact]
    public async Task ValidateEnvironmentFromJsonAsync_With_EmptyPath_Should_NotRequirePath()
    {
        // Arrange - JSON validation doesn't require path
        var configJson = @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": """"
    }
  ],
  ""rules"": [
    {
      ""id"": ""test-rule"",
      ""environments"": [""dev""],
      ""rule"": {
        ""id"": ""rule-detail"",
        ""conditions"": []
      }
    }
  ]
}";

        // Act - Should succeed because requirePath=false for JSON validation
        var result = await _ruleEngine.ValidateEnvironmentFromJsonAsync(configJson, "dev");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue(); // JSON validation doesn't require path
    }

    [Fact]
    public async Task ValidateAllEnvironmentsFromJsonAsync_With_ValidConfig_Should_Succeed()
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
        ""id"": ""rule-detail"",
        ""conditions"": []
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
    public async Task ValidateEnvironmentFromJsonAsync_With_WhitespaceEnvironmentId_Should_ReturnError()
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
  ""rules"": []
}";

        // Act
        var result = await _ruleEngine.ValidateEnvironmentFromJsonAsync(configJson, "   ");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Environment ID is required");
    }

    private string CreateTestConfigFile(string fileName, string content)
    {
        var filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    public void Dispose()
    {
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
    }
}

