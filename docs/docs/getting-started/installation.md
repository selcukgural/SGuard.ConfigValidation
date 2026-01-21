---
sidebar_position: 1
---

# Installation

Learn how to install SGuard.ConfigValidation in your .NET project.

## Prerequisites

- .NET 8.0, 9.0, or 10.0 SDK
- A .NET project (Console, Web API, etc.)

## Install via NuGet

### Using .NET CLI

```bash
dotnet add package SGuard.ConfigValidation
```

### Using Package Manager Console

```powershell
Install-Package SGuard.ConfigValidation
```

### Using Visual Studio

1. Right-click on your project in Solution Explorer
2. Select **Manage NuGet Packages**
3. Search for `SGuard.ConfigValidation`
4. Click **Install**

## Verify Installation

After installation, verify the package is added to your project file:

```xml title="YourProject.csproj"
<ItemGroup>
  <PackageReference Include="SGuard.ConfigValidation" Version="0.1.0" />
</ItemGroup>
```

## Next Steps

Now that you've installed the package, proceed to:
- [**Quick Start**](./quick-start) - Create your first validation
- [**Configuration**](./configuration) - Learn about sguard.json structure
