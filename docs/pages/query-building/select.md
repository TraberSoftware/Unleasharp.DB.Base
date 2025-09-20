---
outline: deep
---

# Select

The select operation will retrieve rows from the database based on the executed query. Depending on how you want to iterate over the data results, there are a few operations available to retrieve them.

## Syntax
The `Select()` method syntax is very simple:
 * Call `Select()` as an alias of `SELECT (*)` to retrieve all columns from the query
 * Use `Select("field_name")` to select a specific column by string
 * Use `Select<TableClassType>(table => table.column)` Query Expression syntax to select a specific class property as the column to be selected
 * Use a `List<string>` as parameter to select multiple columns at once

## Data Retrieval Methods

### FirstOrDefault()
Retrieves the first result of the query. If no `Limit()` has been previously set, it will automatically set the `Limit()` to 1.

::: code-group
```csharp [C#]
// Select first row with ordering
ExampleTable row = dbConnector.QueryBuilder().Build(query => query 
    .Select()
    .From<ExampleTable>()
    .OrderBy("id", OrderDirection.DESC)
).FirstOrDefault<ExampleTable>();
```

```sql [SQL]
SELECT
    *
FROM
    "example_table"
ORDER BY
    "id" DESC
LIMIT
    0, 1
```
:::

### ToList()
Returns a `List<T>` result of the query.

::: code-group
```csharp [C#]
List<example_table> rows = dbConnector.QueryBuilder().Build(query => query
    .Select()
    .From("example_table")
    .OrderBy("id", OrderDirection.DESC)
).ToList<example_table>();
```

```sql [SQL]
SELECT
    *
FROM
    "example_table"
ORDER BY
    "id" DESC
```
:::

### AsEnumerable()
Returns an `IEnumerable<T>` result of the query. This method won't automatically iterate the table, but the result set retrieved by the executed query.

::: code-group
```csharp [C#]
foreach (ExampleTable row in dbConnector.QueryBuilder().Build(query => query
    .Select()
    .From<ExampleTable>()
    .Limit(10)
).AsEnumerable<ExampleTable>()) {
    // Do something with row
}
```


```sql [SQL]
SELECT
    *
FROM
    "example_table"
ORDER BY
    "id" DESC
LIMIT 
    0, 10
```
:::

### Iterate()
The `Iterate()` method will iterate over all rows of the table by using the provided column as the AutoIncrement ID column, for incremental retrieval. LIMIT-OFFSET could be used for this operation, but the performance quickly degrades over large results.

::: code-group
```csharp [C#]
foreach (ExampleTable row in dbConnector.QueryBuilder().Iterate<ExampleTable>(row => row.Id)) {
    // Do things to row
}
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
    "example_table"."id">0
LIMIT
    0, 100
```
:::

The iterator supports building complex queries with existing conditions:
::: code-group
```csharp [C#]
foreach (ExampleTable row in dbConnector.QueryBuilder().Build(query => 
    query.WhereLike<ExampleTable>(row => row.MediumText, "%Edited%")
).Iterate<ExampleTable>(row => row.Id)) {
    // Do things to row
}
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
    "example_table"."_mediumtext" LIKE '%Edited%'
    AND 
    "example_table"."id">0
LIMIT
    0, 100
```
:::

Customize iteration with offset and batch size parameters:
::: code-group
```csharp [C#]
long offset    = 100;
int  batchSize = 50;
foreach (ExampleTable row in dbConnector.QueryBuilder().Build(query => 
    query.WhereLike<ExampleTable>(row => row.MediumText, "%Edited%")
).Iterate<ExampleTable>(row => row.Id, offset, batchSize)) {
    // Do things to row
}
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
    "example_table"."_mediumtext" LIKE '%Edited%'
    AND
    "example_table"."id">100
LIMIT
    0, 50
```
:::
