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

## Simple Union

```csharp
List<ExampleTable> unionRows = dbConnector.QueryBuilder().Build(query => query
    .Union(
        Query.GetInstance()
            .Select<ExampleTable>()
            .From<ExampleTable>()
            .Where<ExampleTable>(row => row.Id, new Where<Query> {
                Value    = 10,
                Comparer = WhereComparer.LOWER_EQUALS
            })
    )
    .Union(
        Query.GetInstance()
            .Select<ExampleTable>()
            .From<ExampleTable>()
            .Where<ExampleTable>(row => row.Id, new Where<Query> {
                Value    = 10,
                Comparer = WhereComparer.GREATER
            })
            .Where<ExampleTable>(row => row.Id, new Where<Query> {
                Value    = 20,
                Comparer = WhereComparer.LOWER_EQUALS,
            })
    )
    .OrderBy(typeof(ExampleTable).GetColumnName(nameof(ExampleTable.Id)))
    .Limit(20)
).ToList<ExampleTable>();
```

## Union as Subselect

```csharp
List<ExampleTable> complexUnion = dbConnector.QueryBuilder().Build(query => query
    .Select()
    .From(Query.GetInstance()
        .Union(
            Query.GetInstance()
                .Select<ExampleTable>()
                .From<ExampleTable>()
                .Where<ExampleTable>(row => row.Id, new Where<Query> {
                    Value    = 10,
                    Comparer = WhereComparer.LOWER_EQUALS
                })
        )
        .Union(
            Query.GetInstance()
                .Select<ExampleTable>()
                .From<ExampleTable>()
                .Where<ExampleTable>(row => row.Id, new Where<Query> {
                    Value    = 10,
                    Comparer = WhereComparer.GREATER
                })
                .Where<ExampleTable>(row => row.Id, new Where<Query> {
                    Value    = 20,
                    Comparer = WhereComparer.LOWER_EQUALS,
                })
        )
        .OrderBy(typeof(ExampleTable).GetColumnName(nameof(ExampleTable.Id)))
        .Limit(20)
    , "subqueryAlias")
    .OrderBy(typeof(ExampleTable).GetColumnName(nameof(ExampleTable.Id)), OrderDirection.DESC)
).ToList<ExampleTable>();
```
