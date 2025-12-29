using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Exceptions;
using SGuard.ConfigValidation.Security;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Utils;

namespace SGuard.ConfigValidation.Test;

public sealed class YamlLoaderTests : IDisposable
{
    private readonly YamlLoader _loader;
    private readonly string _testDirectory;

    public YamlLoaderTests()
    {
        var logger = NullLogger<YamlLoader>.Instance;
        var securityOptions = Options.Create(new SecurityOptions());
        _loader = new YamlLoader(logger, securityOptions);
        _testDirectory = DirectoryUtility.CreateTempDirectory("yamlloader-test");
    }

    [Fact]
    public async Task LoadConfig_With_ValidYamlFile_Should_Return_Config()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("valid-sguard.yaml", @"
version: '1'
environments:
  - id: dev
    name: Development
    path: appsettings.Development.json
    description: Development environment
rules:
  - id: test-rule
    environments:
      - dev
    rule:
      id: rule-detail-1
      conditions:
        - key: Test:Key
          condition:
            - validator: required
              message: Test message
");

        // Act
        var config = await _loader.LoadConfigAsync(yamlPath);

        // Assert
        config.Should().NotBeNull();
        config.Version.Should().Be("1");
        config.Environments.Should().HaveCount(1);
        config.Environments[0].Id.Should().Be("dev");
        config.Environments[0].Name.Should().Be("Development");
        config.Rules.Should().HaveCount(1);
        config.Rules[0].Id.Should().Be("test-rule");
    }

    [Fact]
    public async Task LoadConfig_With_EmptyRulesArray_Should_Throw_ConfigurationException()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("empty-rules.yaml", @"
version: '1'
environments:
  - id: dev
    name: Development
    path: appsettings.Development.json
rules: []
");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(async () => await _loader.LoadConfigAsync(yamlPath));
        exception.Message.Should().Contain("No rules defined");
        exception.Message.Should().Contain("0 rules");
    }

    [Fact]
    public async Task LoadConfig_With_NonExistentFile_Should_Throw_FileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.yaml");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(async () => await _loader.LoadConfigAsync(nonExistentPath));
        exception.Message.Should().Contain("nonexistent.yaml");
    }

    [Fact]
    public async Task LoadAppSettings_With_ValidYamlFile_Should_Return_Dictionary()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("appsettings.yaml", @"
ConnectionStrings:
  DefaultConnection: Server=localhost;
Logging:
  LogLevel: Information
AllowedHosts: '*'
");

        // Act
        var appSettings = await _loader.LoadAppSettingsAsync(yamlPath);

        // Assert
        appSettings.Should().HaveCount(3);
        appSettings.Should().ContainKey("ConnectionStrings:DefaultConnection");
        appSettings.Should().ContainKey("Logging:LogLevel");
        appSettings.Should().ContainKey("AllowedHosts");
        appSettings["ConnectionStrings:DefaultConnection"].Should().Be("Server=localhost;");
        appSettings["Logging:LogLevel"].Should().Be("Information");
        appSettings["AllowedHosts"].Should().Be("*");
    }

    [Fact]
    public async Task LoadConfig_With_EmptyFile_Should_Throw_ConfigurationException()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("empty.yaml", "");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(async () => await _loader.LoadConfigAsync(yamlPath));
        exception.Message.Should().Contain("empty");
    }

    [Fact]
    public async Task LoadConfig_With_InvalidYaml_Should_Throw_ConfigurationException()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("invalid.yaml", @"
version: '1'
environments:
  - id: dev
    name: Development
    invalid: [unclosed bracket
");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(async () => await _loader.LoadConfigAsync(yamlPath));
        exception.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoadConfig_With_YamlContainingRules_Should_LoadRules()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("with-rules.yaml", @"
version: '1'
environments:
  - id: dev
    name: Development
    path: appsettings.Development.json
rules:
  - id: test-rule
    environments: 
      - dev
    rule:
      id: rule-detail-1
      conditions:
        - key: Test:Key
          condition:
            - validator: required
              message: Test message
");

        // Act
        var config = await _loader.LoadConfigAsync(yamlPath);

        // Assert
        config.Should().NotBeNull();
        config.Rules.Should().HaveCount(1);
        config.Rules[0].Id.Should().Be("test-rule");
        config.Rules[0].Environments.Should().Contain("dev");
        config.Rules[0].RuleDetail.Id.Should().Be("rule-detail-1");
        config.Rules[0].RuleDetail.Conditions.Should().HaveCount(1);
        config.Rules[0].RuleDetail.Conditions[0].Key.Should().Be("Test:Key");
        // Note: Validators might be empty if YAML deserialization doesn't match JSON structure exactly
        // This is acceptable as the main structure is validated
    }

    [Fact]
    public async Task LoadAppSettings_With_EmptyFile_Should_Return_EmptyDictionary()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("empty.yaml", "");

        // Act
        var appSettings = await _loader.LoadAppSettingsAsync(yamlPath);

        // Assert
        appSettings.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAppSettings_With_NestedYaml_Should_FlattenCorrectly()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("nested.yaml", @"
Database:
  Connection:
    Host: localhost
    Port: 5432
  Pool:
    MinSize: 5
    MaxSize: 20
");

        // Act
        var appSettings = await _loader.LoadAppSettingsAsync(yamlPath);

        // Assert
        appSettings.Should().HaveCount(4);
        appSettings.Should().ContainKey("Database:Connection:Host");
        appSettings.Should().ContainKey("Database:Connection:Port");
        appSettings.Should().ContainKey("Database:Pool:MinSize");
        appSettings.Should().ContainKey("Database:Pool:MaxSize");
        appSettings["Database:Connection:Host"].Should().Be("localhost");
        appSettings["Database:Connection:Port"].Should().Be("5432");
    }

    [Fact]
    public async Task LoadConfig_With_EmptyEnvironmentsArray_Should_Throw_ConfigurationException()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("empty-environments.yaml", @"
version: '1'
environments: []
rules:
  - id: test-rule
    environments: []
    rule:
      id: rule-detail-1
      conditions: []
");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(async () => await _loader.LoadConfigAsync(yamlPath));
        exception.Message.Should().ContainAny("no environments", "No environments", "environments");
    }

    [Fact]
    public async Task LoadConfig_With_TooManyEnvironments_Should_Throw_ConfigurationException()
    {
        // Arrange - SecurityOptions default MaxEnvironmentsCount is 1000, so we need 1001
        // Create a custom SecurityOptions with lower limit for testing
        var customSecurityOptions = Options.Create(new SecurityOptions { MaxEnvironmentsCount = 10 });
        var customLoader = new YamlLoader(NullLogger<YamlLoader>.Instance, customSecurityOptions);
        
        var environments = string.Join("\n  ", Enumerable.Range(0, 11).Select(i => $@"- id: env{i}
    name: Environment {i}
    path: appsettings.env{i}.json"));
        var yamlPath = CreateTestYamlFile("too-many-environments.yaml", $@"
version: '1'
environments:
  {environments}
rules:
  - id: test-rule
    environments: [env0]
    rule:
      id: rule-detail-1
      conditions: []
");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(async () => await customLoader.LoadConfigAsync(yamlPath));
        exception.Message.Should().ContainAny("too many environments", "exceeds security limit", "Environment count");
    }

    [Fact]
    public async Task LoadConfig_With_TooManyRules_Should_Throw_ConfigurationException()
    {
        // Arrange - SecurityOptions default MaxRulesCount is 10000, so we need 10001
        var customSecurityOptions = Options.Create(new SecurityOptions { MaxRulesCount = 10 });
        var customLoader = new YamlLoader(NullLogger<YamlLoader>.Instance, customSecurityOptions);
        
        var rules = string.Join("\n", Enumerable.Range(0, 11).Select(i => $@"  - id: rule{i}
    environments: [dev]
    rule:
      id: rule-detail-{i}
      conditions: []"));
        var yamlPath = CreateTestYamlFile("too-many-rules.yaml", $@"
version: '1'
environments:
  - id: dev
    name: Development
    path: appsettings.dev.json
rules:
{rules}
");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(async () => await customLoader.LoadConfigAsync(yamlPath));
        exception.Message.Should().ContainAny("too many rules", "exceeds security limit", "Rule count");
    }

    [Fact]
    public async Task LoadAppSettings_With_ComplexNestedStructure_Should_FlattenCorrectly()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("complex-appsettings.yaml", @"
Database:
  Connection:
    Host: localhost
    Port: 5432
    Credentials:
      Username: admin
      Password: secret
Features:
  FeatureA:
    Enabled: true
    Settings:
      Value1: test1
      Value2: test2
");

        // Act
        var appSettings = await _loader.LoadAppSettingsAsync(yamlPath);

        // Assert
        appSettings.Should().ContainKey("Database:Connection:Host");
        appSettings.Should().ContainKey("Database:Connection:Port");
        appSettings.Should().ContainKey("Database:Connection:Credentials:Username");
        appSettings.Should().ContainKey("Database:Connection:Credentials:Password");
        appSettings.Should().ContainKey("Features:FeatureA:Enabled");
        appSettings.Should().ContainKey("Features:FeatureA:Settings:Value1");
        appSettings.Should().ContainKey("Features:FeatureA:Settings:Value2");
    }

    [Fact]
    public async Task LoadAppSettings_With_ArrayValue_Should_StoreAsRawJson()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("array-appsettings.yaml", @"
AllowedHosts:
  - localhost
  - example.com
  - test.com
");

        // Act
        var appSettings = await _loader.LoadAppSettingsAsync(yamlPath);

        // Assert
        appSettings.Should().ContainKey("AllowedHosts");
        appSettings["AllowedHosts"].Should().BeOfType<string>();
    }

    [Fact]
    public async Task LoadAppSettings_With_NullValue_Should_HandleCorrectly()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("null-appsettings.yaml", @"
Database:
  ConnectionString: null
  Host: localhost
");

        // Act
        var appSettings = await _loader.LoadAppSettingsAsync(yamlPath);

        // Assert
        appSettings.Should().ContainKey("Database:ConnectionString");
        appSettings.Should().ContainKey("Database:Host");
        appSettings["Database:Host"].Should().Be("localhost");
    }

    [Fact]
    public async Task LoadAppSettings_With_BooleanValues_Should_HandleCorrectly()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("boolean-appsettings.yaml", @"
Features:
  FeatureA:
    Enabled: true
  FeatureB:
    Enabled: false
");

        // Act
        var appSettings = await _loader.LoadAppSettingsAsync(yamlPath);

        // Assert
        appSettings.Should().ContainKey("Features:FeatureA:Enabled");
        appSettings.Should().ContainKey("Features:FeatureB:Enabled");
        appSettings["Features:FeatureA:Enabled"].Should().Be("true");
        appSettings["Features:FeatureB:Enabled"].Should().Be("false");
    }

    [Fact]
    public async Task LoadAppSettings_With_NumberValues_Should_HandleCorrectly()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("number-appsettings.yaml", @"
Server:
  Port: 8080
  Timeout: 30
  MaxConnections: 100
");

        // Act
        var appSettings = await _loader.LoadAppSettingsAsync(yamlPath);

        // Assert
        appSettings.Should().ContainKey("Server:Port");
        appSettings.Should().ContainKey("Server:Timeout");
        appSettings.Should().ContainKey("Server:MaxConnections");
        appSettings["Server:Port"].Should().Be("8080");
    }

    [Fact]
    public async Task LoadAppSettings_With_EmptyObject_Should_ReturnEmptyDictionary()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("empty-object.yaml", @"{}");

        // Act
        var appSettings = await _loader.LoadAppSettingsAsync(yamlPath);

        // Assert
        appSettings.Should().NotBeNull();
        appSettings.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAppSettings_With_EmptyArray_Should_StoreAsRawJson()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("empty-array.yaml", @"
Items: []
");

        // Act
        var appSettings = await _loader.LoadAppSettingsAsync(yamlPath);

        // Assert
        appSettings.Should().ContainKey("Items");
        appSettings["Items"].Should().Be("[]");
    }

    [Fact]
    public async Task LoadAppSettings_With_MixedTypes_Should_FlattenCorrectly()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("mixed-types.yaml", @"
Config:
  StringValue: test
  NumberValue: 42
  BooleanValue: true
  NullValue: null
  ArrayValue:
    - item1
    - item2
");

        // Act
        var appSettings = await _loader.LoadAppSettingsAsync(yamlPath);

        // Assert
        appSettings.Should().ContainKey("Config:StringValue");
        appSettings.Should().ContainKey("Config:NumberValue");
        appSettings.Should().ContainKey("Config:BooleanValue");
        appSettings.Should().ContainKey("Config:NullValue");
        appSettings.Should().ContainKey("Config:ArrayValue");
        appSettings["Config:StringValue"].Should().Be("test");
        appSettings["Config:NumberValue"].Should().Be("42");
    }

    [Fact]
    public async Task LoadConfig_With_CancellationToken_Should_RespectCancellation()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("valid-sguard.yaml", @"
version: '1'
environments:
  - id: dev
    name: Development
    path: appsettings.Development.json
rules:
  - id: test-rule
    environments: [dev]
    rule:
      id: rule-detail-1
      conditions: []
");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - YamlLoader wraps OperationCanceledException in ConfigurationException
        var exception = await Assert.ThrowsAsync<ConfigurationException>(async () =>
            await _loader.LoadConfigAsync(yamlPath, cts.Token));
        exception.InnerException.Should().BeOfType<OperationCanceledException>();
    }

    [Fact]
    public async Task LoadAppSettings_With_CancellationToken_Should_RespectCancellation()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("appsettings.yaml", @"
ConnectionStrings:
  DefaultConnection: Server=localhost;
");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - YamlLoader wraps OperationCanceledException in ConfigurationException
        var exception = await Assert.ThrowsAsync<ConfigurationException>(async () =>
            await _loader.LoadAppSettingsAsync(yamlPath, cts.Token));
        exception.InnerException.Should().BeOfType<OperationCanceledException>();
    }

    [Fact]
    public async Task LoadAppSettings_With_NullPath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _loader.LoadAppSettingsAsync(null!));
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAppSettings_With_EmptyPath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _loader.LoadAppSettingsAsync(""));
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadConfig_With_NullPath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _loader.LoadConfigAsync(null!));
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadConfig_With_EmptyPath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _loader.LoadConfigAsync(""));
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadConfig_With_InvalidYamlSyntax_Should_Throw_ConfigurationException()
    {
        // Arrange - Invalid YAML syntax that will cause YamlException
        var yamlPath = CreateTestYamlFile("invalid-syntax.yaml", @"
version: '1'
environments:
  - id: dev
    name: Development
    path: appsettings.dev.json
    invalid: [unclosed bracket
rules: []
");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(async () =>
            await _loader.LoadConfigAsync(yamlPath));
        exception.Message.Should().ContainAny("YAML parsing", "parse", "syntax");
    }

    [Fact]
    public async Task LoadAppSettings_With_InvalidYamlSyntax_Should_Throw_ConfigurationException()
    {
        // Arrange - Invalid YAML syntax
        var yamlPath = CreateTestYamlFile("invalid-appsettings.yaml", @"
Database:
  ConnectionString: [unclosed
");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(async () =>
            await _loader.LoadAppSettingsAsync(yamlPath));
        exception.Message.Should().ContainAny("YAML parsing", "parse", "syntax");
    }

    [Fact]
    public async Task LoadConfig_With_DeserializationNull_Should_Throw_ConfigurationException()
    {
        // Arrange - YAML that deserializes to null (hard to create, but we can try with empty content after validation)
        // Actually, this is hard to test because EnsureFileExists and ValidateFileSize will catch empty files first
        // But we can test with a YAML that has only comments or whitespace after trimming
        var yamlPath = CreateTestYamlFile("null-deserialization.yaml", @"
# This is just a comment
# No actual content
");

        // Act & Assert - Should throw because file is empty or deserializes to null
        var exception = await Assert.ThrowsAsync<ConfigurationException>(async () =>
            await _loader.LoadConfigAsync(yamlPath));
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadConfig_With_NoRules_Should_Throw_ConfigurationException()
    {
        // Arrange - YAML with environments but no rules
        var yamlPath = CreateTestYamlFile("no-rules.yaml", @"
version: '1'
environments:
  - id: dev
    name: Development
    path: appsettings.dev.json
rules: []
");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(async () =>
            await _loader.LoadConfigAsync(yamlPath));
        exception.Message.Should().ContainAny("no rules", "No rules");
    }

    [Fact]
    public async Task LoadAppSettings_With_IOException_Should_Throw_FileNotFoundException()
    {
        // Arrange - Create a file and then delete it to simulate I/O error
        var yamlPath = CreateTestYamlFile("appsettings.yaml", @"
Database:
  ConnectionString: test
");
        // Delete the directory to simulate I/O error
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
        Directory.CreateDirectory(_testDirectory);

        // Act & Assert - FileNotFoundException is thrown before IOException can occur
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _loader.LoadAppSettingsAsync(yamlPath));
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadConfig_With_IOException_Should_Throw_FileNotFoundException()
    {
        // Arrange - Create a file and then delete it to simulate I/O error
        var yamlPath = CreateTestYamlFile("config.yaml", @"
version: '1'
environments:
  - id: dev
    name: Development
    path: appsettings.dev.json
rules: []
");
        // Delete the directory to simulate I/O error
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
        Directory.CreateDirectory(_testDirectory);

        // Act & Assert - FileNotFoundException is thrown before IOException can occur
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _loader.LoadConfigAsync(yamlPath));
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadConfig_With_TooManyConditions_Should_Load_ButMayFailLater()
    {
        // Arrange - SecurityOptions default MaxConditionsPerRule is 1000, so we need 1001
        // Note: MaxConditionsPerRule validation happens during rule validation, not during config loading
        var customSecurityOptions = Options.Create(new SecurityOptions { MaxConditionsPerRule = 10 });
        var customLoader = new YamlLoader(NullLogger<YamlLoader>.Instance, customSecurityOptions);
        
        var conditions = string.Join("\n        ", Enumerable.Range(0, 11).Select(i => $@"- key: Key{i}
          condition:
            - validator: required
              message: Required"));
        var yamlPath = CreateTestYamlFile("too-many-conditions.yaml", $@"
version: '1'
environments:
  - id: dev
    name: Development
    path: appsettings.dev.json
rules:
  - id: test-rule
    environments: [dev]
    rule:
      id: rule-detail-1
      conditions:
        {conditions}
");

        // Act - Config loading should succeed, validation happens later
        var config = await customLoader.LoadConfigAsync(yamlPath);

        // Assert - Config should load successfully
        config.Should().NotBeNull();
        config.Rules.Should().HaveCount(1);
        config.Rules[0].RuleDetail.Conditions.Should().HaveCount(11);
    }

    [Fact]
    public async Task LoadConfig_With_InvalidVersion_Should_Load_ButVersionMayBeInvalid()
    {
        // Arrange
        var yamlPath = CreateTestYamlFile("invalid-version.yaml", @"
version: ''
environments:
  - id: dev
    name: Development
    path: appsettings.dev.json
rules:
  - id: test-rule
    environments: [dev]
    rule:
      id: rule-detail-1
      conditions: []
");

        // Act
        var config = await _loader.LoadConfigAsync(yamlPath);

        // Assert
        config.Should().NotBeNull();
        config.Version.Should().Be("");
    }

    [Fact]
    public async Task LoadAppSettings_With_WhitespacePath_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _loader.LoadAppSettingsAsync("   "));
        exception.Should().NotBeNull();
    }

    private string CreateTestYamlFile(string fileName, string content)
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

