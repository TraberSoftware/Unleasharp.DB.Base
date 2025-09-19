---
outline: deep
---

# Union

Union queries are supported with the following restrictions.

* **MySQL**: When selecting specific fields from a UNION, a union alias is **required**

> These restrictions are inherent to the database engines, not limitations of the Query Builder

## Available Methods
The following methods support union operations:
- `Query.Union()` - Combines results with duplicate removal
- `Query.UnionAll()` - Combines results including duplicates
- `Query.Intersect()` - Returns only rows common to both queries
- `Query.Except()` - Returns rows from first query not present in second
- `Query.UnionAlias()` - Set the union alias for the current query

## Examples

### Simple Union

```csharp
List<ExampleTable> unionRows = dbConnector.QueryBuilder().Build(query => query
    .Select()
    .Union(query => query
        .Select<ExampleTable>()
        .From<ExampleTable>()
        .WhereLowerEquals<ExampleTable>(row => row.Id, 10)
    )
    .Union(query => query
        .Select<ExampleTable>()
        .From<ExampleTable>()
        .WhereGreater    <ExampleTable>(row => row.Id, 10)
        .WhereLowerEquals<ExampleTable>(row => row.Id, 20)
    )
    .OrderBy(typeof(ExampleTable).GetColumnName(nameof(ExampleTable.Id)))
).ToList<ExampleTable>();
```

### Union as Subselect

```csharp
List<ExampleTable> unionRows = dbConnector.QueryBuilder().Build(query => query
    .From(query => query
        .Select()
        .Union(query => query
            .Select<ExampleTable>()
            .From<ExampleTable>()
            .WhereLowerEquals<ExampleTable>(row => row.Id, 10)
        )
        .Union(query => query
            .Select<ExampleTable>()
            .From<ExampleTable>()
            .WhereGreater    <ExampleTable>(row => row.Id, 10)
            .WhereLowerEquals<ExampleTable>(row => row.Id, 20)
        )
    ,"unioned")
    .OrderBy(typeof(ExampleTable).GetColumnName(nameof(ExampleTable.Id)))
    .Select()
).ToList<ExampleTable>();
```
