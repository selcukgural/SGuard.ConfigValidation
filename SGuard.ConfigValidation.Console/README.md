# SGuard ConfigValidation Console

This console application was created to test the SGuard.ConfigValidation library and demonstrate example usage scenarios. The application provides a command-line interface (CLI) for validating configuration files.

## Overview

SGuard.ConfigValidation.Console is a console application used to validate SGuard configuration files. The application performs configuration validation operations using all features of the `SGuard.ConfigValidation` library.

### Features

- Configuration file validation
- Environment-based validation (Development, Staging, Production)
- Output generation in JSON and text formats
- Detailed logging support
- Configuration management with environment variables

## Usage

### Basic Usage

```bash
# Validate all environments
dotnet run -- validate

# Validate a specific environment
dotnet run -- validate --env Development

# Validate all environments (explicitly)
dotnet run -- validate --all

# Get output in JSON format
dotnet run -- validate --output json

# Write output to file
dotnet run -- validate --output json --output-file results.json

# Run in verbose mode
dotnet run -- validate --verbose
```

### Command-Line Options

- `--config, -c`: Path to configuration file (default: `sguard.json`)
- `--env, -e`: Environment ID to validate (if not specified, all environments are validated)
- `--all, -a`: Validate all environments
- `--output, -o`: Output format: `json`, `text`, or `console` (default: `console`)
- `--output-file, -f`: Output file path (if specified, results will be written to this file)
- `--verbose, -v`: Enable verbose logging mode

## Environment Management

### Environment Detection

SGuard.ConfigValidation.Console automatically detects the environment according to .NET standards.

### Environment Variable Priority

1. **DOTNET_ENVIRONMENT** (Priority - .NET 6+ standard)
2. **ASPNETCORE_ENVIRONMENT** (For backward compatibility)
3. **Production** (Default, if none is set)

### Setting Environment Variables

#### Linux/macOS
```bash
export DOTNET_ENVIRONMENT=Development
# or
export ASPNETCORE_ENVIRONMENT=Development
```

#### Windows (Command Prompt)
```cmd
set DOTNET_ENVIRONMENT=Development
# or
set ASPNETCORE_ENVIRONMENT=Development
```

#### Windows (PowerShell)
```powershell
$env:DOTNET_ENVIRONMENT="Development"
# or
$env:ASPNETCORE_ENVIRONMENT="Development"
```

### appsettings Files

Files are automatically loaded based on the environment:

- `appsettings.json` - Always loaded (base configuration)
- `appsettings.Development.json` - Loaded in Development environment
- `appsettings.Staging.json` - Loaded in Staging environment
- `appsettings.Production.json` - Loaded in Production environment

**Note:** Environment-specific files are optional. If a file does not exist, only the base `appsettings.json` is used.

### IHostEnvironment Usage

Environment control can be performed by injecting `IHostEnvironment` in services:

```csharp
public class MyService
{
    private readonly IHostEnvironment _environment;
    
    public MyService(IHostEnvironment environment)
    {
        _environment = environment;
    }
    
    public void DoSomething()
    {
        if (_environment.IsDevelopment())
        {
            // Development-specific logic
        }
        
        if (_environment.IsProduction())
        {
            // Production-specific logic
        }
        
        // Custom environment check
        if (_environment.IsEnvironment("Staging"))
        {
            // Staging-specific logic
        }
    }
}
```

### Environment Helper Methods

The `IHostEnvironment` interface provides the following helper methods:

- `IsDevelopment()` - Development environment check
- `IsStaging()` - Staging environment check
- `IsProduction()` - Production environment check
- `IsEnvironment(string name)` - Custom environment check

### Example Scenarios

#### Development Environment
```bash
export DOTNET_ENVIRONMENT=Development
./ConsoleApp1
```
- `appsettings.json` is loaded
- `appsettings.Development.json` is loaded (if exists)
- Debug level logging is active

#### Staging Environment
```bash
export DOTNET_ENVIRONMENT=Staging
./ConsoleApp1
```
- `appsettings.json` is loaded
- `appsettings.Staging.json` is loaded (if exists)
- Information level logging is active

#### Production Environment
```bash
export DOTNET_ENVIRONMENT=Production
# or set nothing (defaults to Production)
./ConsoleApp1
```
- `appsettings.json` is loaded
- `appsettings.Production.json` is loaded (if exists)
- Warning level logging is active

### Docker/Kubernetes Usage

#### Dockerfile
```dockerfile
ENV DOTNET_ENVIRONMENT=Production
```

#### Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      containers:
      - name: sguard-checker
        env:
        - name: DOTNET_ENVIRONMENT
          value: "Production"
```

### Environment Usage in Program.cs

```csharp
private static void ConfigureServices(IServiceCollection services)
{
    // Environment is automatically detected
    var environmentName = GetEnvironmentName(); // "Development", "Staging", "Production"
    
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
        .Build();
    
    // IHostEnvironment is registered
    var hostEnvironment = new HostEnvironment
    {
        EnvironmentName = environmentName,
        // ...
    };
    services.AddSingleton<IHostEnvironment>(hostEnvironment);
}
```

### Best Practices

1. **Use DOTNET_ENVIRONMENT** - .NET 6+ standard
2. **Default to Production** - For security
3. **Make environment-specific files optional** - Base config should always be loaded
4. **Inject IHostEnvironment** - For environment control
5. **Keep sensitive data in environment variables** - Don't write to appsettings.json

## Logging Configuration

### Log Level Management

SGuard loggers are managed from the appsettings.json file. .NET's logging system provides namespace-based log level control.

### Namespace Hierarchy

Log levels are checked in the following order (from most specific to most general):

1. **Specific Namespace** (e.g., `SGuard.ConfigValidation.Services.ConfigLoader`)
2. **Namespace Segment** (e.g., `SGuard.ConfigValidation.Services`)
3. **Root Namespace** (e.g., `SGuard`)
4. **Default** (default for all namespaces)

### appsettings.json Example

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SGuard": "Information",                    // For all SGuard namespaces
      "SGuard.ConfigValidation.Services": "Debug", // Only for Services
      "SGuard.ConfigValidation.Validators": "Warning", // Only for Validators
      "ConsoleApp1": "Information"                // For CLI
    }
  }
}
```

### Log Level Values

- **Trace** (0): Most detailed logs, typically used in development
- **Debug** (1): Debug information for development and troubleshooting
- **Information** (2): General informational messages (default)
- **Warning** (3): Warning messages, non-error situations that require attention
- **Error** (4): Error messages when an operation fails
- **Critical** (5): Critical errors, situations that risk application crash
- **None** (6): Logging disabled

### Usage Examples

#### 1. Enable Debug for All SGuard Logs

```json
{
  "Logging": {
    "LogLevel": {
      "SGuard": "Debug"
    }
  }
}
```

#### 2. Enable Debug Only for Services Layer

```json
{
  "Logging": {
    "LogLevel": {
      "SGuard": "Information",
      "SGuard.ConfigValidation.Services": "Debug"
    }
  }
}
```

#### 3. Silence Validators (Only Warning and Above)

```json
{
  "Logging": {
    "LogLevel": {
      "SGuard": "Information",
      "SGuard.ConfigValidation.Validators": "Warning"
    }
  }
}
```

#### 4. Minimal Logging for Production Environment

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "SGuard": "Warning",
      "ConsoleApp1": "Error"
    }
  }
}
```

### Override with Environment Variables

You can also override log levels using environment variables:

```bash
# Linux/Mac
export Logging__LogLevel__SGuard=Debug

# Windows
set Logging__LogLevel__SGuard=Debug
```

### Log Usage in Code

```csharp
public class ConfigLoader
{
    private readonly ILogger<ConfigLoader> _logger;
    
    public ConfigLoader(ILogger<ConfigLoader> logger)
    {
        _logger = logger;
    }
    
    public void LoadConfig(string path)
    {
        // Log level check is performed automatically
        _logger.LogTrace("Trace: Most detailed information");
        _logger.LogDebug("Debug: Debug information");
        _logger.LogInformation("Information: General information");
        _logger.LogWarning("Warning: Warning");
        _logger.LogError("Error: Error");
        _logger.LogCritical("Critical: Critical error");
    }
}
```

### How Does Log Level Control Work?

1. Namespace is automatically determined when logger is created: `ILogger<ConfigLoader>` → `SGuard.ConfigValidation.Services.ConfigLoader`
2. Log level is checked from appsettings.json before writing the log message
3. If the message's log level is lower than the minimum level in configuration, the log is not written
4. Example: `LogLevel.Debug` message is not written if configuration has `Information`

### Performance Note

Log level checking is very fast and has minimal performance impact. However, be careful when using string interpolation:

```csharp
// ❌ Bad: String is always created
_logger.LogDebug($"Processing {largeObject}");

// ✅ Good: String is only created if Debug is active
_logger.LogDebug("Processing {LargeObject}", largeObject);
```

### Recommended Configurations

#### Development
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "SGuard": "Debug"
    }
  }
}
```

#### Staging
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SGuard": "Information"
    }
  }
}
```

#### Production
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "SGuard": "Warning"
    }
  }
}
```

## Notes

This console application was created to test the SGuard.ConfigValidation library and demonstrate example usage scenarios. The application simulates real-world scenarios using all features of the library and can be used as a reference during development.

