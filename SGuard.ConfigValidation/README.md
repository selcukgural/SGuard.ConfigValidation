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

- **JSON Configuration Support**: Load configuration and app settings from JSON files (`.json`). JSON is the default format for both `sguard.json` configuration files and `appsettings.json` files.
- **YAML Configuration Support**: Load configuration and app settings from YAML files (`.yaml`, `.yml`) when YAML loader is provided
- **JSON Schema Validation**: Validate `sguard.json` against JSON Schema for structure validation
- **Custom Validator Plugins**: Extend validation capabilities with custom validator plugins
- **Performance Optimizations**: Built-in caching for path resolution, schema validation, and reflection operations
- **Post-Validation Hooks**: Execute scripts, webhooks, or send notifications (Slack/Teams) after validation completes. Supports environment-specific hooks and template variables for dynamic content.

## 📖 Usage

### CLI Commands

#### Validate Command (Default)

```bash
# Validate all environments
dotnet run -- validate

# Validate specific environment
dotnet run -- validate --env dev

# Validate with hooks (hooks execute automatically if configured in sguard.json)
dotnet run -- validate --env prod
# Hooks configured in sguard.json will execute automatically after validation

# Validate with custom config file
dotnet run -- validate --config custom-sguard.json

# Validate all environments explicitly
dotnet run -- validate --all

# Output as JSON
dotnet run -- validate --output json

# Write results to JSON file
dotnet run -- validate --output json --output-file results.json

# Write results to text file
dotnet run -- validate --output text --output-file results.txt

# Enable verbose logging
dotnet run -- validate --verbose
```

#### Command Options

- `--config, -c` - Path to the configuration file (default: `sguard.json`)
- `--env, -e` - Environment ID to validate (if not specified, all environments are validated)
- `--all, -a` - Validate all environments
- `--output, -o` - Output format: `json`, `text`, or `console` (default: `console`)
- `--output-file, -f` - Path to output file. If specified, results will be written to this file instead of console. Works with both json and text formats.
- `--verbose, -v` - Enable verbose logging

## 📂 Configuration Format

SGuard.ConfigChecker supports **JSON** (default) and **YAML** configuration formats. JSON is the default format and is used for both `sguard.json` configuration files and `appsettings.json` files.

### Example `sguard.json` (JSON Format)

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
  ],
  "hooks": {
    "global": {
      "onSuccess": [
        {
          "type": "webhook",
          "url": "https://api.example.com/webhook/success",
          "method": "POST",
          "headers": {
            "Authorization": "Bearer ${WEBHOOK_TOKEN}",
            "Content-Type": "application/json"
          },
          "body": {
            "status": "{{status}}",
            "environment": "{{environment}}",
            "message": "Validation succeeded"
          }
        }
      ],
      "onFailure": [
        {
          "type": "notification",
          "provider": "slack",
          "webhookUrl": "https://hooks.slack.com/services/YOUR/WEBHOOK/URL",
          "channel": "#alerts",
          "message": "Validation failed for {{environment}}",
          "color": "{{statusColor}}"
        }
      ]
    },
    "environments": {
      "prod": {
        "onSuccess": [
          {
            "type": "script",
            "command": "./scripts/deploy.sh",
            "arguments": ["--env", "prod"],
            "timeout": 30000
          }
        ],
        "onFailure": [
          {
            "type": "notification",
            "provider": "teams",
            "webhookUrl": "https://outlook.office.com/webhook/YOUR/WEBHOOK/URL",
            "title": "Production Validation Failed",
            "summary": "Environment: {{environment}}",
            "themeColor": "{{statusColor}}"
          }
        ]
      }
    }
  }
}
```

### Configuration Schema

- **version** (string, **required**): Configuration version. Must not be null or empty.
- **environments** (array, **required**): List of environment definitions. Must contain at least one environment.
  - **id** (string, **required**): Unique environment identifier. Must not be null or empty. Must be unique across all environments.
  - **name** (string, **required**): Environment display name. Must not be null or empty.
  - **path** (string, **required**): Path to the appsettings file for this environment. Must not be null or empty.
  - **description** (string, **optional**): Environment description. Can be null or empty.
- **rules** (array, **required**): List of validation rules. Must contain at least one rule. Cannot be empty.
  - **id** (string, **required**): Unique rule identifier. Must not be null or empty. Must be unique across all rules.
  - **environments** (array, **required**): List of environment IDs where this rule applies. Must contain at least one environment ID. All environment IDs must exist in the environments list.
  - **rule** (object, **required**): Rule definition. Must not be null.
    - **id** (string, **required**): Rule detail identifier. Must not be null or empty.
    - **conditions** (array, **required**): List of validation conditions. Must contain at least one condition.
- **hooks** (object, **optional**): Post-validation hooks configuration. Hooks are executed after validation completes.
  - **global** (object, **optional**): Global hooks that run for all environments.
    - **onSuccess** (array, **optional**): Hooks to execute when validation succeeds (no errors).
    - **onFailure** (array, **optional**): Hooks to execute when validation fails (has errors or system error).
    - **onValidationError** (array, **optional**): Hooks to execute when validation errors are found (exit code 1).
    - **onSystemError** (array, **optional**): Hooks to execute when a system error occurs (exit code 2).
  - **environments** (object, **optional**): Environment-specific hooks. Key is the environment ID.
    - **[environmentId]** (object): Hooks configuration for a specific environment.
      - **onSuccess** (array, **optional**): Hooks to execute when validation succeeds for this environment.
      - **onFailure** (array, **optional**): Hooks to execute when validation fails for this environment.
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
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Validators;

// Setup security options
var securityOptions = Options.Create(new SecurityOptions());

// Setup services with logging
var loggerFactory = NullLoggerFactory.Instance;
var validatorFactoryLogger = NullLogger<ValidatorFactory>.Instance;
var configLoaderLogger = NullLogger<ConfigLoader>.Instance;
var fileValidatorLogger = NullLogger<FileValidator>.Instance;
var ruleEngineLogger = NullLogger<RuleEngine>.Instance;

var validatorFactory = new ValidatorFactory(validatorFactoryLogger);
var configLoader = new ConfigLoader(configLoaderLogger, securityOptions);
var fileValidator = new FileValidator(validatorFactory, fileValidatorLogger);
var ruleEngine = new RuleEngine(
    configLoader, 
    fileValidator, 
    validatorFactory, 
    ruleEngineLogger, 
    securityOptions);

// Validate environment
var result = ruleEngine.ValidateEnvironment("sguard.json", "prod");

if (result.IsSuccess && !result.HasValidationErrors)
{
    Console.WriteLine("Validation passed!");
}
else
{
    if (!string.IsNullOrEmpty(result.ErrorMessage))
    {
        Console.WriteLine($"Validation failed: {result.ErrorMessage}");
    }
    
    foreach (var validationResult in result.ValidationResults)
    {
        foreach (var error in validationResult.Errors)
        {
            Console.WriteLine($"  - {error.Key}: {error.Message}");
        }
    }
}
```

### Dependency Injection (Recommended)

Using the extension methods for easy registration:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Extensions;

var services = new ServiceCollection();

// Register logging (required)
services.AddLogging(builder => builder.AddConsole());

// Build configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Register all SGuard.ConfigValidation services with one line
services.AddSGuardConfigValidation(configuration);

// Or specify a logging level directly (logging will be automatically configured)
services.AddSGuardConfigValidation(configuration, logLevel: LogLevel.Debug);

var serviceProvider = services.BuildServiceProvider();
var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();

// Example: Validate all environments
var result = ruleEngine.ValidateAllEnvironments("sguard.json");
if (result.IsSuccess)
{
    Console.WriteLine($"Validated {result.ValidationResults.Count} environment(s)");
}
```

### Manual Dependency Injection Registration

If you prefer manual registration or need more control:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Validators;
using SGuard.ConfigValidation.Validators.Plugin;

var services = new ServiceCollection();

// Register logging (required)
services.AddLogging(builder => builder.AddConsole());

// Register configuration and security options
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

services.Configure<SecurityOptions>(configuration.GetSection("Security"));

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

// Example: Validate all environments
var result = ruleEngine.ValidateAllEnvironments("sguard.json");
if (result.IsSuccess)
{
    Console.WriteLine($"Validated {result.ValidationResults.Count} environment(s)");
}
```

### Logging Level Configuration

You can specify a logging level directly when registering services. This will automatically configure logging for SGuard namespaces:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Extensions;

var services = new ServiceCollection();

// Register with Debug level (verbose logging)
services.AddSGuardConfigValidation(logLevel: LogLevel.Debug);

// Or for production, use Warning level (only warnings and errors)
services.AddSGuardConfigValidation(logLevel: LogLevel.Warning);

// Or for development, use Trace level (most verbose)
services.AddSGuardConfigValidation(logLevel: LogLevel.Trace);

var serviceProvider = services.BuildServiceProvider();
var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();
```

**Note**: When `logLevel` is specified, logging is automatically configured. You don't need to call `AddLogging()` separately. However, you may still want to add a logging provider (e.g., `AddConsole()`) if you need console output.

**Available Log Levels**:
- `LogLevel.Trace` - Most verbose, includes all log messages
- `LogLevel.Debug` - Debug information for troubleshooting
- `LogLevel.Information` - General informational messages (default)
- `LogLevel.Warning` - Warning messages and above
- `LogLevel.Error` - Error messages and above
- `LogLevel.Critical` - Only critical errors

### Core Services Only

If you only need core services without YAML or schema validation:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Extensions;

var services = new ServiceCollection();

// Register logging
services.AddLogging(builder => builder.AddConsole());

// Register only core services
services.AddSGuardConfigValidationCore();

// Or specify a logging level directly
services.AddSGuardConfigValidationCore(logLevel: LogLevel.Debug);

// Optionally add YAML support later
// services.AddSingleton<IYamlLoader, YamlLoader>();

var serviceProvider = services.BuildServiceProvider();
var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();
```

### YAML Configuration Support

The library supports loading configuration and app settings from YAML files:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Services;

// Setup security options
var securityOptions = Options.Create(new SecurityOptions());
var logger = NullLogger<YamlLoader>.Instance;
var yamlLoader = new YamlLoader(logger, securityOptions);

// Load configuration from YAML
var config = yamlLoader.LoadConfig("sguard.yaml");

// Load app settings from YAML
var appSettings = yamlLoader.LoadAppSettings("appsettings.yaml");

// Example: Access loaded configuration
Console.WriteLine($"Loaded {config.Environments.Count} environment(s)");
Console.WriteLine($"Loaded {config.Rules.Count} rule(s)");
```

### JSON Schema Validation

Validate your `sguard.json` configuration against a JSON Schema:

```csharp
using SGuard.ConfigValidation.Services;
using System.Text.Json;

var schemaValidator = new JsonSchemaValidator();

// Example 1: Validate JSON content against schema content
var jsonContent = """
{
  "version": "1",
  "environments": [{"id": "dev", "name": "Development", "path": "appsettings.Dev.json"}],
  "rules": []
}
""";

var schemaContent = """
{
  "type": "object",
  "properties": {
    "version": {"type": "string"},
    "environments": {"type": "array"},
    "rules": {"type": "array"}
  },
  "required": ["version", "environments"]
}
""";

var result = schemaValidator.Validate(jsonContent, schemaContent);

if (!result.IsValid)
{
    Console.WriteLine("Schema validation failed:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
else
{
    Console.WriteLine("Schema validation passed!");
}

// Example 2: Validate against a schema file
var fileResult = schemaValidator.ValidateAgainstFile(jsonContent, "sguard.schema.json");
if (!fileResult.IsValid)
{
    foreach (var error in fileResult.Errors)
    {
        Console.WriteLine($"Schema validation error: {error}");
    }
}
```

### Custom Validator Plugins

Create custom validators by implementing `IValidatorPlugin`:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Validators;
using SGuard.ConfigValidation.Validators.Plugin;
using System.Text.RegularExpressions;

// Step 1: Create a custom validator
public class EmailValidator : IValidator<object>
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public string ValidatorType => "email";

    public ValidationResult Validate(object value, ValidatorCondition condition)
    {
        if (value == null)
        {
            return ValidationResult.Failure(condition.Message ?? "Email is required");
        }

        var email = value.ToString();
        if (string.IsNullOrWhiteSpace(email))
        {
            return ValidationResult.Failure(condition.Message ?? "Email cannot be empty");
        }

        if (!EmailRegex.IsMatch(email))
        {
            return ValidationResult.Failure(
                condition.Message ?? $"Invalid email format. Actual value: '{email}'");
        }

        return ValidationResult.Success();
    }
}

// Step 2: Create a plugin wrapper
public class EmailValidatorPlugin : IValidatorPlugin
{
    public string ValidatorType => "email";
    
    public IValidator<object> Validator => new EmailValidator();
}

// Step 3: Discover and use plugins
var logger = NullLogger<ValidatorPluginDiscovery>.Instance;
var pluginDiscovery = new ValidatorPluginDiscovery(logger);
var plugins = pluginDiscovery.DiscoverValidators(new[] { "./plugins" });

// Step 4: Register plugins with ValidatorFactory
var validatorFactory = new ValidatorFactory(logger);
foreach (var plugin in plugins)
{
    validatorFactory.RegisterValidator(plugin.ValidatorType, plugin.Validator);
}
```

### Error Handling

Handle validation errors gracefully:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Exceptions;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Validators;

var securityOptions = Options.Create(new SecurityOptions());
var logger = NullLogger<RuleEngine>.Instance;
var validatorFactory = new ValidatorFactory(NullLogger<ValidatorFactory>.Instance);
var configLoader = new ConfigLoader(NullLogger<ConfigLoader>.Instance, securityOptions);
var fileValidator = new FileValidator(validatorFactory, NullLogger<FileValidator>.Instance);
var ruleEngine = new RuleEngine(
    configLoader, 
    fileValidator, 
    validatorFactory, 
    logger, 
    securityOptions);

try
{
    var result = ruleEngine.ValidateEnvironment("sguard.json", "prod");
    
    if (result.IsSuccess)
    {
        if (result.HasValidationErrors)
        {
            Console.WriteLine("Validation completed with errors:");
            foreach (var validationResult in result.ValidationResults)
            {
                Console.WriteLine($"File: {validationResult.Path}");
                foreach (var error in validationResult.Errors)
                {
                    Console.WriteLine($"  Key: {error.Key}");
                    Console.WriteLine($"  Error: {error.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine("All validations passed!");
        }
    }
    else
    {
        Console.WriteLine($"Validation failed: {result.ErrorMessage}");
    }
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"Configuration file not found: {ex.FileName}");
}
catch (ConfigurationException ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

### Advanced Usage: Validating Multiple Environments

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Validators;

var securityOptions = Options.Create(new SecurityOptions());
var logger = NullLogger<RuleEngine>.Instance;
var validatorFactory = new ValidatorFactory(NullLogger<ValidatorFactory>.Instance);
var configLoader = new ConfigLoader(NullLogger<ConfigLoader>.Instance, securityOptions);
var fileValidator = new FileValidator(validatorFactory, NullLogger<FileValidator>.Instance);
var ruleEngine = new RuleEngine(
    configLoader, 
    fileValidator, 
    validatorFactory, 
    logger, 
    securityOptions);

// Validate all environments
var allResults = ruleEngine.ValidateAllEnvironments("sguard.json");

var successCount = allResults.ValidationResults.Count(r => r.IsValid);
var failureCount = allResults.ValidationResults.Count - successCount;

Console.WriteLine($"Validated {allResults.ValidationResults.Count} environment(s)");
Console.WriteLine($"  ✅ Passed: {successCount}");
Console.WriteLine($"  ❌ Failed: {failureCount}");

// Validate specific environments
var environmentsToValidate = new[] { "dev", "stag", "prod" };
foreach (var envId in environmentsToValidate)
{
    var result = ruleEngine.ValidateEnvironment("sguard.json", envId);
    Console.WriteLine($"Environment '{envId}': {(result.IsSuccess && !result.HasValidationErrors ? "✅ Pass" : "❌ Fail")}");
}
```

### CI/CD Integration Example

Example GitHub Actions workflow:

```yaml
name: Validate Configuration

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  validate-config:
    runs-on: ubuntu-latest
    env:
      WEBHOOK_TOKEN: ${{ secrets.WEBHOOK_TOKEN }}
      SLACK_WEBHOOK_URL: ${{ secrets.SLACK_WEBHOOK_URL }}
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Validate configuration
        run: |
          dotnet run --project SGuard.ConfigChecker.Console -- validate --all --output json > validation-results.json
          # Hooks configured in sguard.json will execute automatically after validation
          # (e.g., Slack notifications, webhooks, deployment scripts)
      
      - name: Check validation results
        run: |
          if grep -q '"hasValidationErrors":true' validation-results.json; then
            echo "❌ Configuration validation failed!"
            cat validation-results.json
            exit 1
          else
            echo "✅ All configurations are valid!"
          fi
```

Example Azure DevOps pipeline:

```yaml
trigger:
  branches:
    include:
      - main
      - develop

pool:
  vmImage: 'ubuntu-latest'

variables:
  - name: WEBHOOK_TOKEN
    value: $(WEBHOOK_TOKEN)
  - name: SLACK_WEBHOOK_URL
    value: $(SLACK_WEBHOOK_URL)

steps:
  - task: UseDotNet@2
    inputs:
      packageType: 'sdk'
      version: '8.0.x'
  
  - task: DotNetCoreCLI@2
    displayName: 'Restore dependencies'
    inputs:
      command: 'restore'
  
  - task: DotNetCoreCLI@2
    displayName: 'Validate configuration'
    inputs:
      command: 'run'
      projects: 'SGuard.ConfigChecker.Console/SGuard.ConfigChecker.Console.csproj'
      arguments: '-- validate --all --output json'
    continueOnError: false
    env:
      WEBHOOK_TOKEN: $(WEBHOOK_TOKEN)
      SLACK_WEBHOOK_URL: $(SLACK_WEBHOOK_URL)
    # Hooks configured in sguard.json will execute automatically after validation
    # (e.g., Slack notifications, webhooks, deployment scripts)
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

### JSON Output (Console)

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

### File Output

You can write validation results to a file in either text or JSON format:

#### Text File Output

```csharp
using SGuard.ConfigValidation.Output;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

// Write text output to file
var textFormatter = OutputFormatterFactory.Create("text", loggerFactory, "validation-results.txt");
await textFormatter.FormatAsync(result);
```

#### JSON File Output

```csharp
using SGuard.ConfigValidation.Output;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

// Write JSON output to file
var jsonFormatter = OutputFormatterFactory.Create("json", loggerFactory, "validation-results.json");
await jsonFormatter.FormatAsync(result);
```

#### Direct Formatter Usage

You can also use the formatters directly:

```csharp
using SGuard.ConfigValidation.Output;

// Text file formatter
var fileFormatter = new FileOutputFormatter("results.txt");
await fileFormatter.FormatAsync(result);

// JSON file formatter
var jsonFileFormatter = new JsonFileOutputFormatter("results.json");
await jsonFileFormatter.FormatAsync(result);
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

## 🎣 Post-Validation Hooks

SGuard.ConfigChecker supports post-validation hooks that execute automatically after validation completes. Hooks are executed asynchronously and non-blocking - failures do not affect validation results or exit codes.

### Overview

Hooks allow you to:
- Execute scripts (bash, PowerShell, etc.) after validation
- Send HTTP webhook requests to external services
- Trigger custom actions based on validation results

Hooks are configured in your `sguard.json` file and support:
- **Global hooks**: Run for all environments
- **Environment-specific hooks**: Run only for specific environments (higher priority)
- **Template variables**: Dynamic values like `{{status}}`, `{{environment}}`, `{{errorCount}}`, etc.

### Hook Types

#### 1. Script Hook

Execute scripts or commands after validation.

**Configuration:**
```json
{
  "type": "script",
  "command": "./scripts/deploy.sh",
  "arguments": ["--env", "{{environment}}", "--version", "1.0"],
  "workingDirectory": "/app/scripts",
  "environmentVariables": {
    "DEPLOY_ENV": "{{environment}}",
    "API_KEY": "${API_KEY}"
  },
  "timeout": 30000
}
```

**Parameters:**
- `command` (string, **required**): Script command or path to execute
- `arguments` (array, **optional**): Arguments to pass to the script. Supports template variables.
- `workingDirectory` (string, **optional**): Working directory for script execution
- `environmentVariables` (object, **optional**): Environment variables to set. Supports template variables and `${ENV_VAR}` syntax for system environment variables.
- `timeout` (number, **optional**): Timeout in milliseconds (default: 30000)

#### 2. Webhook Hook

Send HTTP requests to external webhooks.

**Configuration:**
```json
{
  "type": "webhook",
  "url": "https://api.example.com/webhook",
  "method": "POST",
  "headers": {
    "Authorization": "Bearer ${WEBHOOK_TOKEN}",
    "Content-Type": "application/json",
    "X-Custom-Header": "value"
  },
  "body": {
    "status": "{{status}}",
    "environment": "{{environment}}",
    "errorCount": "{{errorCount}}",
    "errors": "{{errors}}"
  },
  "timeout": 10000
}
```

**Parameters:**
- `url` (string, **required**): Webhook URL. Supports template variables.
- `method` (string, **optional**): HTTP method (default: "POST")
- `headers` (object, **optional**): HTTP headers. Supports template variables and `${ENV_VAR}` syntax.
- `body` (object, **optional**): Request body. Can be a JSON object or template string. Supports template variables.
- `timeout` (number, **optional**): Timeout in milliseconds (default: 10000)

### Template Variables

Hooks support template variables that are resolved at runtime based on validation results:

- `{{status}}` - "success" or "failure"
- `{{environment}}` - Environment ID (or "all" if validating all environments)
- `{{errorCount}}` - Number of validation errors
- `{{errors}}` - JSON array of error details
- `{{results}}` - JSON array of validation results
- `{{statusColor}}` - "good" (green) or "danger" (red)

**Example:**
```json
{
  "type": "webhook",
  "url": "https://api.example.com/webhook",
  "body": {
    "message": "Validation {{status}} for {{environment}}",
    "errors": "{{errors}}",
    "errorCount": "{{errorCount}}"
  }
}
```

### Environment-Specific Hooks

Environment-specific hooks take priority over global hooks. If both are configured, environment-specific hooks run first.

**Example:**
```json
{
  "hooks": {
    "global": {
      "onSuccess": [
        {
          "type": "webhook",
          "url": "https://api.example.com/webhook/global"
        }
      ]
    },
    "environments": {
      "prod": {
        "onSuccess": [
          {
            "type": "script",
            "command": "./scripts/deploy-prod.sh"
          }
        ]
      }
    }
  }
}
```

In this example, when validating the "prod" environment:
1. First, the production-specific script hook runs
2. Then, the global webhook runs

### Hook Execution Flow

```
Validation Complete
  ↓
Determine Exit Code (Success/Failure)
  ↓
Load Hooks Configuration
  ↓
Execute Environment-Specific Hooks (if applicable)
  ↓
Execute Global Hooks
  ↓
Return Exit Code (hooks don't affect exit code)
```

### Error Handling

- **Hook failures are non-blocking**: If a hook fails, it is logged but does not affect validation results or exit codes
- **Timeout handling**: Scripts and webhooks respect timeout settings. If a timeout occurs, the hook is logged as failed but validation continues
- **Parallel execution**: Multiple hooks execute in parallel for better performance

### Best Practices

1. **Use environment variables for secrets**: Use `${ENV_VAR}` syntax for sensitive values like API keys
2. **Set appropriate timeouts**: Script hooks default to 30 seconds, webhooks to 10 seconds. Adjust based on your needs
3. **Test hooks separately**: Test your hooks independently before integrating them into validation
4. **Monitor hook execution**: Check logs to ensure hooks are executing correctly
5. **Use template variables**: Leverage template variables for dynamic content instead of hardcoding values

### Custom Hooks

You can create custom hooks by implementing the `IHook` interface:

```csharp
using SGuard.ConfigValidation.Hooks;

public class MyCustomHook : IHook
{
    public async Task ExecuteAsync(HookContext context, CancellationToken cancellationToken = default)
    {
        // Your custom logic here
        var status = context.TemplateResolver.GetVariable("status");
        // ...
    }
}
```

Register your custom hook in `HookFactory` or use the factory pattern to create hooks from configuration.

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

## 🔌 Using as a Library (DLL)

The `SGuard.ConfigValidation` project is a standalone library that can be used in any .NET application without requiring the console application.

### Installation

#### NuGet Package (Recommended)
```xml
<PackageReference Include="SGuard.ConfigValidation" Version="0.0.1" />
```

#### Project Reference
```xml
<ProjectReference Include="path/to/SGuard.ConfigValidation/SGuard.ConfigValidation.csproj" />
```

### Quick Start

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

var result = ruleEngine.ValidateEnvironment("sguard.json", "prod");
```

### Use Cases

#### 1. Web API Integration

Validate configurations during application startup:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SGuard.ConfigValidation.Extensions;
using SGuard.ConfigValidation.Output;
using SGuard.ConfigValidation.Services.Abstract;

var builder = WebApplication.CreateBuilder(args);

// Register SGuard services
builder.Services.AddLogging();
builder.Services.AddSGuardConfigValidation(builder.Configuration);

var app = builder.Build();

// Validate configurations at startup
var ruleEngine = app.Services.GetRequiredService<IRuleEngine>();
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
var result = ruleEngine.ValidateAllEnvironments("sguard.json");

if (!result.IsSuccess || result.HasValidationErrors)
{
    app.Logger.LogError("Configuration validation failed");
    
    // Optionally write results to file for debugging
    var formatter = OutputFormatterFactory.Create("json", loggerFactory, "validation-errors.json");
    await formatter.FormatAsync(result);
    
    // Handle error appropriately
}

app.Run();
```

#### 2. Worker Service Integration

Validate configurations in a background worker:

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Extensions;
using SGuard.ConfigValidation.Output;
using SGuard.ConfigValidation.Services.Abstract;

var builder = Host.CreateApplicationBuilder(args);

// Register SGuard services
builder.Services.AddSGuardConfigValidation(builder.Configuration);

// Register worker
builder.Services.AddHostedService<ConfigValidationWorker>();

var host = builder.Build();
host.Run();

public class ConfigValidationWorker : BackgroundService
{
    private readonly IRuleEngine _ruleEngine;
    private readonly ILogger<ConfigValidationWorker> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public ConfigValidationWorker(
        IRuleEngine ruleEngine, 
        ILogger<ConfigValidationWorker> logger,
        ILoggerFactory loggerFactory)
    {
        _ruleEngine = ruleEngine;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var result = _ruleEngine.ValidateAllEnvironments("sguard.json");
            
            if (result.IsSuccess && !result.HasValidationErrors)
            {
                _logger.LogInformation("All configurations are valid");
            }
            else
            {
                _logger.LogError("Configuration validation failed");
                
                // Write validation results to file for analysis
                var formatter = OutputFormatterFactory.Create("json", _loggerFactory, 
                    $"validation-results-{DateTime.UtcNow:yyyyMMddHHmmss}.json");
                await formatter.FormatAsync(result);
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

#### 3. Unit Testing

Use the library in your test projects:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SGuard.ConfigValidation.Common;
using SGuard.ConfigValidation.Services;
using SGuard.ConfigValidation.Validators;
using Xunit;

public class MyServiceTests
{
    [Fact]
    public void TestConfigurationValidation()
    {
        var securityOptions = Options.Create(new SecurityOptions());
        var logger = NullLogger<RuleEngine>.Instance;
        var validatorFactory = new ValidatorFactory(NullLogger<ValidatorFactory>.Instance);
        var configLoader = new ConfigLoader(NullLogger<ConfigLoader>.Instance, securityOptions);
        var fileValidator = new FileValidator(validatorFactory, NullLogger<FileValidator>.Instance);
        var ruleEngine = new RuleEngine(configLoader, fileValidator, validatorFactory, logger, securityOptions);

        var result = ruleEngine.ValidateEnvironment("test-sguard.json", "test-env");
        
        Assert.True(result.IsSuccess);
    }
}
```

#### 4. Custom Console Application

Create your own console application using the library:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SGuard.ConfigValidation.Extensions;
using SGuard.ConfigValidation.Output;
using SGuard.ConfigValidation.Services.Abstract;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddSGuardConfigValidation();

var serviceProvider = services.BuildServiceProvider();
var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

// Your custom logic here
var result = ruleEngine.ValidateAllEnvironments("sguard.json");

if (result.IsSuccess && !result.HasValidationErrors)
{
    Console.WriteLine("✅ All configurations are valid!");
    
    // Optionally save results to file
    var formatter = OutputFormatterFactory.Create("json", loggerFactory, "validation-success.json");
    await formatter.FormatAsync(result);
    
    Environment.Exit(0);
}
else
{
    Console.WriteLine("❌ Configuration validation failed!");
    
    // Save error details to file
    var formatter = OutputFormatterFactory.Create("json", loggerFactory, "validation-errors.json");
    await formatter.FormatAsync(result);
    
    Environment.Exit(1);
}
```

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

For questions or contributions, feel free to open an issue or pull request!
