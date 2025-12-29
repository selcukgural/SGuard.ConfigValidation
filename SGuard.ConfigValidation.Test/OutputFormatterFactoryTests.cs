using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SGuard.ConfigValidation.Output;
using SGuard.ConfigValidation.Utils;

namespace SGuard.ConfigValidation.Test;

public sealed class OutputFormatterFactoryTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ILoggerFactory _loggerFactory;

    public OutputFormatterFactoryTests()
    {
        _testDirectory = DirectoryUtility.CreateTempDirectory("outputformatterfactory-test");
        _loggerFactory = NullLoggerFactory.Instance;
    }

    public void Dispose()
    {
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
    }

    [Fact]
    public void Create_With_JsonFormat_Should_Return_JsonOutputFormatter()
    {
        // Act
        var formatter = OutputFormatterFactory.Create("json", _loggerFactory);

        // Assert
        formatter.Should().NotBeNull();
        formatter.Should().BeOfType<JsonOutputFormatter>();
    }

    [Fact]
    public void Create_With_JsonFormat_Uppercase_Should_Return_JsonOutputFormatter()
    {
        // Act
        var formatter = OutputFormatterFactory.Create("JSON", _loggerFactory);

        // Assert
        formatter.Should().NotBeNull();
        formatter.Should().BeOfType<JsonOutputFormatter>();
    }

    [Fact]
    public void Create_With_TextFormat_Should_Return_ConsoleOutputFormatter()
    {
        // Act
        var formatter = OutputFormatterFactory.Create("text", _loggerFactory);

        // Assert
        formatter.Should().NotBeNull();
        formatter.Should().BeOfType<ConsoleOutputFormatter>();
    }

    [Fact]
    public void Create_With_ConsoleFormat_Should_Return_ConsoleOutputFormatter()
    {
        // Act
        var formatter = OutputFormatterFactory.Create("console", _loggerFactory);

        // Assert
        formatter.Should().NotBeNull();
        formatter.Should().BeOfType<ConsoleOutputFormatter>();
    }

    [Fact]
    public void Create_With_InvalidFormat_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            OutputFormatterFactory.Create("invalid", _loggerFactory));
        
        exception.ParamName.Should().Be("format");
    }

    [Fact]
    public void Create_With_NullFormat_Should_Throw_ArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            OutputFormatterFactory.Create(null!, _loggerFactory));
        
        exception.ParamName.Should().Be("format");
    }

    [Fact]
    public void Create_With_EmptyFormat_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            OutputFormatterFactory.Create("", _loggerFactory));
        
        exception.ParamName.Should().Be("format");
    }

    [Fact]
    public void Create_With_WhitespaceFormat_Should_Throw_ArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            OutputFormatterFactory.Create("   ", _loggerFactory));
        
        exception.ParamName.Should().Be("format");
    }

    [Fact]
    public void Create_With_NullLoggerFactory_Should_Throw_ArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            OutputFormatterFactory.Create("json", null!));
        
        exception.ParamName.Should().Be("loggerFactory");
    }

    [Fact]
    public void Create_With_JsonFormat_And_FilePath_Should_Return_JsonFileOutputFormatter()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "output.json");

        // Act
        var formatter = OutputFormatterFactory.Create("json", _loggerFactory, filePath);

        // Assert
        formatter.Should().NotBeNull();
        formatter.Should().BeOfType<JsonFileOutputFormatter>();
    }

    [Fact]
    public void Create_With_TextFormat_And_FilePath_Should_Return_FileOutputFormatter()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "output.txt");

        // Act
        var formatter = OutputFormatterFactory.Create("text", _loggerFactory, filePath);

        // Assert
        formatter.Should().NotBeNull();
        formatter.Should().BeOfType<FileOutputFormatter>();
    }

    [Fact]
    public void Create_With_ConsoleFormat_And_FilePath_Should_Return_FileOutputFormatter()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "output.txt");

        // Act
        var formatter = OutputFormatterFactory.Create("console", _loggerFactory, filePath);

        // Assert
        formatter.Should().NotBeNull();
        formatter.Should().BeOfType<FileOutputFormatter>();
    }

    [Fact]
    public void Create_With_JsonFormat_And_NullFilePath_Should_Return_JsonOutputFormatter()
    {
        // Act
        var formatter = OutputFormatterFactory.Create("json", _loggerFactory, null);

        // Assert
        formatter.Should().NotBeNull();
        formatter.Should().BeOfType<JsonOutputFormatter>();
    }

    [Fact]
    public void Create_With_JsonFormat_And_EmptyFilePath_Should_Return_JsonOutputFormatter()
    {
        // Act
        var formatter = OutputFormatterFactory.Create("json", _loggerFactory, "");

        // Assert
        formatter.Should().NotBeNull();
        formatter.Should().BeOfType<JsonOutputFormatter>();
    }

    [Fact]
    public void Create_With_JsonFormat_And_WhitespaceFilePath_Should_Return_JsonOutputFormatter()
    {
        // Act
        var formatter = OutputFormatterFactory.Create("json", _loggerFactory, "   ");

        // Assert
        formatter.Should().NotBeNull();
        formatter.Should().BeOfType<JsonOutputFormatter>();
    }

    [Fact]
    public void Create_With_InvalidFormat_And_FilePath_Should_Throw_ArgumentException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "output.txt");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            OutputFormatterFactory.Create("invalid", _loggerFactory, filePath));
        
        exception.ParamName.Should().Be("format");
    }

    [Fact]
    public void Create_With_NullFormat_And_FilePath_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "output.txt");

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            OutputFormatterFactory.Create(null!, _loggerFactory, filePath));
        
        exception.ParamName.Should().Be("format");
    }

    [Fact]
    public void Create_With_NullLoggerFactory_And_FilePath_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "output.json");

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            OutputFormatterFactory.Create("json", null!, filePath));
        
        exception.ParamName.Should().Be("loggerFactory");
    }

    [Fact]
    public void Create_With_MixedCaseFormat_Should_Handle_CaseInsensitive()
    {
        // Act
        var formatter1 = OutputFormatterFactory.Create("Json", _loggerFactory);
        var formatter2 = OutputFormatterFactory.Create("TEXT", _loggerFactory);
        var formatter3 = OutputFormatterFactory.Create("CoNsOlE", _loggerFactory);

        // Assert
        formatter1.Should().BeOfType<JsonOutputFormatter>();
        formatter2.Should().BeOfType<ConsoleOutputFormatter>();
        formatter3.Should().BeOfType<ConsoleOutputFormatter>();
    }
}

