# SGuard.ConfigChecker

A lightweight tool to catch critical configuration issues **before runtime**.

## ✨ Why?

Misconfigured environments, missing connection strings, or wrong URLs can cause major issues after deployment.  
**SGuard.ConfigChecker** helps you detect these problems **early**, during application startup or in your CI/CD pipeline.

## 🚀 Features

### Supported Validators

- **required** → Ensures a specific key must exist in the target config file
- **min_len** → Validates minimum string length
- **max_len** → Validates maximum string length
- **eq** → Checks if value equals a specified value
- **ne** → Checks if value does not equal a specified value
- **gt** → Checks if value is greater than a specified value
- **gte** → Checks if value is greater than or equal to a specified value
- **lt** → Checks if value is less than a specified value
- **lte** → Checks if value is less than or equal to a specified value
- **in** → Checks if value is in a specified array

### Framework Support

- .NET 8.0 (LTS)
- .NET 9.0
- .NET 10.0

### Additional Features

- **YAML Configuration Support**: Load configuration and app settings from YAML files (`.yaml`, `.yml`)
- **JSON Schema Validation**: Validate `sguard.json` against JSON Schema for structure validation
- **Custom Validator Plugins**: Extend validation capabilities with custom validator plugins
- **Performance Optimizations**: Built-in caching for path resolution, schema validation, and reflection operations

## 📖 Usage

### CLI Commands

#### Validate Command (Default)

```bash
# Validate all environments
dotnet run -- validate

# Validate specific environment
dotnet run -- validate --env dev

# Validate with custom config file
dotnet run -- validate --config custom-sguard.json

# Validate all environments explicitly
dotnet run -- validate --all

# Output as JSON
dotnet run -- validate --output json

# Enable verbose logging
dotnet run -- validate --verbose
```

#### Command Options

- `--config, -c` - Path to the configuration file (default: `sguard.json`)
- `--env, -e` - Environment ID to validate (if not specified, all environments are validated)
- `--all, -a` - Validate all environments
- `--output, -o` - Output format: `json`, `text`, or `console` (default: `console`)
- `--verbose, -v` - Enable verbose logging

## 📂 Configuration Format

### Example `sguard.json`

```json
{
  "version": "1",
  "environments": [
    {
      "id": "dev",
      "name": "Development",
      "path": "appsettings.Development.json",
      "description": "Development environment"
    },
    {
      "id": "stag",
      "name": "Staging",
      "path": "appsettings.Staging.json",
      "description": "Staging environment"
    },
    {
      "id": "prod",
      "name": "Production",
      "path": "appsettings.Production.json",
      "description": "Production environment"
    }
  ],
  "rules": [
    {
      "id": "common-rules",
      "environments": ["dev", "stag", "prod"],
      "rule": {
        "id": "connection-string-rule",
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
                "message": "Connection string must be at least 10 characters long"
              },
              {
                "validator": "max_len",
                "value": 200,
                "message": "Connection string must be at most 200 characters long"
              }
            ]
          }
        ]
      }
    },
    {
      "id": "production-rules",
      "environments": ["prod"],
      "rule": {
        "id": "api-key-rule",
        "conditions": [
          {
            "key": "ApiKeys:ExternalService",
            "condition": [
              {
                "validator": "required",
                "message": "External service API key is required in production"
              },
              {
                "validator": "min_len",
                "value": 32,
                "message": "API key must be at least 32 characters"
              }
            ]
          }
        ]
      }
    }
  ]
}
```

### Configuration Schema

- **version** (string, **required**): Configuration version. Must not be null or empty.
- **environments** (array, **required**): List of environment definitions. Must contain at least one environment.
  - **id** (string, **required**): Unique environment identifier. Must not be null or empty. Must be unique across all environments.
  - **name** (string, **required**): Environment display name. Must not be null or empty.
  - **path** (string, **required**): Path to the appsettings file for this environment. Must not be null or empty.
  - **description** (string, **optional**): Environment description. Can be null or empty.
- **rules** (array, **optional**): List of validation rules. Can be empty if no validation rules are needed.
  - **id** (string, **required**): Unique rule identifier. Must not be null or empty. Must be unique across all rules.
  - **environments** (array, **required**): List of environment IDs where this rule applies. Must contain at least one environment ID. All environment IDs must exist in the environments list.
  - **rule** (object, **required**): Rule definition. Must not be null.
    - **id** (string, **required**): Rule detail identifier. Must not be null or empty.
    - **conditions** (array, **required**): List of validation conditions. Must contain at least one condition.
      - **key** (string, **required**): Configuration key to validate (supports colon-separated nested keys). Must not be null or empty.
      - **condition** (array, **required**): List of validators to apply. Must contain at least one validator.
        - **validator** (string, **required**): Validator type (required, min_len, max_len, eq, ne, gt, gte, lt, lte, in). Must not be null or empty. Must be one of the supported validator types.
        - **value** (any, **optional**): Value for comparison validators. Optional for "required" validator, but required for: min_len, max_len, eq, ne, gt, gte, lt, lte, in.
        - **message** (string, **required**): Error message to display if validation fails. Must not be null or empty.

## 🔧 Programmatic Usage

### Using the Library

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Validators;

// Setup services with logging
var loggerFactory = NullLoggerFactory.Instance;
var validatorFactoryLogger = NullLogger<ValidatorFactory>.Instance;
var configLoaderLogger = NullLogger<ConfigLoader>.Instance;
var fileValidatorLogger = NullLogger<FileValidator>.Instance;
var ruleEngineLogger = NullLogger<RuleEngine>.Instance;

var validatorFactory = new ValidatorFactory(validatorFactoryLogger);
var configLoader = new ConfigLoader(configLoaderLogger);
var fileValidator = new FileValidator(validatorFactory, fileValidatorLogger);
var ruleEngine = new RuleEngine(configLoader, fileValidator, validatorFactory, ruleEngineLogger);

// Validate environment
var result = ruleEngine.ValidateEnvironment("sguard.json", "prod");

if (result.IsSuccess && !result.HasValidationErrors)
{
    Console.WriteLine("Validation passed!");
}
else
{
    Console.WriteLine($"Validation failed: {result.ErrorMessage}");
    foreach (var error in result.ValidationResults.SelectMany(r => r.Errors))
    {
        Console.WriteLine($"  - {error.Key}: {error.Message}");
    }
}
```

### Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Validators;
using SGuard.ConfigValidation.Validators.Plugin;

var services = new ServiceCollection();

// Register logging (required)
services.AddLogging(builder => builder.AddConsole());

// Register core services
services.AddSingleton<IValidatorFactory, ValidatorFactory>();
services.AddSingleton<IConfigLoader, ConfigLoader>();
services.AddSingleton<IFileValidator, FileValidator>();
services.AddSingleton<IRuleEngine, RuleEngine>();

// Register optional services
services.AddSingleton<IYamlLoader, YamlLoader>();
services.AddSingleton<ISchemaValidator, JsonSchemaValidator>();
services.AddSingleton<ValidatorPluginDiscovery>();

var serviceProvider = services.BuildServiceProvider();
var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();
```

### YAML Configuration Support

The library supports loading configuration and app settings from YAML files:

```csharp
using SGuard.ConfigValidation.Services;

var logger = NullLogger<YamlLoader>.Instance;
var yamlLoader = new YamlLoader(logger);

// Load configuration from YAML
var config = yamlLoader.LoadConfig("sguard.yaml");

// Load app settings from YAML
var appSettings = yamlLoader.LoadAppSettings("appsettings.yaml");
```

### JSON Schema Validation

Validate your `sguard.json` configuration against a JSON Schema:

```csharp
using SGuard.ConfigValidation.Services;

var schemaValidator = new JsonSchemaValidator();

// Validate JSON content against schema content
var result = schemaValidator.Validate(jsonContent, schemaContent);

// Or validate against a schema file
var result = schemaValidator.ValidateAgainstFile(jsonContent, "sguard.schema.json");

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Schema validation error: {error}");
    }
}
```

### Custom Validator Plugins

Create custom validators by implementing `IValidatorPlugin`:

```csharp
using SGuard.ConfigValidation.Validators;
using SGuard.ConfigValidation.Validators.Plugin;

public class CustomEmailValidator : IValidatorPlugin
{
    public string ValidatorType => "email";
    
    public IValidator<object> Validator => new EmailValidator();
}

// Register plugin via ValidatorPluginDiscovery
var logger = NullLogger<ValidatorPluginDiscovery>.Instance;
var pluginDiscovery = new ValidatorPluginDiscovery(logger);
var plugins = pluginDiscovery.DiscoverValidators(new[] { "./plugins" });
```

## 📊 Output Formats

### Console Output (Default)

```
🔍 Validating Environments:

📁 Environment: Development
   File: appsettings.Development.json
   Status: ✅ PASS
   Validated 2 rule(s)

📁 Environment: Production
   File: appsettings.Production.json
   Status: ❌ FAIL
   Errors (1):
     🔑 ConnectionStrings:DefaultConnection
        ✖ required: Connection string is required
        💡 Current value: 

✅ All validations passed successfully!
```

### JSON Output

```json
{
  "success": true,
  "errorMessage": "",
  "hasValidationErrors": false,
  "results": [
    {
      "path": "appsettings.Development.json",
      "isValid": true,
      "errorCount": 0,
      "results": [...],
      "errors": []
    }
  ]
}
```

## 🔒 Security Configuration

SGuard.ConfigChecker includes built-in security limits to prevent DoS (Denial of Service) attacks. These limits can be configured via `appsettings.json` or environment variables.

### Default Security Limits

| Setting | Default Value | Hard Limit (Maximum) | Description |
|---------|--------------|---------------------|-------------|
| `MaxFileSizeBytes` | 104857600 (100 MB) | 524288000 (500 MB) | Maximum allowed file size for configuration and app settings files |
| `MaxEnvironmentsCount` | 1000 | 5000 | Maximum number of environments allowed in a configuration file |
| `MaxRulesCount` | 10000 | 50000 | Maximum number of rules allowed in a configuration file |
| `MaxConditionsPerRule` | 1000 | 5000 | Maximum number of conditions allowed per rule |
| `MaxValidatorsPerCondition` | 100 | 500 | Maximum number of validators allowed per condition |
| `MaxPathCacheSize` | 10000 | 100000 | Maximum number of entries in the path resolver cache |
| `MaxPathLength` | 4096 | 16384 | Maximum length for a single path string (characters) |
| `MaxJsonDepth` | 64 | 256 | Maximum depth for nested JSON/YAML structures |

**Note:** Hard limits are absolute maximums that cannot be exceeded even if configured higher. If a configuration value exceeds the hard limit, it will be automatically clamped to the hard limit and a warning will be logged.

### Configuring Security Limits

Add a `Security` section to your `appsettings.json`:

```json
{
  "Security": {
    "MaxFileSizeBytes": 104857600,
    "MaxEnvironmentsCount": 1000,
    "MaxRulesCount": 10000,
    "MaxConditionsPerRule": 1000,
    "MaxValidatorsPerCondition": 100,
    "MaxPathCacheSize": 10000,
    "MaxPathLength": 4096,
    "MaxJsonDepth": 64
  }
}
```

### Environment Variables

You can also override security limits using environment variables:

```bash
# Linux/Mac
export Security__MaxFileSizeBytes=209715200  # 200 MB
export Security__MaxEnvironmentsCount=2000

# Windows
set Security__MaxFileSizeBytes=209715200
set Security__MaxEnvironmentsCount=2000
```

### Security Features

- **Path Traversal Protection**: Prevents access to files outside the base directory
- **Symlink Attack Protection**: Validates symlinks to prevent unauthorized file access
- **DoS Protection**: Resource limits prevent memory exhaustion and excessive processing
- **Cache Poisoning Protection**: Sanitizes cache keys to prevent injection attacks

## 🧪 Testing

Run tests with:

```bash
dotnet test
```

### Performance Benchmarks

The project includes BenchmarkDotNet benchmarks for performance-critical operations. Run benchmarks with:

```bash
cd SGuard.ConfigValidation.Tests
dotnet run --configuration Release --project SGuard.ConfigValidation.Tests.csproj
```

This will execute benchmarks for:
- Large file loading performance
- Validator factory lookup performance
- File validation performance
- Memory allocation tracking

## 🗺️ Roadmap

- [x] JSON config support
- [x] Multiple validator types
- [x] Environment-based validation
- [x] CLI tool with System.CommandLine
- [x] JSON and console output formats
- [x] Structured logging
- [x] Dependency injection support
- [x] YAML config support
- [x] Custom validator plugins
- [x] Schema validation for sguard.json
- [x] Performance optimizations (caching, memory allocation improvements)
- [ ] CI/CD pipeline integration examples

## 📝 License

[Add your license information here]

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

For questions or contributions, feel free to open an issue or pull request!
