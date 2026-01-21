---
sidebar_position: 2
---

# Quick Start

Get started with SGuard.ConfigValidation in minutes.

## Step 1: Create Configuration Files

First, create your application settings file:

```json title="appsettings.Production.json"
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-db;Database=myapp;User Id=sa;Password=..."
  },
  "ApiSettings": {
    "BaseUrl": "https://api.example.com",
    "Timeout": 30
  }
}
```

## Step 2: Create Validation Rules

Create `sguard.json` in your project root:

```json title="sguard.json"
{
  "version": "1",
  "environments": [
    {
      "id": "prod",
      "name": "Production",
      "path": "appsettings.Production.json"
    }
  ],
  "rules": [
    {
      "id": "connection-string-rule",
      "environments": ["prod"],
      "rule": {
        "id": "required-connection-string",
        "conditions": [
          {
            "key": "ConnectionStrings:DefaultConnection",
            "condition": [
              {
                "validator": "required",
                "message": "Connection string is required in production"
              },
              {
                "validator": "min_len",
                "value": 20,
                "message": "Connection string must be at least 20 characters"
              }
            ]
          }
        ]
      }
    },
    {
      "id": "api-url-rule",
      "environments": ["prod"],
      "rule": {
        "id": "api-base-url",
        "conditions": [
          {
            "key": "ApiSettings:BaseUrl",
            "condition": [
              {
                "validator": "required",
                "message": "API Base URL is required"
              }
            ]
          }
        ]
      }
    }
  ]
}
```

## Step 3: Setup Dependency Injection

Register SGuard.ConfigValidation services in your application:

```csharp title="Program.cs"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Extensions;

var services = new ServiceCollection();

// Add logging
services.AddLogging(builder => builder.AddConsole());

// Register SGuard.ConfigValidation services
services.AddSGuardConfigValidation();

var serviceProvider = services.BuildServiceProvider();
```

## Step 4: Validate Configuration

Use the `IRuleEngine` to validate your configuration:

```csharp title="Program.cs"
using SGuard.ConfigValidation.Services.Abstract;

var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();

// Validate the 'prod' environment
var result = await ruleEngine.ValidateEnvironmentAsync("sguard.json", "prod");

if (!result.IsValid)
{
    Console.WriteLine("❌ Configuration validation failed!");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
    Environment.Exit(1); // Exit with error code
}

Console.WriteLine("✅ Configuration is valid!");
```

## Complete Example

Here's a complete console application:

```csharp title="Program.cs"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Extensions;
using SGuard.ConfigValidation.Services.Abstract;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddSGuardConfigValidation();

var serviceProvider = services.BuildServiceProvider();
var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();

var result = await ruleEngine.ValidateEnvironmentAsync("sguard.json", "prod");

if (!result.IsValid)
{
    Console.WriteLine("❌ Configuration validation failed!");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
    return 1;
}

Console.WriteLine("✅ Configuration is valid!");
return 0;
```

## Running the Application

```bash
dotnet run
```

### Expected Output (Success)

```
✅ Configuration is valid!
```

### Expected Output (Failure)

```
❌ Configuration validation failed!
  - Connection string is required in production
  - API Base URL is required
```

## Next Steps

- [**Configuration**](./configuration) - Deep dive into sguard.json structure
- [**Validators**](../api/validators) - Learn about all available validators

## Next Steps
