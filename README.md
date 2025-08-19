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
    });
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
public class ExampleTable {
    public ulong? Id { get; set; }
    public string MediumText { get; set; }
    public string LongText { get; set; }
    public string Json { get; set; }
    public byte[] LongBlob { get; set; }
    public EnumExample? EnumValue { get; set; }
    public string Varchar { get; set; }
}
```

### Attribute-Based Schema Definition
```csharp
using System.ComponentModel;
using Unleasharp.DB.Base.SchemaDefinition;

[Table("example_table")]
[Key("id", Field = "id", KeyType = Unleasharp.DB.Base.QueryBuilding.KeyType.PRIMARY)]
public class ExampleTable {
    [Column("id", "bigint", Unsigned = true, PrimaryKey = true, AutoIncrement = true, NotNull = true, Length = 20)]
    public ulong? Id { get; set; }
    
    [Column("_mediumtext", "mediumtext")]
    public string MediumText { get; set; }
    
    [Column("_longtext", "longtext")]
    public string LongText { get; set; }
    
    [Column("_json", "longtext")]
    public string Json { get; set; }
    
    [Column("_enum", "enum")]
    public EnumExample? EnumValue { get; set; }
    
    [Column("_varchar", "varchar", Length = 255)]
    public string Varchar { get; set; }
    
    [Column("_longblob", "longblob")]
    public byte[] CustomFieldName { get; set; }
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
- 🚧 **SQLite** - Work in Progress
- 🚧 **PostgreSQL** - Work in Progress
- 🚧 **MSSQL** - Work in Progress
