---
outline: deep
---

# Enum Value Handling

> 📝 **Note**: Database engines treat enum values starting from 1, while C# enums start from 0. The `[Description]` attribute maps C# enum values to their database string representations.

## NONE as First Value
```csharp
public enum EnumExample {
    NONE,
    [Description("Y")]
    Y,
    [Description("NEGATIVE")]
    N
}
```

## Explicit First Value
```csharp
public enum EnumExample {
    [Description("Y")]
    Y = 1,
    [Description("NEGATIVE")]
    N
}
```

## Engine-Specific

### PostgreSQL

For PostgreSQL enums, the attribute `[PgName]` should be used.

```csharp
public enum EnumExample {
    [PgName("NONE")]
    NONE,
    [Description("Y")]
    [PgName("Y")]
    Y,
    [Description("NEGATIVE")]
    [PgName("NEGATIVE")]
    N
}
```
