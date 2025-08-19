![Unleasharp.DB.Base](https://socialify.git.ci/TraberSoftware/Unleasharp.DB.Base/image?description=1&font=Inter&logo=https%3A%2F%2Fraw.githubusercontent.com%2FTraberSoftware%2FUnleasharp%2Frefs%2Fheads%2Fmain%2Fassets%2Flogo-small.png&name=1&owner=1&pattern=Circuit+Board&theme=Light)

## Concepts
This library deals with: 
 * **Connection handling:** It creates, regenerates automatically after X time, and makes sure a given database connection is open
 * **Threading:** The ConnectorManager class handles the connections internally so only one connection is generated for each thread.
 * **Query generation:** The Query class holds the parameters for a SQL query. The QueryBuilder class holds a Query, and can perform the operations against the database, and retrieves the result of the query.
 * **Serialization:** The QueryBuilder class returns the Query result as the class you specify.

## Query Generation
Each database engine will have its own classes for the specific engine implementation, but Query should remain as much engine-agnostic as possible.

To do so, the Query class follows the CRTP patterns, it doesn't matter the implementation, the rendering is handled by the base but performed in the implementation.

The Query class follows a QueryBuilder pattern with chained functions to achieve transparent query generation, just using code to parametrice the query.


### Connection initialization
The **ConnectionManager** is the base to interact with a database implementation.

#### With a ConnectionString
```csharp
ConnectorManager DBConnector = new ConnectorManager("Server=localhost;Database=unleasharp;Uid=unleasharp;Pwd=unleasharp;");```
```

#### With a chained function
```csharp
ConnectorManager DBConnector = new ConnectorManager()
    .WithAutomaticConnectionRenewal(true)
    .WithAutomaticConnectionRenewalInterval(TimeSpan.FromHours(1))
    .Configure(ConnectionStringBuilder => {
        ConnectionStringBuilder.ConnectionString = "Server=localhost;Database=unleasharp;Uid=unleasharp;Pwd=unleasharp;";
    }
);
```

#### With engine-specific ConnectionStringBuilder

```csharp
// MySQL example
ConnectorManager DBConnector = new ConnectorManager(
    new MySqlConnectionStringBuilder("Server=localhost;Database=unleasharp;Uid=unleasharp;Pwd=unleasharp;")
);
```

## Querying
Once the connection has already been configured, you can start to build queries using the **QueryBuilder**. You can either retrieve the **QueryBuilder** instance and build the query from there, or generate a **Query** instance, parametrize it, and then pass it to a **QueryBuilder** as parameter.

### One-liner execute
```csharp
List<example_table> Rows = ConfiguredDBConnector.QueryBuilder().Build(Query => Query.Select().From("example_table")).ToList<example_table>();
```

### Standalone Query generation and later execution
```csharp
Query StandaloneQuery = Query.GetInstance().Select().From("example_table");
List<example_table> Rows = DBConnector.QueryBuilder(StandaloneQuery).ToList<ExampleTable>();
```

The available functions to build the queries are the same as the standard SQL query syntax, and should at least call the action to perform on que query: Select, Insert, Delete...

```csharp
// Select all fields from a table
Query.Select().From("example_table");

// Select a row from a table by generic Type
Query.Delete().From<ExampleTable>.Where("id", 5);

// Insert single value
Query.Insert().Into("example_table").Value(new ExampleTable());

// Insert multiple values at the same time
Query
    .Insert()
    .Into("example_table")
    .Values(new List<ExampleTable>{ 
            new ExampleTable(), 
            new ExampleTable()
        }
    )
;
```

## Database structure definitions
In order to serialize database data back to C# classes or insert C# objects into database, you can either define a simple class or an annotated class with **Attributes** to allow more flexible data mapping.

**Unleasharp.DB.Base.SchemaDefinition** provides the attributes to annotate the classes and provide with a better understanding of the current database table and column mapping.

### Examples

#### Simple class - 1:1 map
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

#### Mapped table with Attribute annotations
```csharp
using System.ComponentModel;
using Unleasharp.DB.Base.SchemaDefinition;

[Table("example_table")]
[Key("id", Field = "id", KeyType = Unleasharp.DB.Base.QueryBuilding.KeyType.PRIMARY)]
public class ExampleTable {
    [Column("id", "bigint", Unsigned = true, PrimaryKey = true, AutoIncrement = true, NotNull = true, Length = 20)]
    public ulong? ID              { get; set; }

    [Column("_mediumtext", "mediumtext")]
    public string MediumText      { get; set; }

    [Column("_longtext", "longtext")]
    public string _longtext       { get; set; }

    [Column("_json", "longtext")]
    public string _json           { get; set; }

    [Column("_enum", "enum")]
    public EnumExample? _enum     { get; set; }

    [Column("_varchar", "varchar", Length = 255)]
    public string _varchar        { get; set; }

    [Column("_longblob", "longblob")]
    public byte[] CustomFieldName { get; set; }
}
```

### Enum values
There are a few differences in how **Enum** values are handled in database and **C#**:
 * In database engines, the first value of an **Enum** is always 1. In **C#** is 0, thus the recommendation of creating a first value with an empty meaning (**NONE**) or setting the first value of the **C#** enum to 1.
 * In database engines, the **Enum** values are multi-purpose strings, they are not limited in what characters can each value hold. However, in C#, Enum values are limited to a very limited set of characters. If database **Enum** values don't respect the **C#** limits, then a custom annotated **Enum** should be used:

#### Enum with NONE as first value
```csharp
public enum EnumExample {
    NONE,
    Y,
    [Description("NEGATIVE")]
    N
}
```

#### Enum with first value set as 1
```csharp
public enum EnumExample {
    Y=1,
    [Description("NEGATIVE")]
    N
}
```


The **Description** attribute defines the value of the **Enum** in database that will be mapped to the current **C#** **Enum** value.

## Database Engine Implementations
 * MySQL - https://github.com/TraberSoftware/Unleasharp.DB.MySQL
 * SQLite - WIP
 * PostgreSQL - WIP
 * MSSQL - WIP
