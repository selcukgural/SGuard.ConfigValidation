using FluentAssertions;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Output;

namespace SGuard.ConfigValidation.Tests;

public sealed class OutputFormatterTests
{
    [Fact]
    public async Task ConsoleOutputFormatter_With_SuccessfulResult_Should_Output_SuccessMessage()
    {
        // Arrange
        var formatter = new ConsoleOutputFormatter();
        var result = RuleEngineResult.CreateSuccess(new FileValidationResult("test.json", []));
        var output = new StringWriter();
        Console.SetOut(output);

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
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    [Fact]
    public async Task ConsoleOutputFormatter_With_FailedResult_Should_Output_ErrorMessage()
    {
        // Arrange
        var formatter = new ConsoleOutputFormatter();
        var errorResult = ValidationResult.Failure("Test error", "required", "Test:Key", null);
        var fileResult = new FileValidationResult("test.json", [errorResult]);
        var result = RuleEngineResult.CreateSuccess(fileResult);
        var output = new StringWriter();
        Console.SetOut(output);

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
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    [Fact]
    public async Task ConsoleOutputFormatter_With_ErrorResult_Should_Output_Error()
    {
        // Arrange
        var formatter = new ConsoleOutputFormatter();
        var result = RuleEngineResult.CreateError("Test error message");
        var output = new StringWriter();
        Console.SetOut(output);

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
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    [Fact]
    public async Task ConsoleOutputFormatter_With_MultipleEnvironments_Should_Output_All()
    {
        // Arrange
        var formatter = new ConsoleOutputFormatter();
        var results = new List<FileValidationResult>
        {
            new("env1.json", []),
            new("env2.json", [])
        };
        var result = RuleEngineResult.CreateSuccess(results);
        var output = new StringWriter();
        Console.SetOut(output);

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
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    [Fact]
    public async Task JsonOutputFormatter_With_SuccessfulResult_Should_Output_ValidJson()
    {
        // Arrange
        var formatter = new JsonOutputFormatter();
        var result = RuleEngineResult.CreateSuccess(new FileValidationResult("test.json", []));
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            // Act
            await formatter.FormatAsync(result);

            // Assert
            var outputText = output.ToString().Trim();
            outputText.ToLowerInvariant().Should().Contain("\"success\"");
            outputText.Should().Contain("\"test.json\"");
            
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
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
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
        Console.SetOut(output);

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
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    [Fact]
    public async Task JsonOutputFormatter_With_ErrorResult_Should_Output_ErrorInJson()
    {
        // Arrange
        var formatter = new JsonOutputFormatter();
        var result = RuleEngineResult.CreateError("Test error message");
        var output = new StringWriter();
        Console.SetOut(output);

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
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
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
        Console.SetOut(output);

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
                jsonDoc.RootElement.GetProperty("results").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
            }
        }
        finally
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }
}

