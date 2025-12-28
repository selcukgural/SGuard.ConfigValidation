using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Security;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Validators;
using Xunit.Abstractions;
using FileValidator = SGuard.ConfigValidation.Services.FileValidator;

namespace SGuard.ConfigValidation.Tests;

/// <summary>
/// Performance tests for validating system performance and memory efficiency.
/// </summary>
public sealed class PerformanceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _tempDirectory;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"sguard_perf_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task LoadAppSettings_With_LargeFile_Should_Complete_Within_Reasonable_Time()
    {
        // Arrange
        var largeJson = GenerateLargeJson(10000); // 10K keys
        var filePath = Path.Combine(_tempDirectory, "large_appsettings.json");
        File.WriteAllText(filePath, largeJson);
        
        var logger = NullLogger<ConfigLoader>.Instance;
        var securityOptions = Options.Create(new SecurityOptions());
        var configLoader = new ConfigLoader(logger, securityOptions);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await configLoader.LoadAppSettingsAsync(filePath);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().BeGreaterThan(0);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "Large file loading should complete within 5 seconds");
        
        _output.WriteLine($"Loaded {result.Count} settings in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average: {stopwatch.ElapsedMilliseconds / (double)result.Count:F2}ms per setting");
    }

    [Fact]
    public async Task LoadAppSettings_With_VeryLargeFile_Should_Use_Streaming()
    {
        // Arrange - Create a file larger than 1MB to trigger streaming
        var largeJson = GenerateLargeJson(50000); // 50K keys (should be > 1MB)
        var filePath = Path.Combine(_tempDirectory, "very_large_appsettings.json");
        File.WriteAllText(filePath, largeJson);
        
        var fileInfo = new FileInfo(filePath);
        fileInfo.Length.Should().BeGreaterThan(1024 * 1024, "File should be larger than 1MB to test streaming");
        
        var logger = NullLogger<ConfigLoader>.Instance;
        var securityOptions = Options.Create(new SecurityOptions());
        var configLoader = new ConfigLoader(logger, securityOptions);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await configLoader.LoadAppSettingsAsync(filePath);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().BeGreaterThan(0);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000, "Very large file loading should complete within 10 seconds");
        
        _output.WriteLine($"Loaded {result.Count} settings from {fileInfo.Length / 1024}KB file in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void ValidatorFactory_GetValidator_Should_Be_Fast()
    {
        // Arrange
        var logger = NullLogger<ValidatorFactory>.Instance;
        var factory = new ValidatorFactory(logger);
        var validatorTypes = ValidatorConstants.AllValidatorTypes;

        // Act
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            foreach (var type in validatorTypes)
            {
                _ = factory.GetValidator(type);
            }
        }
        stopwatch.Stop();

        // Assert
        var totalLookups = 10000 * validatorTypes.Count;
        var avgTimePerLookup = stopwatch.ElapsedMilliseconds / (double)totalLookups;
        
        avgTimePerLookup.Should().BeLessThan(0.01, "Validator lookup should be very fast (< 0.01ms per lookup)");
        
        _output.WriteLine($"Performed {totalLookups} validator lookups in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average: {avgTimePerLookup:F4}ms per lookup");
    }

    [Fact]
    public void FileValidator_With_ManyRules_Should_Complete_Within_Reasonable_Time()
    {
        // Arrange
        var validatorFactoryLogger = NullLogger<ValidatorFactory>.Instance;
        var validatorFactory = new ValidatorFactory(validatorFactoryLogger);
        var fileValidatorLogger = NullLogger<FileValidator>.Instance;
        var fileValidator = new FileValidator(validatorFactory, fileValidatorLogger);
        
        var appSettings = new Dictionary<string, object>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=TestDb;",
            ["Logging:LogLevel:Default"] = "Information",
            ["ApiKey"] = "test-key-12345"
        };

        var rules = new List<Rule>();
        for (int i = 0; i < 1000; i++)
        {
            rules.Add(new Rule
            {
                Id = $"rule_{i}",
                Environments = ["Development"],
                RuleDetail = new RuleDetail
                {
                    Id = $"rule_detail_{i}",
                    Conditions =
                    [
                        new Condition
                        {
                            Key = "ConnectionStrings:DefaultConnection",
                            Validators = [new ValidatorCondition { Validator = "required", Message = "Connection string is required" }]
                        }
                    ]
                }
            });
        }

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = fileValidator.ValidateFile("test.json", rules, appSettings);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, "Validation with 1000 rules should complete within 2 seconds");
        
        _output.WriteLine($"Validated {rules.Count} rules in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average: {stopwatch.ElapsedMilliseconds / (double)rules.Count:F2}ms per rule");
    }

    [Fact]
    public async Task ConfigLoader_LoadConfig_With_ManyEnvironments_Should_Be_Fast()
    {
        // Arrange
        var configJson = GenerateConfigWithManyEnvironments(100);
        var filePath = Path.Combine(_tempDirectory, "many_environments.json");
        File.WriteAllText(filePath, configJson);
        
        var logger = NullLogger<ConfigLoader>.Instance;
        var securityOptions = Options.Create(new SecurityOptions());
        var configLoader = new ConfigLoader(logger, securityOptions);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await configLoader.LoadConfigAsync(filePath);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        result.Environments.Count.Should().Be(100);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Loading config with 100 environments should complete within 1 second");
        
        _output.WriteLine($"Loaded config with {result.Environments.Count} environments in {stopwatch.ElapsedMilliseconds}ms");
    }

    private static string GenerateLargeJson(int keyCount)
    {
        var keys = new List<string>();
        for (int i = 0; i < keyCount; i++)
        {
            keys.Add($"\"Key{i}\": \"Value{i}\"");
        }
        return "{" + string.Join(",", keys) + "}";
    }

    private static string GenerateConfigWithManyEnvironments(int environmentCount)
    {
        var environments = new List<string>();
        for (int i = 0; i < environmentCount; i++)
        {
            environments.Add($@"{{
                ""id"": ""Env{i}"",
                ""name"": ""Environment {i}"",
                ""path"": ""appsettings.Env{i}.json""
            }}");
        }
        
        var environmentIds = string.Join(",", Enumerable.Range(0, environmentCount).Select(i => $"\"Env{i}\""));
        
        return $@"{{
            ""version"": ""1.0"",
            ""environments"": [{string.Join(",", environments)}],
            ""rules"": [
                {{
                    ""id"": ""test-rule"",
                    ""environments"": [{environmentIds}],
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
    }

    public void Dispose()
    {
        if (!Directory.Exists(_tempDirectory))
        {
            return;
        }

        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

