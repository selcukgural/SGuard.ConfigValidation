using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Exceptions;
using SGuard.ConfigValidation.Services;

namespace SGuard.ConfigValidation.Tests;

public sealed class ConfigLoaderTests : IDisposable
{
    private readonly ConfigLoader _loader;
    private readonly string _testDirectory;
    private readonly ILogger<ConfigLoader> _logger;

    public ConfigLoaderTests()
    {
        _logger = NullLogger<ConfigLoader>.Instance;
        _loader = new ConfigLoader(_logger);
        _testDirectory = SafeFileSystemHelper.CreateSafeTempDirectory("configloader-test");
    }

    [Fact]
    public void LoadConfig_With_ValidFile_Should_Return_Config()
    {
        // Arrange
        var configPath = CreateTestConfigFile("valid-sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": ""appsettings.Development.json"",
      ""description"": ""Development environment""
    }
  ],
  ""rules"": []
}");

        // Act
        var config = _loader.LoadConfig(configPath);

        // Assert
        config.Should().NotBeNull();
        config.Version.Should().Be("1");
        config.Environments.Should().HaveCount(1);
        config.Environments[0].Id.Should().Be("dev");
        config.Rules.Should().BeEmpty();
    }

    [Fact]
    public void LoadConfig_With_NonExistentFile_Should_Throw_FileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() => _loader.LoadConfig(nonExistentPath));
        exception.Message.Should().Contain("nonexistent.json");
    }

    [Fact]
    public void LoadAppSettings_With_ValidFile_Should_Return_Dictionary()
    {
        // Arrange
        var configPath = CreateTestConfigFile("appsettings.json", @"{
  ""ConnectionStrings"": {
    ""DefaultConnection"": ""Server=localhost;""
  },
  ""Logging"": {
    ""LogLevel"": ""Information""
  },
  ""AllowedHosts"": ""*""
}");

        // Act
        var appSettings = _loader.LoadAppSettings(configPath);

        // Assert
        appSettings.Should().HaveCount(3); // ConnectionStrings:DefaultConnection, ConnectionStrings:DefaultConnection, AllowedHosts
        appSettings.Should().ContainKey("ConnectionStrings:DefaultConnection");
        appSettings.Should().ContainKey("Logging:LogLevel");
        appSettings.Should().ContainKey("AllowedHosts");
        appSettings["ConnectionStrings:DefaultConnection"].Should().Be("Server=localhost;");
        appSettings["Logging:LogLevel"].Should().Be("Information");
        appSettings["AllowedHosts"].Should().Be("*");
    }

    [Fact]
    public void LoadConfig_With_SchemaValidation_And_ValidConfig_Should_Return_Config()
    {
        // Arrange
        var schemaPath = CreateTestConfigFile("sguard.schema.json", @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""required"": [""version"", ""environments"", ""rules""],
  ""properties"": {
    ""version"": { ""type"": ""string"" },
    ""environments"": { ""type"": ""array"", ""minItems"": 1 },
    ""rules"": { ""type"": ""array"" }
  }
}");
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": ""appsettings.Development.json""
    }
  ],
  ""rules"": []
}");
        var schemaValidator = new JsonSchemaValidator();
        var logger = NullLogger<ConfigLoader>.Instance;
        var loader = new ConfigLoader(logger, schemaValidator);

        // Act
        var config = loader.LoadConfig(configPath);

        // Assert
        config.Should().NotBeNull();
        config.Version.Should().Be("1");
    }

    [Fact]
    public void LoadConfig_With_SchemaValidation_And_InvalidConfig_Should_Throw_ConfigurationException()
    {
        // Arrange
        var schemaPath = CreateTestConfigFile("sguard.schema.json", @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""type"": ""object"",
  ""required"": [""version"", ""environments"", ""rules""],
  ""properties"": {
    ""version"": { ""type"": ""string"" },
    ""environments"": { ""type"": ""array"", ""minItems"": 1 },
    ""rules"": { ""type"": ""array"" }
  }
}");
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": []
}");
        var schemaValidator = new JsonSchemaValidator();
        var logger = NullLogger<ConfigLoader>.Instance;
        var loader = new ConfigLoader(logger, schemaValidator);

        // Act & Assert
        var exception = Assert.Throws<ConfigurationException>(() => loader.LoadConfig(configPath));
        exception.Message.ToLowerInvariant().Should().Contain("does not match the schema", "Exception message should indicate schema validation failure");
    }

    [Fact]
    public void LoadConfig_With_SchemaValidation_And_NoSchemaFile_Should_Load_WithoutValidation()
    {
        // Arrange
        var configPath = CreateTestConfigFile("sguard.json", @"{
  ""version"": ""1"",
  ""environments"": [
    {
      ""id"": ""dev"",
      ""name"": ""Development"",
      ""path"": ""appsettings.Development.json""
    }
  ],
  ""rules"": []
}");
        var schemaValidator = new JsonSchemaValidator();
        var logger = NullLogger<ConfigLoader>.Instance;
        var loader = new ConfigLoader(logger, schemaValidator);

        // Act
        var config = loader.LoadConfig(configPath);

        // Assert - Should load successfully even without schema file
        config.Should().NotBeNull();
        config.Version.Should().Be("1");
    }

    private string CreateTestConfigFile(string fileName, string content)
    {
        var filePath = Path.Combine(_testDirectory, fileName);
        SafeFileSystemHelper.SafeWriteAllText(filePath, content);
        return filePath;
    }

    [Fact]
    public void LoadConfig_With_YamlFile_Should_UseYamlLoader()
    {
        // Arrange
        var yamlPath = CreateTestConfigFile("sguard.yaml", @"
version: '1'
environments:
  - id: dev
    name: Development
    path: appsettings.Development.json
rules: []
");
        var yamlLogger = NullLogger<YamlLoader>.Instance;
        var yamlLoader = new YamlLoader(yamlLogger);
        var logger = NullLogger<ConfigLoader>.Instance;
        var loader = new ConfigLoader(logger, yamlLoader: yamlLoader);

        // Act
        var config = loader.LoadConfig(yamlPath);

        // Assert
        config.Should().NotBeNull();
        config.Version.Should().Be("1");
        config.Environments.Should().HaveCount(1);
        config.Environments[0].Id.Should().Be("dev");
    }

    [Fact]
    public void LoadAppSettings_With_YamlFile_Should_UseYamlLoader()
    {
        // Arrange
        var yamlPath = CreateTestConfigFile("appsettings.yaml", @"
ConnectionStrings:
  DefaultConnection: Server=localhost;
Logging:
  LogLevel: Information
");
        var yamlLogger = NullLogger<YamlLoader>.Instance;
        var yamlLoader = new YamlLoader(yamlLogger);
        var logger = NullLogger<ConfigLoader>.Instance;
        var loader = new ConfigLoader(logger, yamlLoader: yamlLoader);

        // Act
        var appSettings = loader.LoadAppSettings(yamlPath);

        // Assert
        appSettings.Should().HaveCount(2);
        appSettings.Should().ContainKey("ConnectionStrings:DefaultConnection");
        appSettings.Should().ContainKey("Logging:LogLevel");
    }

    public void Dispose()
    {
        SafeFileSystemHelper.SafeDeleteDirectory(_testDirectory, recursive: true);
    }
}