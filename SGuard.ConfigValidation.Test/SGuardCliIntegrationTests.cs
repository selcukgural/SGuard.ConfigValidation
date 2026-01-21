using SGuard.ConfigValidation.Console;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Security;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Utilities;
using SGuard.ConfigValidation.Validators;
using FileValidator = SGuard.ConfigValidation.Services.FileValidator;

namespace SGuard.ConfigValidation.Test;

public sealed class SGuardCliIntegrationTests : IDisposable
{
    private readonly SGuardCli _cli;
    private readonly string _testDirectory;

    public SGuardCliIntegrationTests()
    {
        var configLogger = NullLogger<ConfigLoader>.Instance;
        var securityOptions = Options.Create(new SecurityOptions());
        var configLoader = new ConfigLoader(configLogger, securityOptions);
        var validatorFactoryLogger = NullLogger<ValidatorFactory>.Instance;
        var validatorFactory = new ValidatorFactory(validatorFactoryLogger);
        var fileValidatorLogger = NullLogger<FileValidator>.Instance;
        var fileValidator = new FileValidator(validatorFactory, fileValidatorLogger);
        var ruleEngineLogger = NullLogger<RuleEngine>.Instance;
        var ruleEngine = new RuleEngine(configLoader, fileValidator, validatorFactory, ruleEngineLogger, securityOptions);
        var cliLogger = NullLogger<SGuardCli>.Instance;
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _cli = new SGuardCli(ruleEngine, configLoader, cliLogger, loggerFactory);
        _testDirectory = DirectoryUtility.CreateTempDirectory("sguardcli-test");
    }

    [Fact]
    public async Task RunAsync_With_ValidConfig_Should_Return_ExitCode_0()
    {
        // Arrange
        CreateTestConfigFile("sguard.json", @"{
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
        CreateTestConfigFile("appsettings.dev.json", @"{
  ""Test"": {
    ""Key"": ""value""
  }
}");
        
        // Act - use absolute path to avoid directory change issues
        var configAbsolutePath = Path.Combine(_testDirectory, "sguard.json");
        var exitCode = await _cli.RunAsync(["validate", "--config", configAbsolutePath, "--env", "dev"]);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
    }

    [Fact]
    public async Task RunAsync_With_EnvAndAllConflict_Should_Return_ExitCode_2()
    {
        // Arrange
        CreateTestConfigFile("sguard.json", @"{
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
        var exitCode = await _cli.RunAsync(["validate", "--config", configAbsolutePath, "--env", "dev", "--all"]);

        // Assert
        exitCode.Should().Be(ExitCode.SystemError);
    }

    [Fact]
    public async Task RunAsync_With_InvalidOutputFormat_Should_Return_NonZeroExitCode()
    {
        // Arrange
        CreateTestConfigFile("sguard.json", @"{
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
        var exitCode = await _cli.RunAsync(["validate", "--config", configAbsolutePath, "--output", "invalid"]);

        // Assert - System.CommandLine validator should catch this and return non-zero exit code
        exitCode.Should().NotBe(ExitCode.Success, "Invalid output format should result in non-zero exit code");
    }

    private string CreateTestConfigFile(string fileName, string content)
    {
        var filePath = Path.Combine(_testDirectory, fileName);
        FileUtility.WriteAllText(filePath, content);
        return filePath;
    }

    public void Dispose()
    {
      DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
    }
}