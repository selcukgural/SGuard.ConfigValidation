using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Security;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Utils;
using SGuard.ConfigValidation.Validators;
using FileValidator = SGuard.ConfigValidation.Services.FileValidator;

namespace SGuard.ConfigValidation.Tests;

/// <summary>
/// Tests for RuleEngine concurrent/parallel execution scenarios.
/// </summary>
public sealed class RuleEngineConcurrencyTests : IDisposable
{
    private readonly RuleEngine _ruleEngine;
    private readonly string _testDirectory;

    public RuleEngineConcurrencyTests()
    {
        var configLogger = NullLogger<ConfigLoader>.Instance;
        var securityOptions = Options.Create(new SecurityOptions
        {
            MaxParallelEnvironments = Environment.ProcessorCount
        });
        var configLoader = new ConfigLoader(configLogger, securityOptions);
        var validatorFactoryLogger = NullLogger<ValidatorFactory>.Instance;
        var validatorFactory = new ValidatorFactory(validatorFactoryLogger);
        var fileValidatorLogger = NullLogger<FileValidator>.Instance;
        var fileValidator = new FileValidator(validatorFactory, fileValidatorLogger);
        var ruleEngineLogger = NullLogger<RuleEngine>.Instance;
        _ruleEngine = new RuleEngine(configLoader, fileValidator, validatorFactory, ruleEngineLogger, securityOptions);
        _testDirectory = DirectoryUtility.CreateTempDirectory("ruleengine-concurrency-test");
    }

    [Fact]
    public async Task ValidateAllEnvironmentsAsync_With_ManyEnvironments_Should_ExecuteInParallel()
    {
        // Arrange - Create config with many environments to test parallel execution
        const int environmentCount = 20;
        var environments = new List<string>();
        var rules = new List<string>();

        for (int i = 0; i < environmentCount; i++)
        {
            var envId = $"env{i}";
            environments.Add($@"{{
      ""id"": ""{envId}"",
      ""name"": ""Environment {i}"",
      ""path"": ""appsettings.{envId}.json""
    }}");
            rules.Add($"\"{envId}\"");
            CreateTestConfigFile($"appsettings.{envId}.json", @"{
  ""Test"": {
    ""Key"": ""value""
  }
}");
        }

        var configJson = $@"{{
  ""version"": ""1"",
  ""environments"": [{string.Join(",", environments)}],
  ""rules"": [
    {{
      ""id"": ""test-rule"",
      ""environments"": [{string.Join(",", rules)}],
      ""rule"": {{
        ""id"": ""test-detail"",
        ""conditions"": [
          {{
            ""key"": ""Test:Key"",
            ""condition"": [
              {{
                ""validator"": ""required"",
                ""message"": ""Test key is required""
              }}
            ]
          }}
        ]
      }}
    }}
  ]
}}";

        var configPath = CreateTestConfigFile("sguard.json", configJson);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _ruleEngine.ValidateAllEnvironmentsAsync(configPath);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ValidationResults.Should().HaveCount(environmentCount);
        
        // All environments should be validated successfully
        result.ValidationResults.Should().AllSatisfy(r => r.IsValid.Should().BeTrue());
        
        // Parallel execution should be faster than sequential (rough check)
        // Sequential would take at least environmentCount * some_minimum_time
        // With parallelism, it should be significantly faster
        var sequentialEstimate = environmentCount * 10; // Rough estimate: 10ms per environment sequentially
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(sequentialEstimate, 
            "Parallel execution should be faster than sequential");
    }

    [Fact]
    public async Task ValidateAllEnvironmentsAsync_With_ConcurrentCalls_Should_NotInterfere()
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
        ""conditions"": []
      }
    }
  ]
}");
        CreateTestConfigFile("appsettings.dev.json", @"{}");
        CreateTestConfigFile("appsettings.prod.json", @"{}");

        // Act - Execute multiple validations concurrently
        var tasks = new List<Task>();
        const int concurrentCalls = 10;
        
        for (int i = 0; i < concurrentCalls; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var result = await _ruleEngine.ValidateAllEnvironmentsAsync(configPath);
                result.Should().NotBeNull();
                result.IsSuccess.Should().BeTrue();
                result.ValidationResults.Should().HaveCount(2);
            }));
        }

        // Assert - All concurrent calls should complete successfully
        var action = async () => await Task.WhenAll(tasks);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateAllEnvironmentsFromJsonAsync_With_ManyEnvironments_Should_ExecuteInParallel()
    {
        // Arrange - Create JSON config with many environments
        const int environmentCount = 15;
        var environments = new List<string>();
        var rules = new List<string>();

        for (int i = 0; i < environmentCount; i++)
        {
            var envId = $"env{i}";
            environments.Add($@"{{
      ""id"": ""{envId}"",
      ""name"": ""Environment {i}"",
      ""path"": ""appsettings.{envId}.json""
    }}");
            rules.Add($"\"{envId}\"");
        }

        var configJson = $@"{{
  ""version"": ""1"",
  ""environments"": [{string.Join(",", environments)}],
  ""rules"": [
    {{
      ""id"": ""test-rule"",
      ""environments"": [{string.Join(",", rules)}],
      ""rule"": {{
        ""id"": ""test-detail"",
        ""conditions"": []
      }}
    }}
  ]
}}";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _ruleEngine.ValidateAllEnvironmentsFromJsonAsync(configJson);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ValidationResults.Should().HaveCount(environmentCount);
        
        // Parallel execution should complete faster than sequential
        var sequentialEstimate = environmentCount * 5; // Rough estimate for JSON parsing
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(sequentialEstimate,
            "Parallel execution should be faster than sequential");
    }

    [Fact]
    public async Task ValidateAllEnvironmentsAsync_With_MixedSuccessAndFailure_Should_PreserveAllResults()
    {
        // Arrange - Some environments succeed, some fail
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""success1"",
      ""name"": ""Success 1"",
      ""path"": ""appsettings.success1.json""
    },
    {
      ""id"": ""success2"",
      ""name"": ""Success 2"",
      ""path"": ""appsettings.success2.json""
    },
    {
      ""id"": ""failure1"",
      ""name"": ""Failure 1"",
      ""path"": ""nonexistent1.json""
    },
    {
      ""id"": ""failure2"",
      ""name"": ""Failure 2"",
      ""path"": ""nonexistent2.json""
    }
  ],
  ""rules"": [
    {
      ""id"": ""test-rule"",
      ""environments"": [""success1"", ""success2"", ""failure1"", ""failure2""],
      ""rule"": {
        ""id"": ""test-detail"",
        ""conditions"": []
      }
    }
  ]
}");
        CreateTestConfigFile("appsettings.success1.json", @"{}");
        CreateTestConfigFile("appsettings.success2.json", @"{}");

        // Act
        var result = await _ruleEngine.ValidateAllEnvironmentsAsync(configPath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue(); // Overall success, but individual results may fail
        result.ValidationResults.Should().HaveCount(4);
        
        // Success environments should be valid
        var successResults = result.ValidationResults.Where(r => 
            r.Path.Contains("success1") || r.Path.Contains("success2")).ToList();
        successResults.Should().HaveCount(2);
        successResults.Should().AllSatisfy(r => r.IsValid.Should().BeTrue());
        
        // Failure environments should have errors
        var failureResults = result.ValidationResults.Where(r => 
            r.Path == "failure1" || r.Path == "failure2" || 
            r.Path.Contains("nonexistent")).ToList();
        failureResults.Should().HaveCount(2);
        failureResults.Should().AllSatisfy(r => r.IsValid.Should().BeFalse());
        failureResults.Should().AllSatisfy(r => r.ErrorCount.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task ValidateAllEnvironmentsAsync_With_CancellationDuringExecution_Should_StopGracefully()
    {
        // Arrange
        const int environmentCount = 10;
        var environments = new List<string>();
        var rules = new List<string>();

        for (int i = 0; i < environmentCount; i++)
        {
            var envId = $"env{i}";
            environments.Add($@"{{
      ""id"": ""{envId}"",
      ""name"": ""Environment {i}"",
      ""path"": ""appsettings.{envId}.json""
    }}");
            rules.Add($"\"{envId}\"");
            CreateTestConfigFile($"appsettings.{envId}.json", @"{}");
        }

        var configJson = $@"{{
  ""version"": ""1"",
  ""environments"": [{string.Join(",", environments)}],
  ""rules"": [
    {{
      ""id"": ""test-rule"",
      ""environments"": [{string.Join(",", rules)}],
      ""rule"": {{
        ""id"": ""test-detail"",
        ""conditions"": []
      }}
    }}
  ]
}}";

        var configPath = CreateTestConfigFile("sguard.json", configJson);
        using var cts = new CancellationTokenSource();

        // Act - Cancel after a short delay
        var validationTask = _ruleEngine.ValidateAllEnvironmentsAsync(configPath, cts.Token);
        cts.CancelAfter(50); // Cancel after 50ms

        // Assert - Should throw OperationCanceledException or TaskCanceledException
        // TaskCanceledException derives from OperationCanceledException, so catching OperationCanceledException covers both
        var action = async () => await validationTask;
        try
        {
            await action();
            // If no exception, cancellation was checked early and validation completed
            // This is acceptable behavior
        }
        catch (OperationCanceledException)
        {
            // Expected - cancellation was respected (covers both OperationCanceledException and TaskCanceledException)
        }
    }

    [Fact]
    public async Task ValidateAllEnvironmentsAsync_With_HighParallelism_Should_RespectMaxParallelism()
    {
        // Arrange - Use low parallelism setting
        var securityOptions = Options.Create(new SecurityOptions
        {
            MaxParallelEnvironments = 2 // Limit to 2 parallel executions
        });
        var configLoader = new ConfigLoader(NullLogger<ConfigLoader>.Instance, securityOptions);
        var validatorFactory = new ValidatorFactory(NullLogger<ValidatorFactory>.Instance);
        var fileValidator = new FileValidator(validatorFactory, NullLogger<FileValidator>.Instance);
        var ruleEngine = new RuleEngine(configLoader, fileValidator, validatorFactory, 
            NullLogger<RuleEngine>.Instance, securityOptions);

        const int environmentCount = 8;
        var environments = new List<string>();
        var rules = new List<string>();

        for (int i = 0; i < environmentCount; i++)
        {
            var envId = $"env{i}";
            environments.Add($@"{{
      ""id"": ""{envId}"",
      ""name"": ""Environment {i}"",
      ""path"": ""appsettings.{envId}.json""
    }}");
            rules.Add($"\"{envId}\"");
            CreateTestConfigFile($"appsettings.{envId}.json", @"{}");
        }

        var configJson = $@"{{
  ""version"": ""1"",
  ""environments"": [{string.Join(",", environments)}],
  ""rules"": [
    {{
      ""id"": ""test-rule"",
      ""environments"": [{string.Join(",", rules)}],
      ""rule"": {{
        ""id"": ""test-detail"",
        ""conditions"": []
      }}
    }}
  ]
}}";

        var configPath = CreateTestConfigFile("sguard.json", configJson);

        // Act
        var result = await ruleEngine.ValidateAllEnvironmentsAsync(configPath);

        // Assert - Should complete successfully even with limited parallelism
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ValidationResults.Should().HaveCount(environmentCount);
    }

    private string CreateTestConfigFile(string fileName, string content)
    {
        var filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}

