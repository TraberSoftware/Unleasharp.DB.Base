# Unleasharp.DB.Base

![Unleasharp.DB.Base](https://socialify.git.ci/TraberSoftware/Unleasharp.DB.Base/image?description=1&font=Inter&logo=https%3A%2F%2Fraw.githubusercontent.com%2FTraberSoftware%2FUnleasharp%2Frefs%2Fheads%2Fmain%2Fassets%2Flogo-small.png&name=1&owner=1&pattern=Circuit+Board&theme=Light)

A lightweight, database-agnostic library for .NET that provides connection management, query building, and data serialization capabilities.

## 🎯 Key Concepts

This library provides a foundation for database operations with the following core features:

### 🔌 Connection Handling
- Automatic connection creation and management
- Configurable automatic connection regeneration after specified intervals
- Ensures database connections are always open and ready for use

### 🧵 Threading Support
- Thread-safe connection management through `ConnectorManager`
- Each thread receives its own dedicated database connection instance

### 📝 Query Generation
- **Query** class: Holds SQL query parameters in an engine-agnostic manner
- **QueryBuilder** class: Executes queries against the database and maps results to specified types
- Follows fluent interface pattern for intuitive query building

### 🔄 Serialization
- Automatic mapping of database results to C# objects using generic type parameters
- Supports both simple class mapping and attribute-based schema definitions

## 🔧 Query Generation Architecture

The library follows a **CRTP (Curiously Recurring Template Pattern)** approach where:
- The base `Query` class provides engine-agnostic functionality
- Engine-specific implementations handle the actual SQL rendering
- Query building follows standard SQL syntax with fluent method chaining

## 🛠️ Connection Initialization

### Using Connection String
```csharp
var connector = new ConnectorManager("Server=localhost;Database=unleasharp;Uid=unleasharp;Pwd=unleasharp;");
```

### Using Fluent Configuration
```csharp
var connector = new ConnectorManager()
    .WithAutomaticConnectionRenewal(true)
    .WithAutomaticConnectionRenewalInterval(TimeSpan.FromHours(1))
    .Configure(config => {
        config.ConnectionString = "Server=localhost;Database=unleasharp;Uid=unleasharp;Pwd=unleasharp;";
    })
    .WithOnQueryExceptionAction    ((query, ex) => Console.WriteLine($"Exception executing query:   {query.QueryRenderedString}\nException message:\n{ex.Message}"))
    .WithBeforeQueryExecutionAction((query    ) => Console.WriteLine($"Preparing query for execute: {query.Render()}"))
    .WithAfterQueryExecutionAction ((query    ) => Console.WriteLine($"Executed query:              {query.QueryRenderedString}"))
;
```

### Engine-Specific Connection Builder
```csharp
// MySQL example
var connector = new ConnectorManager(
    new MySqlConnectionStringBuilder("Server=localhost;Database=unleasharp;Uid=unleasharp;Pwd=unleasharp;")
);
```

## ⚙️ Configuration Options

### Connection Management
The connections are automatically renewed by default, but automatic connection renewal behaviour can be adjusted.

```csharp
connector.WithAutomaticConnectionRenewal(true)
        .WithAutomaticConnectionRenewalInterval(TimeSpan.FromHours(1))
```

## 📊 Querying Examples

### One-liner Execution
```csharp
var rows = DBConnector.QueryBuilder()
    .Build(query => query.Select().From("example_table"))
    .ToList<ExampleTable>();
```

### Standalone Query Generation
```csharp
var standaloneQuery = Query.GetInstance()
    .Select()
    .From("example_table");

var rows = DBConnector.QueryBuilder(standaloneQuery)
    .ToList<ExampleTable>();
```

### Available Query Operations
```csharp
// Select all fields from a table
Query.Select().From("example_table");

// Select with type mapping
Query.Delete().From<ExampleTable>().Where("id", 5);

// Insert single value
Query.Insert().Into("example_table").Value(new ExampleTable());

// Insert multiple values at once
Query.Insert()
    .Into("example_table")
    .Values(new List<ExampleTable> {
        new ExampleTable(),
        new ExampleTable()
    });
```

## 🏗️ Database Structure Definitions

### Simple Class Mapping (1:1)
```csharp
public class example_table {
    public ulong?       id          { get; set; }
    public string       _mediumtext { get; set; }
    public string       _longtext   { get; set; }
    public string       _json       { get; set; }
    public byte[]       _longblob   { get; set; }
    public EnumExample? _enum       { get; set; }
    public string       _varchar    { get; set; }
}
```

### Attribute-Based Schema Definition
```csharp
using System.ComponentModel;
using Unleasharp.DB.Base.SchemaDefinition;

[Table("example_table")]
[PrimaryKey("id")]
[UniqueKey("id", "id", "_enum")]
public class ExampleTable {
    [Column("id",          ColumnDataType.UInt64, Unsigned = true, PrimaryKey = true, AutoIncrement = true, NotNull = true)]
    public ulong? Id              { get; set; }

    [Column("_mediumtext", ColumnDataType.Text)]
    public string MediumText      { get; set; }

    [Column("_longtext",   ColumnDataType.Text)]
    public string Longtext        { get; set; }

    [Column("_json",       ColumnDataType.Json)]
    public string Json            { get; set; }

    [Column("_longblob",   ColumnDataType.Binary)]
    public byte[] CustomFieldName { get; set; }

    [Column("_enum",       ColumnDataType.Enum)]
    public EnumExample? Enum      { get; set; }

    [Column("_varchar",    "varchar", Length = 255)]
    public string Varchar         { get; set; }
}
```

### Enum Value Handling

**Important Considerations:**
- Database engines treat enum values starting from 1, while C# enums start from 0
- **Recommendation**: Use a `NONE` value as the first enum member or set the first C# enum value to 1

#### Option 1: NONE as First Value
```csharp
public enum EnumExample {
    NONE,
    Y,
    [Description("NEGATIVE")]
    N
}
```

#### Option 2: Explicit First Value
```csharp
public enum EnumExample {
    Y = 1,
    [Description("NEGATIVE")]
    N
}
```

The `[Description]` attribute maps the C# enum value to its database representation.

## 🚀 Database Engine Implementations

- ✅ **MySQL** - [Unleasharp.DB.MySQL](https://github.com/TraberSoftware/Unleasharp.DB.MySQL)
- ✅ **SQLite** - [Unleasharp.DB.SQLite](https://github.com/TraberSoftware/Unleasharp.DB.SQLite)
- ✅ **PostgreSQL** - [Unleasharp.DB.PostgreSQL](https://github.com/TraberSoftware/Unleasharp.DB.PostgreSQL)
- 🚧 **MSSQL** - Work in Progress

## 📦 Dependencies

- [Unleasharp](https://github.com/TraberSoftware/Unleasharp) - Multipurpose library

## 📋 Version Compatibility

This library targets .NET 8.0 and later versions. For specific version requirements, please check the package dependencies.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.
