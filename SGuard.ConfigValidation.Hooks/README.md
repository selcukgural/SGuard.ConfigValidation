# SGuard.ConfigValidation.Hooks

Post-validation hooks extension for SGuard.ConfigValidation. Execute scripts, send webhooks, or send notifications (Slack, Teams) after validation completes.

## Version Compatibility

- **Hook Package Version**: `2.0.0`
- **Required Core Package**: `SGuard.ConfigValidation >= 0.0.1`
- **Target Frameworks**: .NET 8.0, 9.0, 10.0

## Installation

### NuGet Package

```bash
dotnet add package SGuard.ConfigValidation.Hooks
```

### Project Reference

```xml
<ItemGroup>
  <ProjectReference Include="path/to/SGuard.ConfigValidation.csproj" />
</ItemGroup>
```

## Quick Start

### 1. Install Package

```bash
dotnet add package SGuard.ConfigValidation.Hooks
```

### 2. Register Services

```csharp
using Microsoft.Extensions.DependencyInjection;
using SGuard.ConfigValidation.Hooks;

var services = new ServiceCollection();
services.AddLogging();
services.AddSGuardHooks(); // Register hook services
```

### 3. Use Hook Executor

```csharp
using SGuard.ConfigValidation.Hooks;
using SGuard.ConfigValidation.Models;
using SGuard.ConfigValidation.Results;

var hookExecutor = serviceProvider.GetRequiredService<HookExecutor>();
var result = ruleEngine.ValidateEnvironment("sguard.json", "dev");
var config = configLoader.LoadConfig("sguard.json");

await hookExecutor.ExecuteHooksAsync(result, config, "dev");
```

## Features

- **Script Hooks**: Execute scripts (bash, PowerShell, etc.) after validation
- **Webhook Hooks**: Send HTTP requests to external services
- **Template Variables**: Dynamic values like `{{status}}`, `{{environment}}`, `{{errorCount}}`
- **Environment-Specific Hooks**: Different hooks for different environments
- **Non-Blocking**: Hook failures don't affect validation results

## Hook Types

### Script Hook

Execute scripts or commands after validation.

```json
{
  "type": "script",
  "command": "./scripts/deploy.sh",
  "arguments": ["--env", "{{environment}}"],
  "workingDirectory": "/app/scripts",
  "environmentVariables": {
    "DEPLOY_ENV": "{{environment}}"
  },
  "timeout": 30000
}
```

### Webhook Hook

Send HTTP requests to external webhooks.

```json
{
  "type": "webhook",
  "url": "https://api.example.com/webhook",
  "method": "POST",
  "headers": {
    "Authorization": "Bearer ${WEBHOOK_TOKEN}"
  },
  "body": {
    "status": "{{status}}",
    "environment": "{{environment}}",
    "errors": "{{errors}}"
  },
  "timeout": 10000
}
```

## Template Variables

Hooks support template variables that are resolved at runtime:

- `{{status}}` - "success" or "failure"
- `{{environment}}` - Environment ID (or "all" if validating all environments)
- `{{errorCount}}` - Number of validation errors
- `{{errors}}` - JSON array of error details
- `{{results}}` - JSON array of validation results
- `{{statusColor}}` - "good" (green) or "danger" (red)

## Configuration

Hooks are configured in your `sguard.json` file:

```json
{
  "version": "1",
  "environments": [...],
  "rules": [...],
  "hooks": {
    "global": {
      "onSuccess": [
        {
          "type": "webhook",
          "url": "https://api.example.com/webhook/success"
        }
      ],
      "onFailure": [
        {
          "type": "notification",
          "provider": "slack",
          "webhookUrl": "https://hooks.slack.com/services/...",
          "message": "Validation failed for {{environment}}"
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

## Extension Methods

### AddSGuardHooks()

Registers hook services in the dependency injection container.

```csharp
services.AddSGuardHooks();
```

This registers:
- `HookFactory` - Creates hook instances from configurations
- `HookExecutor` - Executes hooks based on validation results

## Custom Hooks

You can create custom hooks by implementing the `IHook` interface:

```csharp
using SGuard.ConfigValidation.Hooks;

public class MyCustomHook : IHook
{
    public async Task ExecuteAsync(HookContext context, CancellationToken cancellationToken = default)
    {
        var status = context.TemplateResolver.GetVariable("status");
        // Your custom logic here
    }
}
```

Register your custom hook in `HookFactory` or use the factory pattern to create hooks from configuration.

## Error Handling

- **Hook failures are non-blocking**: If a hook fails, it is logged but does not affect validation results or exit codes
- **Timeout handling**: Scripts and webhooks respect timeout settings
- **Parallel execution**: Multiple hooks execute in parallel for better performance

## Dependencies

- `SGuard.ConfigValidation` (>= 0.0.1) - Core validation library
- `Microsoft.Extensions.Logging.Abstractions` (8.0.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` (8.0.0)

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please see the main project repository for contribution guidelines.

