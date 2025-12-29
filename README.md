# SGuard.ConfigValidation

[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A lightweight, production-ready tool to catch critical configuration issues **before runtime**. Validate your configuration files during application startup or in your CI/CD pipeline.

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

## 📦 Projects

This repository contains multiple projects:

- **[SGuard.ConfigValidation](./SGuard.ConfigValidation/)** - Core library (NuGet package)
  - Full API documentation and usage examples
  - See [detailed README](./SGuard.ConfigValidation/README.md)

- **[SGuard.ConfigValidation.Console](./SGuard.ConfigValidation.Console/)** - CLI application
  - Command-line tool for configuration validation
  - See [detailed README](./SGuard.ConfigValidation.Console/README.md)

- **[SGuard.ConfigValidation.Test](./SGuard.ConfigValidation.Test/)** - Test suite
  - Comprehensive unit and integration tests

## 🏃 Quick Start

### Using the CLI

```bash
# Clone the repository
git clone https://github.com/yourusername/SGuard.ConfigValidation.git
cd SGuard.ConfigValidation

# Run validation
cd SGuard.ConfigValidation.Console
dotnet run -- validate --all
```

### Using as a Library

```csharp
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
```

### Example Configuration (`sguard.json`)

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
              },
              {
                "validator": "min_len",
                "value": 10,
                "message": "Connection string must be at least 10 characters"
              }
            ]
          }
        ]
      }
    }
  ]
}
```

## 🔧 Supported Frameworks

- .NET 8.0 (LTS)
- .NET 9.0
- .NET 10.0

## 📖 Documentation

- **[Core Library Documentation](./SGuard.ConfigValidation/README.md)** - Complete API reference, usage examples, and advanced features
- **[Console Application Documentation](./SGuard.ConfigValidation.Console/README.md)** - CLI usage, environment management, and logging configuration

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory:"./TestResults"
```

## 🔒 Security

SGuard.ConfigValidation includes built-in security features:

- **DoS Protection**: Resource limits prevent memory exhaustion
- **Path Traversal Protection**: Prevents access to files outside base directory
- **Symlink Attack Protection**: Validates symlinks to prevent unauthorized access
- **Configurable Limits**: Adjustable security limits via configuration

See [Security Configuration](./SGuard.ConfigValidation/README.md#-security-configuration) for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔗 Links

- [Core Library README](./SGuard.ConfigValidation/README.md)
- [Console Application README](./SGuard.ConfigValidation.Console/README.md)
- [License](LICENSE)

---

For questions or contributions, feel free to open an issue or pull request!

