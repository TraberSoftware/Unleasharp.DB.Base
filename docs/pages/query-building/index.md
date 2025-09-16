---
outline: deep
---

# 🔍 Query Building
The core and pride of the Unleasharp.DB library is the Query Builder and the Query class. These classes work together to provide database-agnostic functionality while also providing query-execution, data iteration and serialization.

## Syntax Overview

The Query class follows intuitive, SQL-like syntax:
- Standard clauses: `SELECT`, `FROM`, `WHERE`, `JOIN`, `ORDER BY`, `LIMIT`
- Supports multiple result types:
  - **Boolean**: For `DELETE` operations (`true` if affected rows > 0)
  - **Numeric**: For `INSERT`/`UPDATE` (returns affected rows or last insert ID)
  - **Row sets**: For `SELECT` (returns collections or single records)

This makes query construction feel natural and fluent:

```csharp
new Query()
    .Select()
    .From("my_table")
    .Where("column", "column_value")
    .Limit(5)
    .OrderBy("id")
;
```

However, we need first to learn a few key elements to perform queries using Unleasharp.DB.

## Query Expressions
In order to build future-proof queries that rely on class entries instead of strings to define the queries, the usage of Query Expressions is recommended. Query Expressions are used to select a field/property on a query operator, if that field/property gets modified by modifying the Column attribute, the query will automatically get adjusted without the need to change the code. Query Expressions are accepted by the following query operations:

- `Select`
- `Join`
- `Where`, `WhereIn`, `WhereLike`
- `Set`
- `GroupBy`
- `OrderBy`

Query expressions provide type safety and automatic maintenance:

```csharp
new Query()
    .From <ExampleTable>()
    .Set  <ExampleTable>((row) => row.MediumText, "Edited medium text")
    .Set  <ExampleTable>((row) => row.Longtext,   "Edited long text")
    .Where<ExampleTable>((row) => row.Id,         row.Id)
    .Update()
;
```

> ✅ **Recommendation**: Use Query Expressions instead of string literals for better maintainability and refactoring support.

## Query Builder
The `QueryBuilder` class does not really perform the query building but holds an active query and the DB connection, and acts as bridge between the `Query` class and the database engine. There are multiple ways to build queries:

### Standalone Query
```csharp
Query query = new Query()
    .Select()
    .From<ExampleTable>()
    .OrderBy("id", OrderDirection.DESC)
    .Limit(1)
;
QueryBuilder builder = dbConnector.QueryBuilder(query);
```

### Built-in Build Method (Recommended)
```csharp
QueryBuilder builder = dbConnector.QueryBuilder().Build(query => query
    .Select()
    .From<example_table>()
    .OrderBy("id", OrderDirection.DESC)
    .Limit(1)
);
```

With the `QueryBuilder` setup with an active query, you can then retrieve the results or execute the query.
```csharp
QueryBuilder builder = dbConnector.QueryBuilder().Build(query => query
    .Select()
    .From<example_table>()
    .OrderBy("id", OrderDirection.DESC)
    .Limit(1)
);
example_table row = builder.FirstOrDefault<example_table>();
```

#### After-query Data
After execution, the `QueryBuilder` provides these useful properties:

```csharp
QueryBuilder builder = dbConnector.QueryBuilder();
// ... execute query ...

builder.AffectedRows; // [int]       Useful when deleting or updating.
builder.TotalCount;   // [int]       Available when performing a Select COUNT() query.
builder.Result;       // [DataTable] Data retrieved by a Select query. Not intended to be accessed directly.
builder.ScalarValue;  // [object]    Available when using ExecuteScalar() or on a regular Select by retrieving the first column of the first row of the resultset.
```

These properties can be directly retrieved on execution by using the [`Execute<T>()`](../query-building/execute.html) method.