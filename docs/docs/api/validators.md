---
sidebar_position: 1
---

# Validators

SGuard.ConfigValidation provides a comprehensive set of built-in validators.

## Available Validators

### required

Ensures a configuration value exists and is not null or empty.

```json
{
  "validator": "required",
  "message": "This field is required"
}
```

**Use Case:** Validate that critical configuration values are present.

---

### min_len

Validates that a string has a minimum length.

```json
{
  "validator": "min_len",
  "value": 10,
  "message": "Must be at least 10 characters"
}
```

**Parameter:** `value` (integer) - Minimum length

---

### max_len

Validates that a string does not exceed a maximum length.

```json
{
  "validator": "max_len",
  "value": 100,
  "message": "Must not exceed 100 characters"
}
```

**Parameter:** `value` (integer) - Maximum length

---

### eq (equals)

Validates that a value equals a specific value.

```json
{
  "validator": "eq",
  "value": "Production",
  "message": "Environment must be 'Production'"
}
```

**Parameter:** `value` (any) - Expected value

---

### ne (not equals)

Validates that a value does not equal a specific value.

```json
{
  "validator": "ne",
  "value": "localhost",
  "message": "Host cannot be 'localhost' in production"
}
```

**Parameter:** `value` (any) - Value to reject

---

### gt (greater than)

Validates that a numeric value is greater than a threshold.

```json
{
  "validator": "gt",
  "value": 0,
  "message": "Value must be greater than 0"
}
```

**Parameter:** `value` (number) - Threshold value

---

### gte (greater than or equal)

Validates that a numeric value is greater than or equal to a threshold.

```json
{
  "validator": "gte",
  "value": 1,
  "message": "Value must be at least 1"
}
```

**Parameter:** `value` (number) - Threshold value

---

### lt (less than)

Validates that a numeric value is less than a threshold.

```json
{
  "validator": "lt",
  "value": 100,
  "message": "Value must be less than 100"
}
```

**Parameter:** `value` (number) - Threshold value

---

### lte (less than or equal)

Validates that a numeric value is less than or equal to a threshold.

```json
{
  "validator": "lte",
  "value": 99,
  "message": "Value must be at most 99"
}
```

**Parameter:** `value` (number) - Threshold value

---

### in

Validates that a value is in a list of allowed values.

```json
{
  "validator": "in",
  "value": ["Development", "Staging", "Production"],
  "message": "Environment must be Development, Staging, or Production"
}
```

**Parameter:** `value` (array) - List of allowed values

---

## Combining Validators

You can apply multiple validators to a single key:

```json
{
  "key": "ApiSettings:ApiKey",
  "condition": [
    {
      "validator": "required",
      "message": "API Key is required"
    },
    {
      "validator": "min_len",
      "value": 32,
      "message": "API Key must be at least 32 characters"
    },
    {
      "validator": "max_len",
      "value": 64,
      "message": "API Key must not exceed 64 characters"
    }
  ]
}
```

## Common Validation Patterns

### Connection String Validation

```json
{
  "key": "ConnectionStrings:DefaultConnection",
  "condition": [
    {
      "validator": "required",
      "message": "Connection string is required"
    },
    {
      "validator": "min_len",
      "value": 20,
      "message": "Connection string appears invalid (too short)"
    }
  ]
}
```

### URL Validation

```json
{
  "key": "ApiSettings:BaseUrl",
  "condition": [
    {
      "validator": "required",
      "message": "Base URL is required"
    },
    {
      "validator": "ne",
      "value": "http://localhost",
      "message": "Cannot use localhost in production"
    }
  ]
}
```

### Numeric Range Validation

```json
{
  "key": "ApiSettings:Timeout",
  "condition": [
    {
      "validator": "gte",
      "value": 5,
      "message": "Timeout must be at least 5 seconds"
    },
    {
      "validator": "lte",
      "value": 300,
      "message": "Timeout must not exceed 300 seconds"
    }
  ]
}
```

## Next Steps

- [**Core API**](./core) - Learn about IRuleEngine and other core interfaces
