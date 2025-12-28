using FluentAssertions;
using SGuard.ConfigValidation.Models;

namespace SGuard.ConfigValidation.Tests;

public sealed class ModelTests
{
    [Fact]
    public void HookConfig_Constructor_Should_Set_Properties()
    {
        // Arrange
        var type = "script";
        var command = "echo test";
        var arguments = new List<string> { "arg1", "arg2" };
        var timeout = 30000;

        // Act
        var hookConfig = new HookConfig
        {
            Type = type,
            Command = command,
            Arguments = arguments,
            Timeout = timeout
        };

        // Assert
        hookConfig.Should().NotBeNull();
        hookConfig.Type.Should().Be(type);
        hookConfig.Command.Should().Be(command);
        hookConfig.Arguments.Should().BeEquivalentTo(arguments);
        hookConfig.Timeout.Should().Be(timeout);
    }

    [Fact]
    public void HookConfig_With_NullArguments_Should_BeValid()
    {
        // Arrange & Act
        var hookConfig = new HookConfig
        {
            Type = "script",
            Command = "echo test",
            Arguments = null,
            Timeout = 30000
        };

        // Assert
        hookConfig.Should().NotBeNull();
        hookConfig.Arguments.Should().BeNull();
    }

    [Fact]
    public void HookConfig_With_EmptyArguments_Should_BeValid()
    {
        // Arrange & Act
        var hookConfig = new HookConfig
        {
            Type = "script",
            Command = "echo test",
            Arguments = [],
            Timeout = 30000
        };

        // Assert
        hookConfig.Should().NotBeNull();
        hookConfig.Arguments.Should().BeEmpty();
    }

    [Fact]
    public void HookConfig_With_AllProperties_Should_Set_All()
    {
        // Arrange & Act
        var hookConfig = new HookConfig
        {
            Type = "webhook",
            Command = "script.sh",
            Arguments = new List<string> { "arg1" },
            WorkingDirectory = "/tmp",
            EnvironmentVariables = new Dictionary<string, string> { { "KEY", "VALUE" } },
            Timeout = 5000,
            Url = "https://example.com/webhook",
            Method = "POST",
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
            Body = new { status = "success" }
        };

        // Assert
        hookConfig.Should().NotBeNull();
        hookConfig.Type.Should().Be("webhook");
        hookConfig.Command.Should().Be("script.sh");
        hookConfig.Arguments.Should().HaveCount(1);
        hookConfig.WorkingDirectory.Should().Be("/tmp");
        hookConfig.EnvironmentVariables.Should().HaveCount(1);
        hookConfig.Timeout.Should().Be(5000);
        hookConfig.Url.Should().Be("https://example.com/webhook");
        hookConfig.Method.Should().Be("POST");
        hookConfig.Headers.Should().HaveCount(1);
        hookConfig.Body.Should().NotBeNull();
    }

    [Fact]
    public void HooksConfig_Constructor_Should_Set_Properties()
    {
        // Arrange & Act
        var hooksConfig = new HooksConfig
        {
            Global = new GlobalHooksConfig
            {
                OnSuccess = [new HookConfig { Type = "script", Command = "success.sh" }],
                OnFailure = [new HookConfig { Type = "script", Command = "failure.sh" }]
            },
            Environments = new Dictionary<string, EnvironmentHooksConfig>
            {
                {
                    "dev",
                    new EnvironmentHooksConfig
                    {
                        OnSuccess = [new HookConfig { Type = "script", Command = "dev-success.sh" }]
                    }
                }
            }
        };

        // Assert
        hooksConfig.Should().NotBeNull();
        hooksConfig.Global.Should().NotBeNull();
        hooksConfig.Environments.Should().NotBeNull();
        hooksConfig.Environments.Should().HaveCount(1);
        hooksConfig.Environments.Should().ContainKey("dev");
    }

    [Fact]
    public void HooksConfig_With_NullGlobal_Should_BeValid()
    {
        // Arrange & Act
        var hooksConfig = new HooksConfig
        {
            Global = null,
            Environments = new Dictionary<string, EnvironmentHooksConfig>()
        };

        // Assert
        hooksConfig.Should().NotBeNull();
        hooksConfig.Global.Should().BeNull();
    }

    [Fact]
    public void HooksConfig_With_NullEnvironments_Should_BeValid()
    {
        // Arrange & Act
        var hooksConfig = new HooksConfig
        {
            Global = new GlobalHooksConfig(),
            Environments = null
        };

        // Assert
        hooksConfig.Should().NotBeNull();
        hooksConfig.Environments.Should().BeNull();
    }

    [Fact]
    public void GlobalHooksConfig_Constructor_Should_Set_Properties()
    {
        // Arrange & Act
        var globalHooksConfig = new GlobalHooksConfig
        {
            OnSuccess = [new HookConfig { Type = "script", Command = "success.sh" }],
            OnFailure = [new HookConfig { Type = "script", Command = "failure.sh" }],
            OnValidationError = [new HookConfig { Type = "script", Command = "validation-error.sh" }],
            OnSystemError = [new HookConfig { Type = "script", Command = "system-error.sh" }]
        };

        // Assert
        globalHooksConfig.Should().NotBeNull();
        globalHooksConfig.OnSuccess.Should().NotBeNull();
        globalHooksConfig.OnSuccess.Should().HaveCount(1);
        globalHooksConfig.OnFailure.Should().NotBeNull();
        globalHooksConfig.OnFailure.Should().HaveCount(1);
        globalHooksConfig.OnValidationError.Should().NotBeNull();
        globalHooksConfig.OnValidationError.Should().HaveCount(1);
        globalHooksConfig.OnSystemError.Should().NotBeNull();
        globalHooksConfig.OnSystemError.Should().HaveCount(1);
    }

    [Fact]
    public void GlobalHooksConfig_With_NullOnSuccess_Should_BeValid()
    {
        // Arrange & Act
        var globalHooksConfig = new GlobalHooksConfig
        {
            OnSuccess = null,
            OnFailure = [new HookConfig { Type = "script", Command = "failure.sh" }]
        };

        // Assert
        globalHooksConfig.Should().NotBeNull();
        globalHooksConfig.OnSuccess.Should().BeNull();
    }

    [Fact]
    public void GlobalHooksConfig_With_NullOnFailure_Should_BeValid()
    {
        // Arrange & Act
        var globalHooksConfig = new GlobalHooksConfig
        {
            OnSuccess = [new HookConfig { Type = "script", Command = "success.sh" }],
            OnFailure = null
        };

        // Assert
        globalHooksConfig.Should().NotBeNull();
        globalHooksConfig.OnFailure.Should().BeNull();
    }

    [Fact]
    public void EnvironmentHooksConfig_Constructor_Should_Set_Properties()
    {
        // Arrange & Act
        var environmentHooksConfig = new EnvironmentHooksConfig
        {
            OnSuccess = [new HookConfig { Type = "script", Command = "success.sh" }],
            OnFailure = [new HookConfig { Type = "script", Command = "failure.sh" }]
        };

        // Assert
        environmentHooksConfig.Should().NotBeNull();
        environmentHooksConfig.OnSuccess.Should().NotBeNull();
        environmentHooksConfig.OnSuccess.Should().HaveCount(1);
        environmentHooksConfig.OnFailure.Should().NotBeNull();
        environmentHooksConfig.OnFailure.Should().HaveCount(1);
    }

    [Fact]
    public void EnvironmentHooksConfig_With_NullOnSuccess_Should_BeValid()
    {
        // Arrange & Act
        var environmentHooksConfig = new EnvironmentHooksConfig
        {
            OnSuccess = null,
            OnFailure = [new HookConfig { Type = "script", Command = "failure.sh" }]
        };

        // Assert
        environmentHooksConfig.Should().NotBeNull();
        environmentHooksConfig.OnSuccess.Should().BeNull();
    }

    [Fact]
    public void EnvironmentHooksConfig_With_NullOnFailure_Should_BeValid()
    {
        // Arrange & Act
        var environmentHooksConfig = new EnvironmentHooksConfig
        {
            OnSuccess = [new HookConfig { Type = "script", Command = "success.sh" }],
            OnFailure = null
        };

        // Assert
        environmentHooksConfig.Should().NotBeNull();
        environmentHooksConfig.OnFailure.Should().BeNull();
    }
}

