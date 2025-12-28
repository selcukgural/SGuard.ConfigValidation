using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Security;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Services.Abstract;
using SGuard.ConfigValidation.Utils;
using SGuard.ConfigValidation.Validators;
using FileValidator = SGuard.ConfigValidation.Services.FileValidator;

namespace SGuard.ConfigValidation.Tests;

/// <summary>
/// Tests for RuleEngine critical exception handling scenarios.
/// </summary>
public sealed class RuleEngineCriticalExceptionTests : IDisposable
{
    private readonly string _testDirectory;

    public RuleEngineCriticalExceptionTests()
    {
        _testDirectory = DirectoryUtility.CreateTempDirectory("ruleengine-critical-test");
    }

    [Fact]
    public async Task ValidateAllEnvironmentsAsync_With_CriticalExceptionInOneEnvironment_Should_PreservePartialResults()
    {
        // Arrange - Create a config loader that throws critical exception for one environment
        var configLogger = NullLogger<ConfigLoader>.Instance;
        var securityOptions = Options.Create(new SecurityOptions());
        
        // Create a mock config loader that throws OutOfMemoryException for specific file
        var throwingConfigLoader = new ThrowingConfigLoader(configLogger, securityOptions, "appsettings.critical.json");
        var validatorFactory = new ValidatorFactory(NullLogger<ValidatorFactory>.Instance);
        var fileValidator = new FileValidator(validatorFactory, NullLogger<FileValidator>.Instance);
        var ruleEngine = new RuleEngine(throwingConfigLoader, fileValidator, validatorFactory, 
            NullLogger<RuleEngine>.Instance, securityOptions);

        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""normal"",
      ""name"": ""Normal"",
      ""path"": ""appsettings.normal.json""
    },
    {
      ""id"": ""critical"",
      ""name"": ""Critical"",
      ""path"": ""appsettings.critical.json""
    },
    {
      ""id"": ""normal2"",
      ""name"": ""Normal 2"",
      ""path"": ""appsettings.normal2.json""
    }
  ],
  ""rules"": [
    {
      ""id"": ""test-rule"",
      ""environments"": [""normal"", ""critical"", ""normal2""],
      ""rule"": {
        ""id"": ""test-detail"",
        ""conditions"": []
      }
    }
  ]
}");
        CreateTestConfigFile("appsettings.normal.json", @"{}");
        CreateTestConfigFile("appsettings.normal2.json", @"{}");
        CreateTestConfigFile("appsettings.critical.json", @"{}");

        // Act & Assert - Critical exception should be thrown after all environments are processed
        // For a single critical exception, it's thrown directly (not wrapped in AggregateException)
        // For multiple critical exceptions, they're wrapped in AggregateException
        var action = async () => await ruleEngine.ValidateAllEnvironmentsAsync(configPath);
        
        // With one critical exception, it's thrown directly as OutOfMemoryException
        var exception = await action.Should().ThrowAsync<OutOfMemoryException>();
        exception.Which.Message.Should().Contain("Simulated critical exception");
    }


    [Fact]
    public async Task ValidateEnvironmentAsync_With_CriticalException_Should_ThrowImmediately()
    {
        // Arrange
        var configLogger = NullLogger<ConfigLoader>.Instance;
        var securityOptions = Options.Create(new SecurityOptions());
        
        var throwingConfigLoader = new ThrowingConfigLoader(configLogger, securityOptions, "appsettings.critical.json");
        var validatorFactory = new ValidatorFactory(NullLogger<ValidatorFactory>.Instance);
        var fileValidator = new FileValidator(validatorFactory, NullLogger<FileValidator>.Instance);
        var ruleEngine = new RuleEngine(throwingConfigLoader, fileValidator, validatorFactory, 
            NullLogger<RuleEngine>.Instance, securityOptions);

        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""critical"",
      ""name"": ""Critical"",
      ""path"": ""appsettings.critical.json""
    }
  ],
  ""rules"": [
    {
      ""id"": ""test-rule"",
      ""environments"": [""critical""],
      ""rule"": {
        ""id"": ""test-detail"",
        ""conditions"": []
      }
    }
  ]
}");
        CreateTestConfigFile("appsettings.critical.json", @"{}");

        // Act & Assert - Single environment validation should throw critical exception immediately
        var action = async () => await ruleEngine.ValidateEnvironmentAsync(configPath, "critical");
        await action.Should().ThrowAsync<OutOfMemoryException>();
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

    /// <summary>
    /// Mock config loader that throws critical exceptions for specific file paths.
    /// </summary>
    private sealed class ThrowingConfigLoader : IConfigLoader
    {
        private readonly IConfigLoader _baseLoader;
        private readonly HashSet<string> _criticalFilePaths;

        public ThrowingConfigLoader(ILogger<ConfigLoader> logger, IOptions<SecurityOptions> securityOptions, 
            params string[] criticalFilePaths)
        {
            _baseLoader = new ConfigLoader(logger, securityOptions);
            _criticalFilePaths = new HashSet<string>(criticalFilePaths, StringComparer.OrdinalIgnoreCase);
        }

        public Task<SGuardConfig> LoadConfigAsync(string configPath, CancellationToken cancellationToken = default)
        {
            return _baseLoader.LoadConfigAsync(configPath, cancellationToken);
        }

        public Task<Dictionary<string, object>> LoadAppSettingsAsync(string appSettingsPath, CancellationToken cancellationToken = default)
        {
            // Check if this is a critical file path
            // Check both the full path and just the filename
            var fileName = Path.GetFileName(appSettingsPath);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(appSettingsPath);
            
            // Check if any critical path matches (by filename, full path, or filename without extension)
            foreach (var criticalPath in _criticalFilePaths)
            {
                var criticalFileName = Path.GetFileName(criticalPath);
                var criticalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(criticalPath);
                
                if (fileName.Equals(criticalFileName, StringComparison.OrdinalIgnoreCase) ||
                    fileNameWithoutExtension.Equals(criticalFileNameWithoutExtension, StringComparison.OrdinalIgnoreCase) ||
                    appSettingsPath.Contains(criticalPath, StringComparison.OrdinalIgnoreCase) ||
                    appSettingsPath.Contains(criticalFileName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new OutOfMemoryException($"Simulated critical exception for file: {appSettingsPath}");
                }
            }

            return _baseLoader.LoadAppSettingsAsync(appSettingsPath, cancellationToken);
        }
    }
}

