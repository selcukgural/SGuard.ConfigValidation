---
sidebar_position: 2
---

# Core API

Learn about the core interfaces and classes in SGuard.ConfigValidation.

## IRuleEngine

The main interface for validating configuration.

### Methods

#### ValidateEnvironmentAsync

Validates a specific environment defined in the rules file.

```csharp
Task<ValidationResult> ValidateEnvironmentAsync(
    string rulesFilePath, 
    string environmentId
)
```

**Parameters:**
- `rulesFilePath` (string): Path to the sguard.json file
- `environmentId` (string): Environment ID to validate (e.g., "prod", "dev")

**Returns:** `ValidationResult` - Contains validation status and any errors

**Example:**

```csharp
var result = await ruleEngine.ValidateEnvironmentAsync("sguard.json", "prod");

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine(error);
    }
}
```

## ValidationResult

Represents the result of a validation operation.

### Properties

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; }
}
```

**Properties:**
- `IsValid` (bool): True if validation passed, false otherwise
- `Errors` (List&lt;string&gt;): List of error messages (empty if valid)

## Extension Methods

### AddSGuardConfigValidation

Registers SGuard.ConfigValidation services with dependency injection.

```csharp
public static IServiceCollection AddSGuardConfigValidation(
    this IServiceCollection services
)
```

**Usage:**

```csharp
using Microsoft.Extensions.DependencyInjection;
using SGuard.ConfigValidation.Extensions;

var services = new ServiceCollection();
services.AddSGuardConfigValidation();
```

This registers:
- `IRuleEngine`
- `IRuleLoader`
- All built-in validators
- Configuration loaders (JSON, YAML)

## Complete Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Extensions;
using SGuard.ConfigValidation.Services.Abstract;

// Setup DI container
var services = new ServiceCollection();

// Add logging
services.AddLogging(builder => 
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Register SGuard.ConfigValidation
services.AddSGuardConfigValidation();

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Get rule engine
var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();

// Validate environment
var result = await ruleEngine.ValidateEnvironmentAsync(
    "sguard.json", 
    "prod"
);

// Handle result
if (!result.IsValid)
{
    Console.WriteLine("❌ Validation failed:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
    Environment.Exit(1);
}

Console.WriteLine("✅ Configuration is valid!");
```

## ASP.NET Core Integration

In ASP.NET Core applications:

```csharp title="Program.cs"
using SGuard.ConfigValidation.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register SGuard.ConfigValidation
builder.Services.AddSGuardConfigValidation();

var app = builder.Build();

// Validate configuration at startup
using (var scope = app.Services.CreateScope())
{
    var ruleEngine = scope.ServiceProvider.GetRequiredService<IRuleEngine>();
    var result = await ruleEngine.ValidateEnvironmentAsync(
        "sguard.json", 
        builder.Environment.EnvironmentName.ToLower()
    );

    if (!result.IsValid)
    {
        throw new InvalidOperationException(
            $"Configuration validation failed: {string.Join(", ", result.Errors)}"
        );
    }
}

app.Run();
```

## Next Steps

- [**Validators**](./validators) - Learn about all available validators
- [**Getting Started**](../getting-started/quick-start) - Quick start guide
