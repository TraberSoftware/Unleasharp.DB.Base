---
outline: deep
---

# 🚀 Getting Started

## Namespace Imports

::: code-group
```csharp [MySQL]
// Main QueryBuilding Namespace - Required for advanced operations
using Unleasharp.DB.Base.QueryBuilding;
// Database engine Query Builder Namespace
using Unleasharp.DB.MySQL;
```

```csharp [SQLite]
// Main QueryBuilding Namespace - Required for advanced operations
using Unleasharp.DB.Base.QueryBuilding;
// Database engine Query Builder Namespace
using Unleasharp.DB.SQLite;
```

```csharp [PostgreSQL]
// Main QueryBuilding Namespace - Required for advanced operations
using Unleasharp.DB.Base.QueryBuilding;
// Database engine Query Builder Namespace
using Unleasharp.DB.PostgreSQL;
```

```csharp [MSSQL]
// Main QueryBuilding Namespace - Required for advanced operations
using Unleasharp.DB.Base.QueryBuilding;
// Database engine Query Builder Namespace
using Unleasharp.DB.MSSQL;
```

```csharp [DuckDB]
// Main QueryBuilding Namespace - Required for advanced operations
using Unleasharp.DB.Base.QueryBuilding;
// Database engine Query Builder Namespace
using Unleasharp.DB.DuckDB;
```
:::

> 📝 **Note**: The `QueryBuilding` namespace is only required for advanced operations. Basic functionality works without it.

## Connection Setup

There are multiple ways to initialize the ConnectorManager.

### Basic Setup

::: code-group
```csharp [MySQL]
ConnectorManager dbConnector = new ConnectorManager(
    "Server=localhost;Database=unleasharp;Uid=unleasharp;Pwd=unleasharp;"
);
```

```csharp [SQLite]
ConnectorManager dbConnector = new ConnectorManager(
    "Data Source=unleasharp.db;Version=3;"
);
```

```csharp [PostgreSQL]
ConnectorManager dbConnector = new ConnectorManager(
    "Host=localhost;Port=5432;Database=unleasharp;Username=unleasharp;Password=unleasharp;"
);
```

```csharp [MSSQL]
ConnectorManager dbConnector = new ConnectorManager(
    "Data Source=localhost;Database=unleasharp;User ID=unleasharp;Password=unleasharp;Integrated Security=false;Encrypt=True;TrustServerCertificate=True"
);
```

```csharp [DuckDB]
ConnectorManager dbConnector = new ConnectorManager(
    "Data Source=unleasharp.duckdb"
);
```
:::

### Using ConnectionStringBuilder

Each database implementation has its own `ConnectionStringBuilder`. You can provide an engine-specific `ConnectionStringBuilder` object to the `ConnectorManager` constructor.

::: code-group
```csharp [MySQL]
ConnectorManager dbConnector = new ConnectorManager(new MySqlConnectionStringBuilder(
    "Server=localhost;Database=unleasharp;Uid=unleasharp;Pwd=unleasharp;"
));
```

```csharp [SQLite]
ConnectorManager dbConnector = new ConnectorManager(new SQLiteConnectionStringBuilder(
    "Data Source=unleasharp.db;Version=3;"
));
```

```csharp [PostgreSQL]
ConnectorManager dbConnector = new ConnectorManager(new NpgsqlConnectionStringBuilder(
    "Host=localhost;Port=5432;Database=unleasharp;Username=unleasharp;Password=unleasharp;"
));
```

```csharp [MSSQL]
ConnectorManager dbConnector = new ConnectorManager(new SqlConnectionStringBuilder(
    "Data Source=localhost;Database=unleasharp;User ID=unleasharp;Password=unleasharp;Integrated Security=false;Encrypt=True;TrustServerCertificate=True"
));
```

```csharp [DuckDB]
// DuckDB does not allow to initialize a DuckDBConnectionStringBuilder with a Connection String
```
:::

### Fluent Configuration

The ConnectorManager follows the same Fluent principles as the `QueryBuilder` and the `Query` classes. 

```csharp
ConnectorManager dbConnector = new ConnectorManager("Data Source=unleasharp.db;Version=3;")
    // Automatic connection recycling, recommended for long-running applications (default: true)
    .WithAutomaticConnectionRenewal(true)
    
    // Set automatic connection renewal interval (default: 900 seconds)
    .WithAutomaticConnectionRenewalInterval(TimeSpan.FromHours(1))
    
    // Exception handling callback
    .WithOnQueryExceptionAction((query, ex) => {
        Console.WriteLine($"Exception executing query: {query.QueryRenderedString}");
        Console.WriteLine($"Exception message: {ex.Message}");
        // Log to your preferred logging framework
    })
    
    // Pre-execution callback
    .WithBeforeQueryExecutionAction((query) => {
        Console.WriteLine($"Preparing query for execution: {query.Render()}");
    })
    
    // Post-execution callback (even on exceptions)
    .WithAfterQueryExecutionAction((query) => {
        Console.WriteLine($"Executed query: {query.QueryRenderedString}");
    })
;
```

## Logging

Unleasharp.DB provides debug logging to trace the Query, QueryBuilder and ConnectorManager behaviour to detect possible bugs or query rendering errors.

The default log level is `ERROR` to avoid excessive logging, but the logger can be configured to match the program needs.

### Set Minimum Logging Level
```csharp
using Unleasharp.DB.Base;

Logging.SetMinimumLogLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
```

### Setup Default ILoggerFactory
```csharp
Logging.SetLoggingOptions(options => {
    options.SingleLine      = true;
    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] "; // Custom format
    options.UseUtcTimestamp = false;
    options.IncludeScopes   = true;
});
```

### Setup Custom ILoggerFactory
```csharp
Logging.SetLoggerFactory(
    LoggerFactory.Create(loggingBuilder => loggingBuilder
        .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace)
        .AddConsole()
    )
);
```

## Engine-Specific

### PostgreSQL

PostgreSQL requires special configuration when performing operations with Enums. In PostgreSQL, Enums can be defined as a specific type. This requires creating the Enum type first in PostgreSQL, which can be done with Unleasharp.DB, but also requires to Map first the C# Enum to the PostgreSQL Enum before creating the database connection.

```csharp
// Custom PostgreSQL constructor
ConnectorManager dbConnector = new ConnectorManager(new NpgsqlDataSourceBuilder(
        "Host=localhost;Port=5432;Database=unleasharp;Username=unleasharp;Password=unleasharp;Include Error Detail=true"
    ))
    // Built-in way to map C# Enum types to PostgreSQL Enums. It does automatically name the Enum the same way as the C# Enum, but lowercase
    .WithMappedEnum<EnumExample>()
    // Use PostgreSQL DataSourceBuilder to add the C# Enum using MapEnum<T>()
    .WithDataSourceBuilderSetup(sourceBuilder => {
        sourceBuilder.MapEnum<EnumExample>("enumexample");
    })
;

// Create the PostgreSQL Enum type based on the C# Enum type if it doesn't exist already
dbConnector.QueryBuilder().Build(query => query.CreateEnumType<EnumExample>()).Execute();
```
> ⚠️ **Note**: When creating the Enum type using the previous method, given Enum will not be mapped to PostgreSQL Enum in the ConnectorManager, it will require re-instantiating it or resetting the application. A future workaround should improve this behaviour.

### DuckDB

DuckDB can also define Enums as specific types, but they don't require to be maped to C# Enum types.

```csharp
// Create the DuckDB Enum type based on the C# Enum type if it doesn't exist already
dbConnector.QueryBuilder().Build(query => query.CreateEnumType<EnumExample>()).Execute();
```
