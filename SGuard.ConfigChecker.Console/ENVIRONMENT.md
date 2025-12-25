# Environment Yönetimi

## Environment Detection

SGuard.ConfigChecker, .NET standartlarına uygun olarak environment'ı otomatik olarak algılar.

## Environment Variable Önceliği

1. **DOTNET_ENVIRONMENT** (Öncelikli - .NET 6+ standardı)
2. **ASPNETCORE_ENVIRONMENT** (Geriye dönük uyumluluk için)
3. **Production** (Varsayılan, hiçbiri set edilmemişse)

## Kullanım

### Environment Variable Set Etme

#### Linux/macOS
```bash
export DOTNET_ENVIRONMENT=Development
# veya
export ASPNETCORE_ENVIRONMENT=Development
```

#### Windows (Command Prompt)
```cmd
set DOTNET_ENVIRONMENT=Development
# veya
set ASPNETCORE_ENVIRONMENT=Development
```

#### Windows (PowerShell)
```powershell
$env:DOTNET_ENVIRONMENT="Development"
# veya
$env:ASPNETCORE_ENVIRONMENT="Development"
```

### appsettings Dosyaları

Environment'e göre otomatik olarak yüklenir:

- `appsettings.json` - Her zaman yüklenir (base configuration)
- `appsettings.Development.json` - Development environment'ında yüklenir
- `appsettings.Staging.json` - Staging environment'ında yüklenir
- `appsettings.Production.json` - Production environment'ında yüklenir

**Not:** Environment-specific dosyalar optional'dır. Eğer dosya yoksa sadece base `appsettings.json` kullanılır.

## IHostEnvironment Kullanımı

Services içerisinde `IHostEnvironment` inject edilerek environment kontrolü yapılabilir:

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

`IHostEnvironment` interface'i şu helper method'ları sağlar:

- `IsDevelopment()` - Development environment kontrolü
- `IsStaging()` - Staging environment kontrolü
- `IsProduction()` - Production environment kontrolü
- `IsEnvironment(string name)` - Custom environment kontrolü

## Örnek Senaryolar

### Development Ortamı
```bash
export DOTNET_ENVIRONMENT=Development
./ConsoleApp1
```
- `appsettings.json` yüklenir
- `appsettings.Development.json` yüklenir (varsa)
- Debug level logging aktif olur

### Staging Ortamı
```bash
export DOTNET_ENVIRONMENT=Staging
./ConsoleApp1
```
- `appsettings.json` yüklenir
- `appsettings.Staging.json` yüklenir (varsa)
- Information level logging aktif olur

### Production Ortamı
```bash
export DOTNET_ENVIRONMENT=Production
# veya hiçbir şey set etme (varsayılan Production)
./ConsoleApp1
```
- `appsettings.json` yüklenir
- `appsettings.Production.json` yüklenir (varsa)
- Warning level logging aktif olur

## Docker/Kubernetes Kullanımı

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

## Program.cs İçinde Environment Kullanımı

```csharp
private static void ConfigureServices(IServiceCollection services)
{
    // Environment otomatik algılanır
    var environmentName = GetEnvironmentName(); // "Development", "Staging", "Production"
    
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
        .Build();
    
    // IHostEnvironment register edilir
    var hostEnvironment = new HostEnvironment
    {
        EnvironmentName = environmentName,
        // ...
    };
    services.AddSingleton<IHostEnvironment>(hostEnvironment);
}
```

## Best Practices

1. **DOTNET_ENVIRONMENT kullanın** - .NET 6+ standardı
2. **Production varsayılan olsun** - Güvenlik için
3. **Environment-specific dosyaları optional yapın** - Base config her zaman yüklensin
4. **IHostEnvironment inject edin** - Environment kontrolü için
5. **Sensitive data'yı environment variables'da tutun** - appsettings.json'a yazmayın

