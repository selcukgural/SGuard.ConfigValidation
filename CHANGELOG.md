# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Async/await support for JSON schema validation (`ValidateAsync`, `ValidateAgainstFileAsync`)
- CI/CD pipeline with GitHub Actions
- Code coverage reporting with Coverlet

### Changed
- `ISchemaValidator.ValidateAgainstFile` is now async (`ValidateAgainstFileAsync`)
- `IConfigLoader.LoadConfig` now has async version (`LoadConfigAsync`)
- Repository URLs updated (placeholder replaced)

### Fixed
- Fixed async/await pattern violation in `JsonSchemaValidator` (replaced `.GetAwaiter().GetResult()` with proper async/await)

## [0.0.1] - 2024-01-01

### Added
- Initial release
- Configuration validation support for JSON and YAML files
- Built-in validators (required, min_len, max_len, eq, ne, gt, gte, lt, lte, in)
- JSON Schema validation support
- Custom validator plugin support
- Post-validation hooks (Script, Webhook)
- Security features (path traversal protection, DoS protection, symlink validation)
- Multiple output formats (Console, JSON, File)
- Logging level configuration support
- Dependency Injection support with extension methods

[Unreleased]: https://github.com/sguard/SGuard.ConfigChecker/compare/v0.0.1...HEAD
[0.0.1]: https://github.com/sguard/SGuard.ConfigChecker/releases/tag/v0.0.1

