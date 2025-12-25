# SGuard.ConfigChecker

A lightweight tool to catch critical configuration issues **before runtime**.

## ✨ Why?
Misconfigured environments, missing connection strings, or wrong URLs can cause major issues after deployment.  
**SGuard.ConfigChecker** helps you detect these problems **early**, during application startup or in your CI/CD pipeline.

## 🚀 Supported Features (v0.1 - Phase 1)
- **required** → Ensures a specific key must exist in the target config file.  
  Example: `ConnectionStrings:Default` must be defined in production.

## 🛠️ Phase 1 Roadmap
- [x] JSON config support (`checker.config.json`)
- [x] "required" rule implementation
- [ ] CLI tool for running checks
- [ ] Basic error reporting (missing keys)
- [ ] Environment-based validation (e.g., Production, Development)
- [ ] Sample config and usage documentation

## 📂 Example config
`checker.config.json`:
```json
{
  "version": "0.1",
  "environments": [
    { "name": "Production", "files": ["samples/appsettings.Production.json"] }
  ],
  "rules": [
    {
      "id": "req-conn",
      "when": { "env": ["Production"] },
      "assert": { "kind": "required", "key": "ConnectionStrings:Default" }
    }
  ]
}
```

## 🗺️ Future Plans
- YAML config support
- Advanced rule types (e.g., value checks, regex, range)
- Integration with CI/CD pipelines
- Custom error messages
- Multi-file and multi-environment support

## 📖 Usage
1. Define your rules in `checker.config.json`.
2. Run the checker tool (CLI coming soon).
3. Review the output for missing or misconfigured keys.

---
For questions or contributions, feel free to open an issue or pull request!
