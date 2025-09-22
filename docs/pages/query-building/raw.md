---
outline: deep
---

# Raw Queries
While strongly discouraged for maintainability and safety reasons, the `Query Builder` supports executing raw SQL statements and returning the results through the same query pipeline as built queries.

Use raw queries only when the query builder cannot express the SQL you need (complex vendor-specific SQL, optimized windowing, custom DDL/DDL-like statements, etc.). Prefer the fluent Query Builder API for readability, automatic parameterization, and cross-database compatibility.

A few considerations should be taken:

- `Query.Raw(...)` injects your SQL into the builder pipeline. The builder will return the execution result according to the specified underlying `Query Type`.
- You should supply the correct underlying `QueryType` (for example: `QueryType.SELECT`) so the library can correctly handle the results.
- When possible, use prepared parameters instead of string concatenation to avoid SQL injection and to allow the database driver to map types correctly.

## Set Query
```csharp
System.Data.DataRow rawRow = dbConnector.QueryBuilder().Build(query => query
    .Raw("SELECT 1", QueryType.SELECT)
).FirstOrDefault();
```

## Prepared Values

When using raw queries, prepared value placeholders can be used to generate a prepared query statement.

> 📝 **Note**: While most database engines use `@`, DuckDB uses `$` as the prepared parameter character.

### Setting Prepared Values

You can pass a dictionary of prepared values directly to `Query.Raw(...)`.

::: code-group
```csharp [Any]
System.Data.DataRow rawRow = dbConnector.QueryBuilder().Build(query => query
    .Raw("SELECT @my_custom_key_1", new Dictionary<string, dynamic>{
        { "@my_custom_key_1", "a random string value" }
    }, QueryType.SELECT)
).FirstOrDefault();
```

```csharp [DuckDB]
System.Data.DataRow rawRow = dbConnector.QueryBuilder().Build(query => query
    .Raw("SELECT $my_custom_key_1", new Dictionary<string, dynamic>{
        { "$my_custom_key_1", "a random string value" }
    }, QueryType.SELECT)
).FirstOrDefault();
```
:::

### Adding Prepared Values

Parameters can also be added incrementally with .AddPreparedValue(...), which is chainable.

::: code-group
```csharp [Any]
System.Data.DataRow rawRow = dbConnector.QueryBuilder().Build(query => query
    .Raw("SELECT @my_custom_key_1", QueryType.SELECT)
    .AddPreparedValue("@my_custom_key_1", "a random string value")
).FirstOrDefault();
```

```csharp [DuckDB]
System.Data.DataRow rawRow = dbConnector.QueryBuilder().Build(query => query
    .Raw("SELECT $my_custom_key_1", QueryType.SELECT)
    .AddPreparedValue("$my_custom_key_1", "a random string value")
).FirstOrDefault();
```
