---
outline: deep
---

# Database-Specific Operations

## PostgreSQL Enum Types
PostgreSQL requires special configuration for enum types:

```csharp
// Custom PostgreSQL constructor with enum mapping
ConnectorManager dbConnector = new ConnectorManager(
    new NpgsqlDataSourceBuilder(
        "Host=localhost;Port=5432;Database=unleasharp;Username=unleasharp;Password=unleasharp;"
    ))

    // Map C# Enum to PostgreSQL Enum type
    .WithMappedEnum<EnumExample>()
;

// Create the PostgreSQL Enum type based on the C# Enum type if it doesn't exist already
dbConnector.QueryBuilder().Build(query => query.CreateEnumType<EnumExample>()).Execute();
```
