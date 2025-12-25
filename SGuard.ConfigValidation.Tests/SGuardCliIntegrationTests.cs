using SGuard.ConfigChecker.Console;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Tests;

public sealed class SGuardCliIntegrationTests : IDisposable
{
    private readonly SGuardCli _cli;
    private readonly string _testDirectory;

    public SGuardCliIntegrationTests()
    {
        var configLogger = NullLogger<ConfigLoader>.Instance;
        var configLoader = new ConfigLoader(configLogger);
        var validatorFactoryLogger = NullLogger<ValidatorFactory>.Instance;
        var validatorFactory = new ValidatorFactory(validatorFactoryLogger);
        var fileValidatorLogger = NullLogger<FileValidator>.Instance;
        var fileValidator = new FileValidator(validatorFactory, fileValidatorLogger);
        var ruleEngineLogger = NullLogger<RuleEngine>.Instance;
        var ruleEngine = new RuleEngine(configLoader, fileValidator, validatorFactory, ruleEngineLogger);
        var cliLogger = NullLogger<SGuardCli>.Instance;
        _cli = new SGuardCli(ruleEngine, cliLogger);
        _testDirectory = SafeFileSystemHelper.CreateSafeTempDirectory("sguardcli-test");
    }

    [Fact]
    public async Task RunAsync_With_ValidConfig_Should_Return_ExitCode_0()
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
        var exitCode = await _cli.RunAsync(new[] { "validate", "--config", configAbsolutePath, "--env", "dev" });

        // Assert
        exitCode.Should().Be(ExitCode.Success);
    }

    [Fact]
    public async Task RunAsync_With_EnvAndAllConflict_Should_Return_ExitCode_2()
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
        var configAbsolutePath = Path.Combine(_testDirectory, "sguard.json");
        var exitCode = await _cli.RunAsync(new[] { "validate", "--config", configAbsolutePath, "--env", "dev", "--all" });

        // Assert
        exitCode.Should().Be(ExitCode.SystemError);
    }

    [Fact]
    public async Task RunAsync_With_InvalidOutputFormat_Should_Return_NonZeroExitCode()
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
        var configAbsolutePath = Path.Combine(_testDirectory, "sguard.json");
        var exitCode = await _cli.RunAsync(new[] { "validate", "--config", configAbsolutePath, "--output", "invalid" });

        // Assert - System.CommandLine validator should catch this and return non-zero exit code
        exitCode.Should().NotBe(ExitCode.Success, "Invalid output format should result in non-zero exit code");
    }

    private string CreateTestConfigFile(string fileName, string content)
    {
        var filePath = Path.Combine(_testDirectory, fileName);
        SafeFileSystemHelper.SafeWriteAllText(filePath, content);
        return filePath;
    }

    public void Dispose()
    {
        SafeFileSystemHelper.SafeDeleteDirectory(_testDirectory, recursive: true);
    }
}