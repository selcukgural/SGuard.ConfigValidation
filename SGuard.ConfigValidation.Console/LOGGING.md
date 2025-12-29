# SGuard Logging Configuration

## Log Level Management

SGuard loggers are managed from the appsettings.json file. .NET's logging system provides namespace-based log level control.

## Namespace Hierarchy

Log levels are checked in the following order (from most specific to most general):

1. **Specific Namespace** (e.g., `SGuard.ConfigValidation.Services.ConfigLoader`)
2. **Namespace Segment** (e.g., `SGuard.ConfigValidation.Services`)
3. **Root Namespace** (e.g., `SGuard`)
4. **Default** (default for all namespaces)

## appsettings.json Example

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

## Log Level Values

- **Trace** (0): Most detailed logs, typically used in development
- **Debug** (1): Debug information for development and troubleshooting
- **Information** (2): General informational messages (default)
- **Warning** (3): Warning messages, non-error situations that require attention
- **Error** (4): Error messages when an operation fails
- **Critical** (5): Critical errors, situations that risk application crash
- **None** (6): Logging disabled

## Usage Examples

### 1. Enable Debug for All SGuard Logs

```json
{
  "Logging": {
    "LogLevel": {
      "SGuard": "Debug"
    }
  }
}
```

### 2. Enable Debug Only for Services Layer

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

### 3. Silence Validators (Only Warning and Above)

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

### 4. Minimal Logging for Production Environment

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

## Override with Environment Variables

You can also override log levels using environment variables:

```bash
# Linux/Mac
export Logging__LogLevel__SGuard=Debug

# Windows
set Logging__LogLevel__SGuard=Debug
```

## Log Usage in Code

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

## How Does Log Level Control Work?

1. Namespace is automatically determined when logger is created: `ILogger<ConfigLoader>` → `SGuard.ConfigValidation.Services.ConfigLoader`
2. Log level is checked from appsettings.json before writing the log message
3. If the message's log level is lower than the minimum level in configuration, the log is not written
4. Example: `LogLevel.Debug` message is not written if configuration has `Information`

## Performance Note

Log level checking is very fast and has minimal performance impact. However, be careful when using string interpolation:

```csharp
// ❌ Bad: String is always created
_logger.LogDebug($"Processing {largeObject}");

// ✅ Good: String is only created if Debug is active
_logger.LogDebug("Processing {LargeObject}", largeObject);
```

## Recommended Configurations

### Development
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

### Staging
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

### Production
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
