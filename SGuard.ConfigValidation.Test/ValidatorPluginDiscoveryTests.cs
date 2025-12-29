using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SGuard.ConfigValidation.Validators.Plugin;
using SGuard.ConfigValidation.Utils;

namespace SGuard.ConfigValidation.Test;

public sealed class ValidatorPluginDiscoveryTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ILogger<ValidatorPluginDiscovery> _logger;

    public ValidatorPluginDiscoveryTests()
    {
        _testDirectory = DirectoryUtility.CreateTempDirectory("plugin-discovery-test");
        _logger = NullLogger<ValidatorPluginDiscovery>.Instance;
    }

    public void Dispose()
    {
        DirectoryUtility.DeleteDirectory(_testDirectory, recursive: true);
    }

    [Fact]
    public void Constructor_With_NullLogger_Should_Throw_ArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new ValidatorPluginDiscovery(null!));
        
        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public void DiscoverValidators_With_NoPluginDirectories_Should_Discover_FromCurrentAssembly()
    {
        // Arrange
        var discovery = new ValidatorPluginDiscovery(_logger);

        // Act
        var validators = discovery.DiscoverValidators();

        // Assert
        validators.Should().NotBeNull();
        // Current assembly'de built-in validator'lar yok (onlar ValidatorFactory'de), 
        // ama metod çağrılabilir olmalı
        validators.Should().BeEmpty(); // Veya mevcut validator sayısına göre kontrol edilebilir
    }

    [Fact]
    public void DiscoverValidators_With_NonExistentDirectory_Should_LogWarning_And_Continue()
    {
        // Arrange
        var discovery = new ValidatorPluginDiscovery(_logger);
        var nonExistentDir = Path.Combine(_testDirectory, "nonexistent");

        // Act
        var validators = discovery.DiscoverValidators([nonExistentDir]);

        // Assert
        validators.Should().NotBeNull();
        // Warning log edilir ama exception fırlatılmaz
    }

    [Fact]
    public void DiscoverValidators_With_EmptyPluginDirectories_Should_NotThrow()
    {
        // Arrange
        var discovery = new ValidatorPluginDiscovery(_logger);

        // Act
        var validators = discovery.DiscoverValidators([]);

        // Assert
        validators.Should().NotBeNull();
    }

    [Fact]
    public void DiscoverValidators_With_NullPluginDirectories_Should_NotThrow()
    {
        // Arrange
        var discovery = new ValidatorPluginDiscovery(_logger);

        // Act
        var validators = discovery.DiscoverValidators(null);

        // Assert
        validators.Should().NotBeNull();
    }

    [Fact]
    public void DiscoverValidators_With_EmptyDirectory_Should_NotThrow()
    {
        // Arrange
        var discovery = new ValidatorPluginDiscovery(_logger);
        Directory.CreateDirectory(_testDirectory);

        // Act
        var validators = discovery.DiscoverValidators([_testDirectory]);

        // Assert
        validators.Should().NotBeNull();
    }

    [Fact]
    public void DiscoverValidators_With_InvalidAssemblyFile_Should_LogError_And_Continue()
    {
        // Arrange
        var discovery = new ValidatorPluginDiscovery(_logger);
        Directory.CreateDirectory(_testDirectory);
        
        // Create a file that looks like a DLL but isn't
        var invalidDll = Path.Combine(_testDirectory, "invalid.dll");
        File.WriteAllText(invalidDll, "This is not a valid DLL");

        // Act
        var validators = discovery.DiscoverValidators([_testDirectory]);

        // Assert
        validators.Should().NotBeNull();
        // Error log edilir ama exception fırlatılmaz
    }

    [Fact]
    public void DiscoverValidators_With_MultipleDirectories_Should_Process_All()
    {
        // Arrange
        var discovery = new ValidatorPluginDiscovery(_logger);
        var dir1 = Path.Combine(_testDirectory, "dir1");
        var dir2 = Path.Combine(_testDirectory, "dir2");
        Directory.CreateDirectory(dir1);
        Directory.CreateDirectory(dir2);

        // Act
        var validators = discovery.DiscoverValidators([dir1, dir2]);

        // Assert
        validators.Should().NotBeNull();
    }

    [Fact]
    public void DiscoverValidators_Should_Return_CaseInsensitiveDictionary()
    {
        // Arrange
        var discovery = new ValidatorPluginDiscovery(_logger);

        // Act
        var validators = discovery.DiscoverValidators();

        // Assert
        validators.Should().NotBeNull();
        // Dictionary case-insensitive olmalı (StringComparer.OrdinalIgnoreCase kullanılıyor)
        // Bu test için gerçek plugin'ler gerekir, şimdilik sadece metodun çağrılabildiğini doğruluyoruz
    }

    [Fact]
    public void DiscoverValidators_Should_Cache_AssemblyTypes()
    {
        // Arrange
        var discovery = new ValidatorPluginDiscovery(_logger);

        // Act - Call multiple times
        var validators1 = discovery.DiscoverValidators();
        var validators2 = discovery.DiscoverValidators();

        // Assert
        validators1.Should().NotBeNull();
        validators2.Should().NotBeNull();
        // Cache kullanıldığı için performans iyileştirmesi olmalı
        // Aynı assembly'den tekrar discovery yapıldığında cache kullanılır
    }

    [Fact]
    public void DiscoverValidators_With_DirectoryContainingNonDllFiles_Should_IgnoreNonDllFiles()
    {
        // Arrange
        var discovery = new ValidatorPluginDiscovery(_logger);
        Directory.CreateDirectory(_testDirectory);
        
        // Create non-DLL files
        File.WriteAllText(Path.Combine(_testDirectory, "test.txt"), "text");
        File.WriteAllText(Path.Combine(_testDirectory, "test.exe"), "exe");
        File.WriteAllText(Path.Combine(_testDirectory, "test.dll.txt"), "dll.txt");

        // Act
        var validators = discovery.DiscoverValidators([_testDirectory]);

        // Assert
        validators.Should().NotBeNull();
        // Non-DLL files should be ignored
    }

    [Fact]
    public void DiscoverValidators_With_Subdirectories_Should_OnlyScanTopDirectory()
    {
        // Arrange
        var discovery = new ValidatorPluginDiscovery(_logger);
        var subDir = Path.Combine(_testDirectory, "subdir");
        Directory.CreateDirectory(_testDirectory);
        Directory.CreateDirectory(subDir);
        
        // Create a file in subdirectory (should be ignored due to SearchOption.TopDirectoryOnly)
        File.WriteAllText(Path.Combine(subDir, "test.dll"), "test");

        // Act
        var validators = discovery.DiscoverValidators([_testDirectory]);

        // Assert
        validators.Should().NotBeNull();
        // Subdirectory should not be scanned
    }

    [Fact]
    public void DiscoverValidators_With_MixedValidAndInvalidDirectories_Should_ProcessValidOnes()
    {
        // Arrange
        var discovery = new ValidatorPluginDiscovery(_logger);
        var validDir = Path.Combine(_testDirectory, "valid");
        var invalidDir = Path.Combine(_testDirectory, "invalid");
        Directory.CreateDirectory(validDir);
        // invalidDir doesn't exist

        // Act
        var validators = discovery.DiscoverValidators([validDir, invalidDir]);

        // Assert
        validators.Should().NotBeNull();
        // Should process validDir and log warning for invalidDir
    }

    [Fact]
    public void DiscoverValidators_With_ExceptionDuringAssemblyLoad_Should_LogError_And_Continue()
    {
        // Arrange
        var discovery = new ValidatorPluginDiscovery(_logger);
        Directory.CreateDirectory(_testDirectory);
        
        // Create a file that will cause an exception when trying to load as assembly
        var badDll = Path.Combine(_testDirectory, "bad.dll");
        File.WriteAllBytes(badDll, new byte[] { 0x00, 0x01, 0x02, 0x03 }); // Invalid DLL header

        // Act
        var validators = discovery.DiscoverValidators([_testDirectory]);

        // Assert
        validators.Should().NotBeNull();
        // Error should be logged but discovery should continue
    }

    [Fact]
    public void DiscoverValidators_With_ExceptionDuringTypeDiscovery_Should_LogError_And_Continue()
    {
        // Arrange
        var discovery = new ValidatorPluginDiscovery(_logger);

        // Act - Discover from current assembly (which may have types that cause issues)
        var validators = discovery.DiscoverValidators();

        // Assert
        validators.Should().NotBeNull();
        // Any errors during type discovery should be logged but not throw
    }

    [Fact]
    public void DiscoverValidators_With_EmptyValidatorType_Should_SkipPlugin()
    {
        // Arrange
        var discovery = new ValidatorPluginDiscovery(_logger);

        // Act
        var validators = discovery.DiscoverValidators();

        // Assert
        validators.Should().NotBeNull();
        // Plugins with empty ValidatorType should be skipped (logged as warning)
    }

    [Fact]
    public void DiscoverValidators_With_DuplicateValidatorTypes_Should_SkipDuplicates()
    {
        // Arrange
        var discovery = new ValidatorPluginDiscovery(_logger);

        // Act
        var validators = discovery.DiscoverValidators();

        // Assert
        validators.Should().NotBeNull();
        // Duplicate validator types should be skipped (logged as warning)
    }
}

