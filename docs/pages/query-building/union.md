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

::: code-group
```csharp [C#]
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

```sql [SQL]
SELECT
    "example_table"."id",
    "example_table"."_mediumtext",
    "example_table"."_longtext",
    "example_table"."_json",
    "example_table"."_longblob",
    "example_table"."_enum",
    "example_table"."_varchar"
FROM
    "example_table"
WHERE
    "example_table"."id"<=10
UNION
SELECT
    "example_table"."id",
    "example_table"."_mediumtext",
    "example_table"."_longtext",
    "example_table"."_json",
    "example_table"."_longblob",
    "example_table"."_enum",
    "example_table"."_varchar"
FROM
    "example_table"
WHERE
    "example_table"."id">10
    AND
    "example_table"."id"<=20
ORDER BY
    "id" ASC
```
:::

### Union as Subselect

::: code-group
```csharp [C#]
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

```sql [SQL]
SELECT
    *
FROM (
    SELECT
        "example_table"."id",
        "example_table"."_mediumtext",
        "example_table"."_longtext",
        "example_table"."_json",
        "example_table"."_longblob",
        "example_table"."_enum",
        "example_table"."_varchar"
    FROM
        "example_table"
    WHERE
        "example_table"."id"<=10
    UNION
    SELECT
        "example_table"."id",
        "example_table"."_mediumtext",
        "example_table"."_longtext",
        "example_table"."_json",
        "example_table"."_longblob",
        "example_table"."_enum",
        "example_table"."_varchar"
    FROM
        "example_table"
    WHERE
        "example_table"."id">10
        AND
        "example_table"."id"<=20
) unioned
ORDER BY
    "id" ASC
```
:::
