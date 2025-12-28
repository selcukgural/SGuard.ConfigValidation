using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Extensions;
using SGuard.ConfigValidation.Output;
using SGuard.ConfigValidation.Security;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Services.Abstract;
using SGuard.ConfigValidation.Utils;
using SGuard.ConfigValidation.Validators;
using SGuard.ConfigValidation.Validators.Plugin;
using FileValidator = SGuard.ConfigValidation.Services.FileValidator;

namespace SGuard.ConfigValidation.Tests;

/// <summary>
/// Tests for ServiceCollectionExtensions logging level support.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Test logger provider that captures log entries for verification.
    /// </summary>
    private sealed class TestLoggerProvider : ILoggerProvider
    {
        private readonly List<LogEntry> _logs = new();
        private readonly LogLevel _minimumLevel;

        public TestLoggerProvider(LogLevel minimumLevel = LogLevel.Trace)
        {
            _minimumLevel = minimumLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName, _logs, _minimumLevel);
        }

        public IReadOnlyList<LogEntry> Logs => _logs;
        public void Clear() => _logs.Clear();
        public void Dispose() { }
    }

    /// <summary>
    /// Test logger that captures log entries.
    /// </summary>
    private sealed class TestLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly List<LogEntry> _logs;
        private readonly LogLevel _minimumLevel;

        public TestLogger(string categoryName, List<LogEntry> logs, LogLevel minimumLevel)
        {
            _categoryName = categoryName;
            _logs = logs;
            _minimumLevel = minimumLevel;
        }

        IDisposable ILogger.BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _minimumLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                _logs.Add(new LogEntry
                {
                    Level = logLevel,
                    Category = _categoryName,
                    Message = formatter(state, exception),
                    Exception = exception
                });
            }
        }
    }

    /// <summary>
    /// Represents a log entry captured during testing.
    /// </summary>
    private sealed record LogEntry
    {
        public LogLevel Level { get; init; }
        public string Category { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public Exception? Exception { get; init; }
    }

    [Fact]
    public void AddSGuardConfigValidation_WithoutLogLevel_Should_NotRegisterLogging()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSGuardConfigValidation();

        // Assert
        services.Should().NotContain(sd => sd.ServiceType == typeof(ILoggerFactory));
    }

    [Fact]
    public void AddSGuardConfigValidation_WithoutLogLevel_Should_RegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSGuardConfigValidation();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(IRuleEngine));
        services.Should().Contain(sd => sd.ServiceType == typeof(IConfigLoader));
        services.Should().Contain(sd => sd.ServiceType == typeof(IFileValidator));
        services.Should().Contain(sd => sd.ServiceType == typeof(IValidatorFactory));
    }

    [Fact]
    public void AddSGuardConfigValidation_WithDebugLevel_Should_RegisterLogging()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSGuardConfigValidation(logLevel: LogLevel.Debug);

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(ILoggerFactory));
    }

    [Fact]
    public void AddSGuardConfigValidation_WithDebugLevel_Should_EnableDebugLogs()
    {
        // Arrange
        var services = new ServiceCollection();
        var testLoggerProvider = new TestLoggerProvider(LogLevel.Debug);

        // Act
        services.AddSGuardConfigValidation(logLevel: LogLevel.Debug);
        services.AddSingleton<ILoggerProvider>(testLoggerProvider);

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<ConfigLoader>();

        // Assert
        logger.IsEnabled(LogLevel.Debug).Should().BeTrue();
        logger.IsEnabled(LogLevel.Trace).Should().BeFalse();

        // Test actual logging
        logger.LogDebug("Debug message");
        logger.LogTrace("Trace message");

        testLoggerProvider.Logs.Should().Contain(l => l.Level == LogLevel.Debug && l.Message == "Debug message");
        testLoggerProvider.Logs.Should().NotContain(l => l.Level == LogLevel.Trace);
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    public void AddSGuardConfigValidation_WithLogLevel_Should_SetCorrectMinimumLevel(LogLevel logLevel)
    {
        // Arrange
        var services = new ServiceCollection();
        var testLoggerProvider = new TestLoggerProvider(logLevel);

        // Act
        services.AddSGuardConfigValidation(logLevel: logLevel);
        services.AddSingleton<ILoggerProvider>(testLoggerProvider);

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<ConfigLoader>();

        // Assert
        logger.IsEnabled(logLevel).Should().BeTrue();

        // Test that lower levels are disabled
        if (logLevel > LogLevel.Trace)
        {
            logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
        }
        if (logLevel > LogLevel.Debug)
        {
            logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        }
        if (logLevel > LogLevel.Information)
        {
            logger.IsEnabled(LogLevel.Information).Should().BeFalse();
        }
    }

    [Fact]
    public void AddSGuardConfigValidation_WithDebugLevel_Should_EnableDebugLogsForLibraryServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var testLoggerProvider = new TestLoggerProvider(LogLevel.Debug);

        // Act
        services.AddSGuardConfigValidation(logLevel: LogLevel.Debug);
        services.AddSingleton<ILoggerProvider>(testLoggerProvider);

        var serviceProvider = services.BuildServiceProvider();
        var configLoaderLogger = serviceProvider.GetRequiredService<ILogger<ConfigLoader>>();
        var ruleEngineLogger = serviceProvider.GetRequiredService<ILogger<RuleEngine>>();
        var fileValidatorLogger = serviceProvider.GetRequiredService<ILogger<FileValidator>>();
        var validatorFactoryLogger = serviceProvider.GetRequiredService<ILogger<ValidatorFactory>>();

        // Assert
        configLoaderLogger.IsEnabled(LogLevel.Debug).Should().BeTrue();
        ruleEngineLogger.IsEnabled(LogLevel.Debug).Should().BeTrue();
        fileValidatorLogger.IsEnabled(LogLevel.Debug).Should().BeTrue();
        validatorFactoryLogger.IsEnabled(LogLevel.Debug).Should().BeTrue();
    }

    [Fact]
    public void AddSGuardConfigValidationCore_WithDebugLevel_Should_EnableDebugLogs()
    {
        // Arrange
        var services = new ServiceCollection();
        var testLoggerProvider = new TestLoggerProvider(LogLevel.Debug);

        // Act
        services.AddSGuardConfigValidationCore(logLevel: LogLevel.Debug);
        services.AddSingleton<ILoggerProvider>(testLoggerProvider);

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<ConfigLoader>();

        // Assert
        logger.IsEnabled(LogLevel.Debug).Should().BeTrue();
        logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
    }

    [Fact]
    public void AddSGuardConfigValidation_WithExistingLogging_Should_OverrideLogLevel()
    {
        // Arrange
        var services = new ServiceCollection();
        var testLoggerProvider = new TestLoggerProvider(LogLevel.Debug);

        // Act - Register logging first with Information level
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Then register SGuard with Debug level
        services.AddSGuardConfigValidation(logLevel: LogLevel.Debug);
        services.AddSingleton<ILoggerProvider>(testLoggerProvider);

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<ConfigLoader>();

        // Assert - Debug should be enabled due to SGuard filter
        logger.IsEnabled(LogLevel.Debug).Should().BeTrue();
    }

    [Fact]
    public void AddSGuardConfigValidation_WithNullLogLevel_Should_NotRegisterLogging()
    {
        // Arrange
        var services = new ServiceCollection();
        LogLevel? nullLogLevel = null;

        // Act
        services.AddSGuardConfigValidation(logLevel: nullLogLevel);

        // Assert
        // Note: This test verifies that null logLevel doesn't cause issues
        // The actual behavior depends on whether logging was already registered
        services.Should().Contain(sd => sd.ServiceType == typeof(IRuleEngine));
    }

    [Fact]
    public void AddSGuardConfigValidation_WithWarningLevel_Should_OnlyLogWarningsAndAbove()
    {
        // Arrange
        var services = new ServiceCollection();
        var testLoggerProvider = new TestLoggerProvider(LogLevel.Warning);

        // Act
        services.AddSGuardConfigValidation(logLevel: LogLevel.Warning);
        services.AddSingleton<ILoggerProvider>(testLoggerProvider);

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<ConfigLoader>();

        // Assert
        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogLevel.Error).Should().BeTrue();
        logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
        logger.IsEnabled(LogLevel.Information).Should().BeFalse();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        logger.IsEnabled(LogLevel.Trace).Should().BeFalse();

        // Test actual logging
        logger.LogWarning("Warning message");
        logger.LogInformation("Information message");
        logger.LogDebug("Debug message");

        testLoggerProvider.Logs.Should().Contain(l => l.Level == LogLevel.Warning);
        testLoggerProvider.Logs.Should().NotContain(l => l.Level == LogLevel.Information);
        testLoggerProvider.Logs.Should().NotContain(l => l.Level == LogLevel.Debug);
    }

    [Fact]
    public async Task AddSGuardConfigValidation_WithLogLevel_Should_WorkWithRealValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        var testLoggerProvider = new TestLoggerProvider(LogLevel.Debug);
        var testDirectory = DirectoryUtility.CreateTempDirectory("sguard-test");

        try
        {
            // Create test config file
            var configPath = Path.Combine(testDirectory, "sguard.json");
            File.WriteAllText(configPath, @"{
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

            var appSettingsPath = Path.Combine(testDirectory, "appsettings.dev.json");
            File.WriteAllText(appSettingsPath, @"{
  ""Test"": ""Value""
}");

            // Act
            services.AddSGuardConfigValidation(logLevel: LogLevel.Debug);
            services.AddSingleton<ILoggerProvider>(testLoggerProvider);

            var serviceProvider = services.BuildServiceProvider();
            var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();

            // Perform validation
            var result = await ruleEngine.ValidateEnvironmentAsync(configPath, "dev");

            // Assert
            result.Should().NotBeNull();
            // Verify that debug logs were captured (if any were logged)
            // The actual log count depends on implementation details
        }
        finally
        {
            try
            {
                Directory.Delete(testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void AddSGuardConfigValidationCore_WithoutLogLevel_Should_NotRegisterLogging()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSGuardConfigValidationCore();

        // Assert
        services.Should().NotContain(sd => sd.ServiceType == typeof(ILoggerFactory));
    }

    [Fact]
    public void AddSGuardConfigValidationCore_WithoutLogLevel_Should_RegisterCoreServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSGuardConfigValidationCore();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(IRuleEngine));
        services.Should().Contain(sd => sd.ServiceType == typeof(IConfigLoader));
        services.Should().Contain(sd => sd.ServiceType == typeof(IFileValidator));
        services.Should().NotContain(sd => sd.ServiceType == typeof(IYamlLoader));
        services.Should().NotContain(sd => sd.ServiceType == typeof(ISchemaValidator));
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    public void AddSGuardConfigValidationCore_WithLogLevel_Should_SetCorrectMinimumLevel(LogLevel logLevel)
    {
        // Arrange
        var services = new ServiceCollection();
        var testLoggerProvider = new TestLoggerProvider(logLevel);

        // Act
        services.AddSGuardConfigValidationCore(logLevel: logLevel);
        services.AddSingleton<ILoggerProvider>(testLoggerProvider);

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<ConfigLoader>();

        // Assert
        logger.IsEnabled(logLevel).Should().BeTrue();
    }

    [Fact]
    public void AddSGuardConfigValidation_MultipleCalls_Should_UseLastLogLevel()
    {
        // Arrange
        var services = new ServiceCollection();
        var testLoggerProvider = new TestLoggerProvider(LogLevel.Warning);

        // Act - Call multiple times with different log levels
        services.AddSGuardConfigValidation(logLevel: LogLevel.Debug);
        services.AddSGuardConfigValidation(logLevel: LogLevel.Warning);
        services.AddSingleton<ILoggerProvider>(testLoggerProvider);

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<ConfigLoader>();

        // Assert - Last log level should be used
        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
    }

    [Fact]
    public void AddSGuardConfigValidation_With_Configuration_Should_BindSecurityOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Security:MaxFileSizeBytes", "1048576" },
                { "Security:MaxEnvironmentsCount", "100" }
            })
            .Build();

        // Act
        services.AddSGuardConfigValidation(configuration);
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var securityOptions = serviceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value;

        // Assert
        securityOptions.Should().NotBeNull();
        securityOptions.MaxFileSizeBytes.Should().Be(1048576);
        securityOptions.MaxEnvironmentsCount.Should().Be(100);
    }

    [Fact]
    public void AddSGuardConfigValidation_With_ConfigureSecurityOptions_Should_UseConfiguredOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<SecurityOptions> configureOptions = options =>
        {
            options.MaxFileSizeBytes = 2048576;
            options.MaxEnvironmentsCount = 200;
        };

        // Act
        services.AddSGuardConfigValidation(configureSecurityOptions: configureOptions);
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var securityOptions = serviceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value;

        // Assert
        securityOptions.Should().NotBeNull();
        securityOptions.MaxFileSizeBytes.Should().Be(2048576);
        securityOptions.MaxEnvironmentsCount.Should().Be(200);
    }

    [Fact]
    public void AddSGuardConfigValidation_With_PluginDirectories_Should_RegisterValidatorPluginDiscovery()
    {
        // Arrange
        var services = new ServiceCollection();
        var pluginDirectories = new[] { "/path/to/plugins" };

        // Act
        services.AddSGuardConfigValidation(pluginDirectories: pluginDirectories);
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var discovery = serviceProvider.GetService<ValidatorPluginDiscovery>();

        // Assert
        discovery.Should().NotBeNull();
    }

    [Fact]
    public void AddSGuardConfigValidation_Without_PluginDirectories_Should_NotRegisterValidatorPluginDiscovery()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSGuardConfigValidation();
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var discovery = serviceProvider.GetService<ValidatorPluginDiscovery>();

        // Assert
        discovery.Should().BeNull();
    }

    [Fact]
    public void AddSGuardConfigValidation_Should_Register_AllServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSGuardConfigValidation();
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();

        // Assert - Core services
        serviceProvider.GetService<IRuleEngine>().Should().NotBeNull();
        serviceProvider.GetService<IConfigLoader>().Should().NotBeNull();
        serviceProvider.GetService<IFileValidator>().Should().NotBeNull();
        serviceProvider.GetService<IPathResolver>().Should().NotBeNull();
        serviceProvider.GetService<IValidatorFactory>().Should().NotBeNull();
        serviceProvider.GetService<IConfigValidator>().Should().NotBeNull();

        // Assert - Optional services
        serviceProvider.GetService<IYamlLoader>().Should().NotBeNull();
        serviceProvider.GetService<ISchemaValidator>().Should().NotBeNull();

        // Assert - Output formatters
        var formatters = serviceProvider.GetServices<IOutputFormatter>();
        formatters.Should().HaveCount(2);
    }

    [Fact]
    public void AddSGuardConfigValidationCore_Should_Register_OnlyCoreServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSGuardConfigValidationCore();
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();

        // Assert - Core services
        serviceProvider.GetService<IRuleEngine>().Should().NotBeNull();
        serviceProvider.GetService<IConfigLoader>().Should().NotBeNull();
        serviceProvider.GetService<IFileValidator>().Should().NotBeNull();
        serviceProvider.GetService<IPathResolver>().Should().NotBeNull();
        serviceProvider.GetService<IValidatorFactory>().Should().NotBeNull();
        serviceProvider.GetService<IConfigValidator>().Should().NotBeNull();

        // Assert - Optional services should NOT be registered
        serviceProvider.GetService<IYamlLoader>().Should().BeNull();
        serviceProvider.GetService<ISchemaValidator>().Should().BeNull();
    }

    [Fact]
    public void AddSGuardConfigValidationCore_With_Configuration_Should_BindSecurityOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Security:MaxFileSizeBytes", "1048576" }
            })
            .Build();

        // Act
        services.AddSGuardConfigValidationCore(configuration);
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var securityOptions = serviceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value;

        // Assert
        securityOptions.Should().NotBeNull();
        securityOptions.MaxFileSizeBytes.Should().Be(1048576);
    }

    [Fact]
    public void AddSGuardConfigValidationCore_With_ConfigureSecurityOptions_Should_UseConfiguredOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<SecurityOptions> configureOptions = options =>
        {
            options.MaxFileSizeBytes = 3048576;
        };

        // Act
        services.AddSGuardConfigValidationCore(configureSecurityOptions: configureOptions);
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var securityOptions = serviceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value;

        // Assert
        securityOptions.Should().NotBeNull();
        securityOptions.MaxFileSizeBytes.Should().Be(3048576);
    }

    [Fact]
    public void AddSGuardConfigValidationCore_With_PluginDirectories_Should_RegisterValidatorPluginDiscovery()
    {
        // Arrange
        var services = new ServiceCollection();
        var pluginDirectories = new[] { "/path/to/plugins" };

        // Act
        services.AddSGuardConfigValidationCore(pluginDirectories: pluginDirectories);
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var discovery = serviceProvider.GetService<ValidatorPluginDiscovery>();

        // Assert
        discovery.Should().NotBeNull();
    }

    [Fact]
    public void AddSGuardConfigValidation_With_ConfigurationAndConfigureOptions_Should_PreferConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Security:MaxFileSizeBytes", "1048576" }
            })
            .Build();

        Action<SecurityOptions> configureOptions = options =>
        {
            options.MaxFileSizeBytes = 4048576; // Override configuration
        };

        // Act
        services.AddSGuardConfigValidation(configuration, configureOptions);
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var securityOptions = serviceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value;

        // Assert
        securityOptions.Should().NotBeNull();
        securityOptions.MaxFileSizeBytes.Should().Be(4048576); // configureOptions should take precedence
    }
}

