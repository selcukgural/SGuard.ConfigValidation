# SGuard Logging Konfigürasyonu

## Log Level Yönetimi

SGuard logger'ları appsettings.json dosyasından yönetilir. .NET'in logging sistemi namespace bazlı log level kontrolü sağlar.

## Namespace Hiyerarşisi

Log level'lar şu sırayla kontrol edilir (en spesifikten en genele):

1. **Spesifik Namespace** (örn: `SGuard.ConfigValidation.Services.ConfigLoader`)
2. **Namespace Segment** (örn: `SGuard.ConfigValidation.Services`)
3. **Root Namespace** (örn: `SGuard`)
4. **Default** (tüm namespace'ler için varsayılan)

## appsettings.json Örneği

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SGuard": "Information",                    // Tüm SGuard namespace'leri için
      "SGuard.ConfigValidation.Services": "Debug", // Sadece Services için
      "SGuard.ConfigValidation.Validators": "Warning", // Sadece Validators için
      "ConsoleApp1": "Information"                // CLI için
    }
  }
}
```

## Log Level Değerleri

- **Trace** (0): En detaylı loglar, genellikle development'ta kullanılır
- **Debug** (1): Debug bilgileri, development ve troubleshooting için
- **Information** (2): Genel bilgilendirme mesajları (varsayılan)
- **Warning** (3): Uyarı mesajları, hata olmayan ama dikkat gerektiren durumlar
- **Error** (4): Hata mesajları, işlem başarısız olduğunda
- **Critical** (5): Kritik hatalar, uygulama çökmesi riski olan durumlar
- **None** (6): Loglama kapalı

## Kullanım Örnekleri

### 1. Tüm SGuard Loglarını Debug Yapmak

```json
{
  "Logging": {
    "LogLevel": {
      "SGuard": "Debug"
    }
  }
}
```

### 2. Sadece Services Katmanını Debug Yapmak

```json
{
  "Logging": {
    "LogLevel": {
      "SGuard": "Information",
      "SGuard.ConfigValidation.Services": "Debug"
    }
  }
}
```

### 3. Validators'ı Sessiz Yapmak (Sadece Warning ve Üzeri)

```json
{
  "Logging": {
    "LogLevel": {
      "SGuard": "Information",
      "SGuard.ConfigValidation.Validators": "Warning"
    }
  }
}
```

### 4. Production Ortamı İçin Minimal Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "SGuard": "Warning",
      "ConsoleApp1": "Error"
    }
  }
}
```

## Environment Variables ile Override

Log level'ları environment variables ile de override edebilirsiniz:

```bash
# Linux/Mac
export Logging__LogLevel__SGuard=Debug

# Windows
set Logging__LogLevel__SGuard=Debug
```

## Kod İçinde Log Kullanımı

```csharp
public class ConfigLoader
{
    private readonly ILogger<ConfigLoader> _logger;
    
    public ConfigLoader(ILogger<ConfigLoader> logger)
    {
        _logger = logger;
    }
    
    public void LoadConfig(string path)
    {
        // Log level kontrolü otomatik yapılır
        _logger.LogTrace("Trace: En detaylı bilgi");
        _logger.LogDebug("Debug: Debug bilgisi");
        _logger.LogInformation("Information: Genel bilgi");
        _logger.LogWarning("Warning: Uyarı");
        _logger.LogError("Error: Hata");
        _logger.LogCritical("Critical: Kritik hata");
    }
}
```

## Log Level Kontrolü Nasıl Çalışır?

1. Logger oluşturulurken namespace otomatik belirlenir: `ILogger<ConfigLoader>` → `SGuard.ConfigValidation.Services.ConfigLoader`
2. Log mesajı yazılmadan önce appsettings.json'dan log level kontrol edilir
3. Eğer mesajın log level'ı, konfigürasyondaki minimum level'dan düşükse log yazılmaz
4. Örnek: `LogLevel.Debug` mesajı, konfigürasyonda `Information` varsa yazılmaz

## Performans Notu

Log level kontrolü çok hızlıdır ve performans etkisi minimaldir. Ancak string interpolation kullanırken dikkatli olun:

```csharp
// ❌ Kötü: Her zaman string oluşturulur
_logger.LogDebug($"Processing {largeObject}");

// ✅ İyi: Sadece Debug aktifse string oluşturulur
_logger.LogDebug("Processing {LargeObject}", largeObject);
```

## Önerilen Konfigürasyonlar

### Development
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "SGuard": "Debug"
    }
  }
}
```

### Staging
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SGuard": "Information"
    }
  }
}
```

### Production
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "SGuard": "Warning"
    }
  }
}
```

