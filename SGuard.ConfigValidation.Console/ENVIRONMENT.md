# Environment Management

## Environment Detection

SGuard.ConfigValidation.Console automatically detects the environment according to .NET standards.

## Environment Variable Priority

1. **DOTNET_ENVIRONMENT** (Priority - .NET 6+ standard)
2. **ASPNETCORE_ENVIRONMENT** (For backward compatibility)
3. **Production** (Default, if none is set)

## Usage

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

## IHostEnvironment Usage

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

## Environment Helper Methods

The `IHostEnvironment` interface provides the following helper methods:

- `IsDevelopment()` - Development environment check
- `IsStaging()` - Staging environment check
- `IsProduction()` - Production environment check
- `IsEnvironment(string name)` - Custom environment check

## Example Scenarios

### Development Environment
```bash
export DOTNET_ENVIRONMENT=Development
./ConsoleApp1
```
- `appsettings.json` is loaded
- `appsettings.Development.json` is loaded (if exists)
- Debug level logging is active

### Staging Environment
```bash
export DOTNET_ENVIRONMENT=Staging
./ConsoleApp1
```
- `appsettings.json` is loaded
- `appsettings.Staging.json` is loaded (if exists)
- Information level logging is active

### Production Environment
```bash
export DOTNET_ENVIRONMENT=Production
# or set nothing (defaults to Production)
./ConsoleApp1
```
- `appsettings.json` is loaded
- `appsettings.Production.json` is loaded (if exists)
- Warning level logging is active

## Docker/Kubernetes Usage

### Dockerfile
```dockerfile
ENV DOTNET_ENVIRONMENT=Production
```

### Kubernetes Deployment
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

## Environment Usage in Program.cs

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

## Best Practices

1. **Use DOTNET_ENVIRONMENT** - .NET 6+ standard
2. **Default to Production** - For security
3. **Make environment-specific files optional** - Base config should always be loaded
4. **Inject IHostEnvironment** - For environment control
5. **Keep sensitive data in environment variables** - Don't write to appsettings.json
