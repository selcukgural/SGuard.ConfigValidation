---
sidebar_position: 3
---

# Configuration

Learn about the `sguard.json` configuration file structure.

## File Structure

```json
{
  "version": "1",
  "environments": [...],
  "rules": [...]
}
```

## Root Properties

### version

The schema version. Currently, only `"1"` is supported.

```json
{
  "version": "1"
}
```

### environments

An array of environment configurations to validate.

```json
{
  "environments": [
    {
      "id": "dev",
      "name": "Development",
      "path": "appsettings.Development.json"
    },
    {
      "id": "prod",
      "name": "Production",
      "path": "appsettings.Production.json"
    }
  ]
}
```

**Properties:**
- `id` (string, required): Unique identifier for the environment
- `name` (string, required): Human-readable name
- `path` (string, required): Path to the configuration file (JSON or YAML)

### rules

An array of validation rules to apply.

```json
{
  "rules": [
    {
      "id": "my-rule",
      "environments": ["prod"],
      "rule": {
        "id": "rule-definition",
        "conditions": [...]
      }
    }
  ]
}
```

**Properties:**
- `id` (string, required): Unique identifier for the rule
- `environments` (string[], required): Array of environment IDs to apply this rule
- `rule` (object, required): The rule definition

## Rule Definition

### conditions

An array of key-validator pairs:

```json
{
  "rule": {
    "id": "connection-rules",
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
            "message": "Connection string too short"
          }
        ]
      }
    ]
  }
}
```

**Properties:**
- `key` (string, required): Configuration key using colon notation (e.g., `"Section:SubSection:Key"`)
- `condition` (array, required): Array of validator objects

## Validator Object

Each validator has the following structure:

```json
{
  "validator": "required",
  "value": null,
  "message": "Custom error message"
}
```

**Properties:**
- `validator` (string, required): Validator name (see [Available Validators](../api/validators))
- `value` (any, optional): Validator parameter (depends on validator type)
- `message` (string, required): Error message displayed when validation fails

## Multiple Environments Example

```json
{
  "version": "1",
  "environments": [
    {
      "id": "dev",
      "name": "Development",
      "path": "appsettings.Development.json"
    },
    {
      "id": "staging",
      "name": "Staging",
      "path": "appsettings.Staging.json"
    },
    {
      "id": "prod",
      "name": "Production",
      "path": "appsettings.Production.json"
    }
  ],
  "rules": [
    {
      "id": "db-rule",
      "environments": ["staging", "prod"],
      "rule": {
        "id": "database-validation",
        "conditions": [
          {
            "key": "ConnectionStrings:DefaultConnection",
            "condition": [
              {
                "validator": "required",
                "message": "Connection string is required"
              }
            ]
          }
        ]
      }
    },
    {
      "id": "dev-only-rule",
      "environments": ["dev"],
      "rule": {
        "id": "dev-settings",
        "conditions": [
          {
            "key": "Debug:Enabled",
            "condition": [
              {
                "validator": "eq",
                "value": true,
                "message": "Debug must be enabled in development"
              }
            ]
          }
        ]
      }
    }
  ]
}
```

## YAML Configuration Files

SGuard.ConfigValidation also supports YAML files:

```json
{
  "environments": [
    {
      "id": "prod",
      "name": "Production",
      "path": "appsettings.Production.yaml"
    }
  ]
}
```

```yaml title="appsettings.Production.yaml"
ConnectionStrings:
  DefaultConnection: "Server=prod-db;Database=myapp"

ApiSettings:
  BaseUrl: "https://api.example.com"
  Timeout: 30
```

## Best Practices

1. **Environment-Specific Rules**: Apply strict validation to `prod` and `staging`
2. **Clear Messages**: Write descriptive error messages for quick debugging
3. **Key Notation**: Use colon-separated keys for nested configuration (e.g., `"Logging:LogLevel:Default"`)
4. **Version Control**: Always commit `sguard.json` to version control
5. **CI/CD Integration**: Run validation in your pipeline before deployment

## Next Steps

- [**Validators**](../api/validators) - Learn about all available validators
