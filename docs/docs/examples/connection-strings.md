---
sidebar_position: 1
---

# Connection String Validation

Validate database connection strings in different environments.

## Scenario

You need to ensure that:
1. Connection strings are present in production and staging
2. Connection strings are not empty
3. Connection strings have minimum length (basic sanity check)
4. Development can use localhost, but production cannot

## Configuration Files

### appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-db.example.com;Database=MyApp;User Id=sa;Password=SecureP@ss123"
  }
}
```

### appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyApp_Dev;Trusted_Connection=True;"
  }
}
```

## Validation Rules

```json title="sguard.json"
{
  "version": "1",
  "environments": [
    {
      "id": "dev",
      "name": "Development",
      "path": "appsettings.Development.json"
    },
    {
      "id": "prod",
      "name": "Production",
      "path": "appsettings.Production.json"
    }
  ],
  "rules": [
    {
      "id": "connection-string-required",
      "environments": ["dev", "prod"],
      "rule": {
        "id": "db-connection-validation",
        "conditions": [
          {
            "key": "ConnectionStrings:DefaultConnection",
            "condition": [
              {
                "validator": "required",
                "message": "Database connection string is required"
              },
              {
                "validator": "min_len",
                "value": 20,
                "message": "Connection string appears invalid (too short)"
              }
            ]
          }
        ]
      }
    },
    {
      "id": "prod-no-localhost",
      "environments": ["prod"],
      "rule": {
        "id": "prod-security-check",
        "conditions": [
          {
            "key": "ConnectionStrings:DefaultConnection",
            "condition": [
              {
                "validator": "ne",
                "value": "localhost",
                "message": "Production cannot use 'localhost' in connection string"
              }
            ]
          }
        ]
      }
    }
  ]
}
```

## Validation Code

```csharp
using Microsoft.Extensions.DependencyInjection;
using SGuard.ConfigValidation.Extensions;
using SGuard.ConfigValidation.Services.Abstract;

var services = new ServiceCollection();
services.AddSGuardConfigValidation();
var serviceProvider = services.BuildServiceProvider();

var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();

// Validate production environment
Console.WriteLine("Validating Production environment...");
var prodResult = await ruleEngine.ValidateEnvironmentAsync("sguard.json", "prod");

if (!prodResult.IsValid)
{
    Console.WriteLine("❌ Production validation failed:");
    foreach (var error in prodResult.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
else
{
    Console.WriteLine("✅ Production configuration is valid!");
}

// Validate development environment
Console.WriteLine("\nValidating Development environment...");
var devResult = await ruleEngine.ValidateEnvironmentAsync("sguard.json", "dev");

if (!devResult.IsValid)
{
    Console.WriteLine("❌ Development validation failed:");
    foreach (var error in devResult.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
else
{
    Console.WriteLine("✅ Development configuration is valid!");
}
```

## Expected Output

### When Valid

```
Validating Production environment...
✅ Production configuration is valid!

Validating Development environment...
✅ Development configuration is valid!
```

### When Invalid (Production uses localhost)

```
Validating Production environment...
❌ Production validation failed:
  - Production cannot use 'localhost' in connection string

Validating Development environment...
✅ Development configuration is valid!
```

## CI/CD Integration

Add to your deployment pipeline:

```yaml title=".github/workflows/deploy.yml"
- name: Validate Configuration
  run: |
    dotnet run --project MyApp.ConfigValidator -- prod
    if [ $? -ne 0 ]; then
      echo "Configuration validation failed"
      exit 1
    fi
```

## Next Steps

- [**Validators**](../api/validators) - Learn about all validators
- [**Getting Started**](../getting-started/quick-start) - Quick start guide
