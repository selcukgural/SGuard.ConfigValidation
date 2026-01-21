---
sidebar_position: 1
---

# Introduction

**SGuard.ConfigValidation** is a lightweight, production-ready tool to catch critical configuration issues **before runtime**. Validate your configuration files during application startup or in your CI/CD pipeline.

## ✨ Why SGuard.ConfigValidation?

Misconfigured environments, missing connection strings, or wrong URLs can cause major issues after deployment.  
**SGuard.ConfigValidation** helps you detect these problems **early**, preventing runtime failures and reducing debugging time.

## 🚀 Key Features

- ✅ **Multiple Validators**: `required`, `min_len`, `max_len`, `eq`, `ne`, `gt`, `gte`, `lt`, `lte`, `in`
- ✅ **JSON & YAML Support**: Load configuration and app settings from JSON and YAML files
- ✅ **JSON Schema Validation**: Validate `sguard.json` against JSON Schema
- ✅ **Custom Validator Plugins**: Extend validation capabilities with custom validators
- ✅ **CLI Tool**: Command-line interface for easy validation
- ✅ **Dependency Injection**: Full DI support with extension methods
- ✅ **Security Features**: Built-in DoS protection, path traversal protection, and resource limits
- ✅ **Performance Optimized**: Caching, streaming, and parallel validation support
- ✅ **Multiple Output Formats**: Console, JSON, and text file output
- ✅ **Comprehensive Testing**: High test coverage with xUnit

## 🔧 Supported Frameworks

- .NET 8.0 (LTS)
- .NET 9.0
- .NET 10.0

## 📦 Installation

Install the NuGet package:

```bash
dotnet add package SGuard.ConfigValidation
```

Or via Package Manager Console:

```powershell
Install-Package SGuard.ConfigValidation
```

## 🏃 Quick Start

### 1. Create Configuration File (`sguard.json`)

```json
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
                "message": "Connection string is required"
              }
            ]
          }
        ]
      }
    }
  ]
}
```

### 2. Register Services

```csharp
using Microsoft.Extensions.DependencyInjection;
using SGuard.ConfigValidation.Extensions;

var services = new ServiceCollection();
services.AddSGuardConfigValidation();
var serviceProvider = services.BuildServiceProvider();
```

### 3. Validate Configuration

```csharp
using SGuard.ConfigValidation.Services.Abstract;

var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();
var result = await ruleEngine.ValidateEnvironmentAsync("sguard.json", "prod");

if (!result.IsValid)
{
    Console.WriteLine("Validation failed!");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"- {error}");
    }
}
```

## 📖 Next Steps

- [**Getting Started**](./getting-started/installation) - Detailed installation and setup guide
- [**API Reference**](./api/validators) - Complete API documentation
- [**Examples**](./examples/connection-strings) - Real-world scenarios and code samples

## 🤝 Contributing

Contributions are welcome! Please visit our [GitHub repository](https://github.com/selcukgural/SGuard.ConfigValidation) to:
- Report issues
- Submit pull requests
- Join discussions

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/selcukgural/SGuard.ConfigValidation/blob/main/LICENSE) file for details.
