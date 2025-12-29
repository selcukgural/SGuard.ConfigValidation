using FluentAssertions;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Output;
using SGuard.ConfigValidation.Results;
using System.Text;
using SGuard.ConfigValidation.Utils;

namespace SGuard.ConfigValidation.Test;

public sealed class OutputFormatterTests : IDisposable
{
    private readonly string _testDirectory;

    public OutputFormatterTests()
    {
        _testDirectory = DirectoryUtility.CreateTempDirectory("outputformatter-test");
    }

    public void Dispose()
    {
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
    }
    private sealed class TestLoggerProvider : ILoggerProvider
    {
        private readonly StringWriter _output;

        public TestLoggerProvider(StringWriter output)
        {
            _output = output;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(_output);
        }

        public void Dispose() { }
    }

    private sealed class TestLogger : ILogger
    {
        private readonly StringWriter _output;

        public TestLogger(StringWriter output)
        {
            _output = output;
        }

        IDisposable ILogger.BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            if (!string.IsNullOrEmpty(message))
            {
                _output.WriteLine(message);
            }
        }
    }

    private static (ILogger<ConsoleOutputFormatter> Logger, StringWriter Output) CreateTestLogger()
    {
        var output = new StringWriter();
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddProvider(new TestLoggerProvider(output)).SetMinimumLevel(LogLevel.Trace));
        return (loggerFactory.CreateLogger<ConsoleOutputFormatter>(), output);
    }

    [Fact]
    public async Task ConsoleOutputFormatter_With_SuccessfulResult_Should_Output_SuccessMessage()
    {
        // Arrange
        var (logger, output) = CreateTestLogger();
        var formatter = new ConsoleOutputFormatter(logger);
        var result = RuleEngineResult.CreateSuccess(new FileValidationResult("test.json", []));

        try
        {
            // Act
            await formatter.FormatAsync(result);

            // Assert
            var outputText = output.ToString();
            outputText.ToLowerInvariant().Should().Contain("pass");
            outputText.ToLowerInvariant().Should().Contain("successfully");
        }
        finally
        {
            System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    [Fact]
    public async Task ConsoleOutputFormatter_With_FailedResult_Should_Output_ErrorMessage()
    {
        // Arrange
        var (logger, output) = CreateTestLogger();
        var formatter = new ConsoleOutputFormatter(logger);
        var errorResult = ValidationResult.Failure("Test error", "required", "Test:Key", null);
        var fileResult = new FileValidationResult("test.json", [errorResult]);
        var result = RuleEngineResult.CreateSuccess(fileResult);

        try
        {
            // Act
            await formatter.FormatAsync(result);

            // Assert
            var outputText = output.ToString();
            outputText.ToLowerInvariant().Should().Contain("fail");
            outputText.Should().Contain("Test error");
            outputText.Should().Contain("Test:Key");
        }
        finally
        {
            System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    [Fact]
    public async Task ConsoleOutputFormatter_With_ErrorResult_Should_Output_Error()
    {
        // Arrange
        var (logger, output) = CreateTestLogger();
        var formatter = new ConsoleOutputFormatter(logger);
        var result = RuleEngineResult.CreateError("Test error message");

        try
        {
            // Act
            await formatter.FormatAsync(result);

            // Assert
            var outputText = output.ToString();
            outputText.ToLowerInvariant().Should().Contain("error");
            outputText.Should().Contain("Test error message");
        }
        finally
        {
            System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    [Fact]
    public async Task ConsoleOutputFormatter_With_MultipleEnvironments_Should_Output_All()
    {
        // Arrange
        var (logger, output) = CreateTestLogger();
        var formatter = new ConsoleOutputFormatter(logger);
        var results = new List<FileValidationResult>
        {
            new("env1.json", []),
            new("env2.json", [])
        };
        var result = RuleEngineResult.CreateSuccess(results);

        try
        {
            // Act
            await formatter.FormatAsync(result);

            // Assert
            var outputText = output.ToString();
            outputText.Should().Contain("env1");
            outputText.Should().Contain("env2");
        }
        finally
        {
            System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    [Fact]
    public async Task JsonOutputFormatter_With_SuccessfulResult_Should_Output_ValidJson()
    {
        // Arrange
        var formatter = new JsonOutputFormatter();
        var result = RuleEngineResult.CreateSuccess(new FileValidationResult("test.json", []));
        var output = new StringWriter();
        System.Console.SetOut(output);

        try
        {
            // Act
            await formatter.FormatAsync(result);

            // Assert
            var outputText = output.ToString().Trim();
            outputText.ToLowerInvariant().Should().Contain("\"success\"");
            outputText.Should().Contain("\"test.json\"");
            
            // Verify it's valid JSON and check structure
            var jsonStart = outputText.IndexOf('{');
            var jsonEnd = outputText.LastIndexOf('}') + 1;
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonPart = outputText.Substring(jsonStart, jsonEnd - jsonStart);
                var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonPart);
                
                // Verify root structure (camelCase due to JsonNamingPolicy.CamelCase)
                jsonDoc.RootElement.GetProperty("success").ValueKind.Should().Be(System.Text.Json.JsonValueKind.True);
                jsonDoc.RootElement.GetProperty("hasValidationErrors").ValueKind.Should().Be(System.Text.Json.JsonValueKind.False);
                jsonDoc.RootElement.GetProperty("results").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
                
                // Verify Results array structure
                var results = jsonDoc.RootElement.GetProperty("results");
                results.GetArrayLength().Should().Be(1);
                results[0].GetProperty("path").GetString().Should().Be("test.json");
                results[0].GetProperty("isValid").GetBoolean().Should().BeTrue();
            }
        }
        finally
        {
            System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    [Fact]
    public async Task JsonOutputFormatter_With_FailedResult_Should_Output_ErrorsInJson()
    {
        // Arrange
        var formatter = new JsonOutputFormatter();
        var errorResult = ValidationResult.Failure("Test error", "required", "Test:Key", "value");
        var fileResult = new FileValidationResult("test.json", [errorResult]);
        var result = RuleEngineResult.CreateSuccess(fileResult);
        var output = new StringWriter();
        System.Console.SetOut(output);

        try
        {
            // Act
            await formatter.FormatAsync(result);

            // Assert
            var outputText = output.ToString().Trim();
            outputText.ToLowerInvariant().Should().Contain("\"errors\"");
            outputText.Should().Contain("Test error");
            
            // Verify it's valid JSON (find JSON part, might have newlines)
            var jsonStart = outputText.IndexOf('{');
            var jsonEnd = outputText.LastIndexOf('}') + 1;
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonPart = outputText.Substring(jsonStart, jsonEnd - jsonStart);
                var act = () => System.Text.Json.JsonDocument.Parse(jsonPart);
                act.Should().NotThrow("Output should be valid JSON");
            }
        }
        finally
        {
            System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    [Fact]
    public async Task JsonOutputFormatter_With_ErrorResult_Should_Output_ErrorInJson()
    {
        // Arrange
        var formatter = new JsonOutputFormatter();
        var result = RuleEngineResult.CreateError("Test error message");
        var output = new StringWriter();
        System.Console.SetOut(output);

        try
        {
            // Act
            await formatter.FormatAsync(result);

            // Assert
            var outputText = output.ToString().Trim();
            outputText.ToLowerInvariant().Should().Contain("\"success\"");
            outputText.Should().Contain("Test error message");
            
            // Verify it's valid JSON (find JSON part, might have newlines)
            var jsonStart = outputText.IndexOf('{');
            var jsonEnd = outputText.LastIndexOf('}') + 1;
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonPart = outputText.Substring(jsonStart, jsonEnd - jsonStart);
                var act = () => System.Text.Json.JsonDocument.Parse(jsonPart);
                act.Should().NotThrow("Output should be valid JSON");
            }
        }
        finally
        {
            System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    [Fact]
    public async Task JsonOutputFormatter_With_SingleResult_Should_Output_AsArray()
    {
        // Arrange
        var formatter = new JsonOutputFormatter();
        var singleResult = new FileValidationResult("test.json", []);
        var result = RuleEngineResult.CreateSuccess(singleResult);
        var output = new StringWriter();
        System.Console.SetOut(output);

        try
        {
            // Act
            await formatter.FormatAsync(result);

            // Assert
            var outputText = output.ToString().Trim();
            var jsonStart = outputText.IndexOf('{');
            var jsonEnd = outputText.LastIndexOf('}') + 1;
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonPart = outputText.Substring(jsonStart, jsonEnd - jsonStart);
                var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonPart);
                // Note: JsonOutputFormatter uses camelCase (JsonNamingPolicy.CamelCase)
                jsonDoc.RootElement.GetProperty("results").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
            }
        }
        finally
        {
            System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    #region FileOutputFormatter Tests

    [Fact]
    public async Task FileOutputFormatter_With_SuccessfulResult_Should_Write_ToFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "success-result.txt");
        var formatter = new FileOutputFormatter(filePath);
        var result = RuleEngineResult.CreateSuccess(new FileValidationResult("test.json", []));

        // Act
        await formatter.FormatAsync(result);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        content.Should().NotBeNullOrEmpty();
        content.ToLowerInvariant().Should().Contain("pass");
    }

    [Fact]
    public async Task FileOutputFormatter_With_FailedResult_Should_Write_Errors_ToFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "failed-result.txt");
        var formatter = new FileOutputFormatter(filePath);
        var errorResult = ValidationResult.Failure("Test error", "required", "Test:Key", null);
        var fileResult = new FileValidationResult("test.json", [errorResult]);
        var result = RuleEngineResult.CreateSuccess(fileResult);

        // Act
        await formatter.FormatAsync(result);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        content.Should().Contain("Test error");
        content.Should().Contain("Test:Key");
        content.ToLowerInvariant().Should().Contain("fail");
    }

    [Fact]
    public async Task FileOutputFormatter_With_ErrorResult_Should_Write_Error_ToFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "error-result.txt");
        var formatter = new FileOutputFormatter(filePath);
        var result = RuleEngineResult.CreateError("Test error message");

        // Act
        await formatter.FormatAsync(result);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        content.Should().Contain("Test error message");
        content.ToLowerInvariant().Should().Contain("error");
    }

    [Fact]
    public async Task FileOutputFormatter_With_MultipleEnvironments_Should_Write_All_ToFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "multiple-env-result.txt");
        var formatter = new FileOutputFormatter(filePath);
        var results = new List<FileValidationResult>
        {
            new("env1.json", []),
            new("env2.json", [])
        };
        var result = RuleEngineResult.CreateSuccess(results);

        // Act
        await formatter.FormatAsync(result);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        content.Should().Contain("env1");
        content.Should().Contain("env2");
    }

    [Fact]
    public async Task FileOutputFormatter_Should_Create_Directory_IfNotExists()
    {
        // Arrange
        var subDirectory = Path.Combine(_testDirectory, "subdir", "nested");
        var filePath = Path.Combine(subDirectory, "result.txt");
        var formatter = new FileOutputFormatter(filePath);
        var result = RuleEngineResult.CreateSuccess(new FileValidationResult("test.json", []));

        // Act
        await formatter.FormatAsync(result);

        // Assert
        Directory.Exists(subDirectory).Should().BeTrue();
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task FileOutputFormatter_Should_Write_UTF8_Encoded_Content()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "utf8-result.txt");
        var formatter = new FileOutputFormatter(filePath);
        var result = RuleEngineResult.CreateSuccess(new FileValidationResult("test.json", []));

        // Act
        await formatter.FormatAsync(result);

        // Assert
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        var bytes = File.ReadAllBytes(filePath);
        
        // Check for UTF-8 BOM (optional) or verify UTF-8 encoding
        // UTF-8 files should not have BOM by default in .NET
        content.Should().NotBeNullOrEmpty();
        
        // Verify we can read it as UTF-8 (compare without BOM)
        var utf8Content = Encoding.UTF8.GetString(bytes);
        // Remove BOM if present for comparison
        var contentWithoutBom = content.TrimStart('\uFEFF');
        var utf8ContentWithoutBom = utf8Content.TrimStart('\uFEFF');
        utf8ContentWithoutBom.Should().Be(contentWithoutBom);
    }

    [Fact]
    public async Task FileOutputFormatter_Should_Write_Correct_Text_Format()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "format-test.txt");
        var formatter = new FileOutputFormatter(filePath);
        var errorResult = ValidationResult.Failure("Test error", "required", "Test:Key", "test-value");
        var successResult = ValidationResult.Success();
        var fileResult = new FileValidationResult("appsettings.Development.json", [errorResult, successResult]);
        var result = RuleEngineResult.CreateSuccess(fileResult);

        // Act
        await formatter.FormatAsync(result);

        // Assert
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        
        // Check for expected text format elements
        content.Should().Contain("appsettings.Development.json"); // File path
        content.Should().Contain("Development"); // Environment name (from filename)
        content.Should().Contain("Test error"); // Error message
        content.Should().Contain("Test:Key"); // Error key
        content.Should().Contain("test-value"); // Error value
        content.ToLowerInvariant().Should().Contain("fail"); // Status
    }

    #endregion

    #region JsonFileOutputFormatter Tests

    [Fact]
    public async Task JsonFileOutputFormatter_With_SuccessfulResult_Should_Write_ValidJson_ToFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "success-result.json");
        var formatter = new JsonFileOutputFormatter(filePath);
        var result = RuleEngineResult.CreateSuccess(new FileValidationResult("test.json", []));

        // Act
        await formatter.FormatAsync(result);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        content.Should().NotBeNullOrEmpty();
        
        // Verify it's valid JSON
        var act = () => System.Text.Json.JsonDocument.Parse(content);
        act.Should().NotThrow("Output should be valid JSON");
        
        var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        jsonDoc.RootElement.GetProperty("hasValidationErrors").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task JsonFileOutputFormatter_With_FailedResult_Should_Write_ErrorsInJson_ToFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "failed-result.json");
        var formatter = new JsonFileOutputFormatter(filePath);
        var errorResult = ValidationResult.Failure("Test error", "required", "Test:Key", "value");
        var fileResult = new FileValidationResult("test.json", [errorResult]);
        var result = RuleEngineResult.CreateSuccess(fileResult);

        // Act
        await formatter.FormatAsync(result);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        
        var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
        jsonDoc.RootElement.GetProperty("hasValidationErrors").GetBoolean().Should().BeTrue();
        
        var results = jsonDoc.RootElement.GetProperty("results");
        results[0].GetProperty("errors").GetArrayLength().Should().Be(1);
        results[0].GetProperty("errors")[0].GetProperty("message").GetString().Should().Be("Test error");
    }

    [Fact]
    public async Task JsonFileOutputFormatter_With_ErrorResult_Should_Write_ErrorInJson_ToFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "error-result.json");
        var formatter = new JsonFileOutputFormatter(filePath);
        var result = RuleEngineResult.CreateError("Test error message");

        // Act
        await formatter.FormatAsync(result);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        
        var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
        // JSON uses camelCase (JsonNamingPolicy.CamelCase)
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        
        // ErrorMessage should exist and contain the error message (camelCase: errorMessage)
        jsonDoc.RootElement.TryGetProperty("errorMessage", out var errorMessageProp).Should().BeTrue();
        errorMessageProp.GetString().Should().Be("Test error message");
        
        // Results should be an empty array for error results (camelCase: results)
        jsonDoc.RootElement.GetProperty("results").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task JsonFileOutputFormatter_Should_Create_Directory_IfNotExists()
    {
        // Arrange
        var subDirectory = Path.Combine(_testDirectory, "subdir", "nested");
        var filePath = Path.Combine(subDirectory, "result.json");
        var formatter = new JsonFileOutputFormatter(filePath);
        var result = RuleEngineResult.CreateSuccess(new FileValidationResult("test.json", []));

        // Act
        await formatter.FormatAsync(result);

        // Assert
        Directory.Exists(subDirectory).Should().BeTrue();
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task JsonFileOutputFormatter_Should_Write_UTF8_Encoded_Content()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "utf8-result.json");
        var formatter = new JsonFileOutputFormatter(filePath);
        var result = RuleEngineResult.CreateSuccess(new FileValidationResult("test.json", []));

        // Act
        await formatter.FormatAsync(result);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        var bytes = File.ReadAllBytes(filePath);
        
        // Verify UTF-8 encoding - decode bytes as UTF-8
        var utf8Content = Encoding.UTF8.GetString(bytes);
        utf8Content.Should().NotBeNullOrEmpty();
        content.Should().NotBeNullOrEmpty();
        
        // Verify JSON is valid (this confirms UTF-8 encoding works)
        var act = () => System.Text.Json.JsonDocument.Parse(content);
        act.Should().NotThrow();
        
        // Verify UTF-8 encoding by parsing the decoded bytes directly
        // Skip UTF-8 BOM if present (0xEF 0xBB 0xBF)
        var utf8ContentWithoutBom = utf8Content;
        if (utf8Content.Length > 0 && utf8Content[0] == '\uFEFF')
        {
            utf8ContentWithoutBom = utf8Content.Substring(1);
        }
        var utf8ParseAct = () => System.Text.Json.JsonDocument.Parse(utf8ContentWithoutBom);
        utf8ParseAct.Should().NotThrow("Content should be valid UTF-8 encoded JSON");
        
        // Verify it contains expected JSON structure
        content.Should().Contain("success");
        content.Should().Contain("results");
    }

    [Fact]
    public async Task JsonFileOutputFormatter_Should_Write_Correct_Json_Structure()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "structure-test.json");
        var formatter = new JsonFileOutputFormatter(filePath);
        var errorResult = ValidationResult.Failure("Test error", "required", "Test:Key", "test-value");
        var successResult = ValidationResult.Success();
        var fileResult = new FileValidationResult("appsettings.Development.json", [errorResult, successResult]);
        var result = RuleEngineResult.CreateSuccess(fileResult);

        // Act
        await formatter.FormatAsync(result);

        // Assert
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
        
        // Verify root structure (camelCase)
        jsonDoc.RootElement.GetProperty("success").ValueKind.Should().Be(System.Text.Json.JsonValueKind.True);
        // ErrorMessage might be null or empty string (empty string is serialized as string, not null)
        if (jsonDoc.RootElement.TryGetProperty("errorMessage", out var errorMessageProp))
        {
            // Can be null or empty string
            errorMessageProp.ValueKind.Should().BeOneOf(System.Text.Json.JsonValueKind.Null, System.Text.Json.JsonValueKind.String);
        }
        jsonDoc.RootElement.GetProperty("hasValidationErrors").ValueKind.Should().Be(System.Text.Json.JsonValueKind.True);
        jsonDoc.RootElement.GetProperty("results").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
        
        // Verify Results array structure
        var results = jsonDoc.RootElement.GetProperty("results");
        results.GetArrayLength().Should().Be(1);
        
        var firstResult = results[0];
        firstResult.GetProperty("path").GetString().Should().Be("appsettings.Development.json");
        firstResult.GetProperty("isValid").GetBoolean().Should().BeFalse();
        firstResult.GetProperty("errorCount").GetInt32().Should().Be(1);
        firstResult.GetProperty("results").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
        firstResult.GetProperty("errors").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
        
        // Verify Results array items structure
        var resultItems = firstResult.GetProperty("results");
        resultItems.GetArrayLength().Should().Be(2);
        resultItems[0].GetProperty("isValid").ValueKind.Should().Be(System.Text.Json.JsonValueKind.False);
        resultItems[0].GetProperty("message").GetString().Should().Be("Test error");
        resultItems[0].GetProperty("validatorType").GetString().Should().Be("required");
        resultItems[0].GetProperty("key").GetString().Should().Be("Test:Key");
        resultItems[0].GetProperty("value").GetString().Should().Be("test-value");
        
        // Verify Errors array items structure
        var errors = firstResult.GetProperty("errors");
        errors.GetArrayLength().Should().Be(1);
        errors[0].GetProperty("message").GetString().Should().Be("Test error");
        errors[0].GetProperty("validatorType").GetString().Should().Be("required");
        errors[0].GetProperty("key").GetString().Should().Be("Test:Key");
        errors[0].GetProperty("value").GetString().Should().Be("test-value");
    }

    [Fact]
    public async Task JsonFileOutputFormatter_With_SingleResult_Should_Write_AsArray()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "single-result.json");
        var formatter = new JsonFileOutputFormatter(filePath);
        var singleResult = new FileValidationResult("test.json", []);
        var result = RuleEngineResult.CreateSuccess(singleResult);

        // Act
        await formatter.FormatAsync(result);

        // Assert
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
        jsonDoc.RootElement.GetProperty("results").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
        jsonDoc.RootElement.GetProperty("results").GetArrayLength().Should().Be(1);
    }

    #endregion

    #region OutputFormatterFactory Tests

    [Fact]
    public void OutputFormatterFactory_Create_WithJsonFormat_Should_Return_JsonOutputFormatter()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Act
        var formatter = OutputFormatterFactory.Create("json", loggerFactory);

        // Assert
        formatter.Should().BeOfType<JsonOutputFormatter>();
    }

    [Fact]
    public void OutputFormatterFactory_Create_WithTextFormat_Should_Return_ConsoleOutputFormatter()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Act
        var formatter = OutputFormatterFactory.Create("text", loggerFactory);

        // Assert
        formatter.Should().BeOfType<ConsoleOutputFormatter>();
    }

    [Fact]
    public void OutputFormatterFactory_Create_WithConsoleFormat_Should_Return_ConsoleOutputFormatter()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Act
        var formatter = OutputFormatterFactory.Create("console", loggerFactory);

        // Assert
        formatter.Should().BeOfType<ConsoleOutputFormatter>();
    }

    [Fact]
    public void OutputFormatterFactory_Create_WithJsonFormat_AndFilePath_Should_Return_JsonFileOutputFormatter()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var filePath = Path.Combine(_testDirectory, "result.json");

        // Act
        var formatter = OutputFormatterFactory.Create("json", loggerFactory, filePath);

        // Assert
        formatter.Should().BeOfType<JsonFileOutputFormatter>();
    }

    [Fact]
    public void OutputFormatterFactory_Create_WithTextFormat_AndFilePath_Should_Return_FileOutputFormatter()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var filePath = Path.Combine(_testDirectory, "result.txt");

        // Act
        var formatter = OutputFormatterFactory.Create("text", loggerFactory, filePath);

        // Assert
        formatter.Should().BeOfType<FileOutputFormatter>();
    }

    [Fact]
    public void OutputFormatterFactory_Create_WithConsoleFormat_AndFilePath_Should_Return_FileOutputFormatter()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var filePath = Path.Combine(_testDirectory, "result.txt");

        // Act
        var formatter = OutputFormatterFactory.Create("console", loggerFactory, filePath);

        // Assert
        formatter.Should().BeOfType<FileOutputFormatter>();
    }

    [Fact]
    public void OutputFormatterFactory_Create_WithNullFormat_Should_Throw_ArgumentException()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Act
        var act = () => OutputFormatterFactory.Create(null!, loggerFactory);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OutputFormatterFactory_Create_WithUnknownFormat_Should_Throw_ArgumentException()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Act
        var act = () => OutputFormatterFactory.Create("unknown", loggerFactory);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OutputFormatterFactory_Create_WithJsonFormat_AndNullFilePath_Should_Return_JsonOutputFormatter()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Act
        var formatter = OutputFormatterFactory.Create("json", loggerFactory, null);

        // Assert
        formatter.Should().BeOfType<JsonOutputFormatter>();
    }

    [Fact]
    public void OutputFormatterFactory_Create_WithTextFormat_AndEmptyFilePath_Should_Return_ConsoleOutputFormatter()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Act
        var formatter = OutputFormatterFactory.Create("text", loggerFactory, string.Empty);

        // Assert
        formatter.Should().BeOfType<ConsoleOutputFormatter>();
    }

    #endregion
}

