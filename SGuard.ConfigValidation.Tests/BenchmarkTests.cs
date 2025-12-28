using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Results;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Validators;

namespace SGuard.ConfigValidation.Tests;

/// <summary>
/// BenchmarkDotNet benchmarks for performance-critical operations.
/// Run with: dotnet run --project SGuard.ConfigValidation.Tests --configuration Release
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkTests
{
    private ConfigLoader _configLoader = null!;
    private FileValidator _fileValidator = null!;
    private ValidatorFactory _validatorFactory = null!;
    private string _tempDirectory = null!;
    private string _largeJsonFile = null!;
    private Dictionary<string, object> _appSettings = null!;
    private List<Rule> _rules = null!;

    [GlobalSetup]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"sguard_bench_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);

        var logger = NullLogger<ConfigLoader>.Instance;
        var securityOptions = Options.Create(new SecurityOptions());
        _configLoader = new ConfigLoader(logger, securityOptions);

        var validatorFactoryLogger = NullLogger<ValidatorFactory>.Instance;
        _validatorFactory = new ValidatorFactory(validatorFactoryLogger);

        var fileValidatorLogger = NullLogger<FileValidator>.Instance;
        _fileValidator = new FileValidator(_validatorFactory, fileValidatorLogger);

        // Create large JSON file
        var largeJson = GenerateLargeJson(10000);
        _largeJsonFile = Path.Combine(_tempDirectory, "large_appsettings.json");
        File.WriteAllText(_largeJsonFile, largeJson);

        // Setup app settings
        _appSettings = new Dictionary<string, object>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=TestDb;",
            ["Logging:LogLevel:Default"] = "Information",
            ["ApiKey"] = "test-key-12345"
        };

        // Setup rules
        _rules = [];
        for (int i = 0; i < 100; i++)
        {
            _rules.Add(new Rule
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
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDirectory))
        {
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

    [Benchmark]
    public Dictionary<string, object> LoadAppSettings_LargeFile()
    {
        return _configLoader.LoadAppSettingsAsync(_largeJsonFile).GetAwaiter().GetResult();
    }

    [Benchmark]
    public IValidator<object> ValidatorFactory_GetValidator()
    {
        return _validatorFactory.GetValidator("required");
    }

    [Benchmark]
    public FileValidationResult FileValidator_ValidateFile()
    {
        return _fileValidator.ValidateFile("test.json", _rules, _appSettings);
    }

    [Benchmark]
    public List<ValidationResult> FileValidationResult_Errors()
    {
        var result = _fileValidator.ValidateFile("test.json", _rules, _appSettings);
        // Access Errors property multiple times to test caching
        _ = result.Errors;
        _ = result.Errors;
        return result.Errors;
    }

    private static string GenerateLargeJson(int keyCount)
    {
        var keys = new List<string>(keyCount);
        for (int i = 0; i < keyCount; i++)
        {
            keys.Add($"\"Key{i}\": \"Value{i}\"");
        }
        return "{" + string.Join(",", keys) + "}";
    }
}

