# Select

The select operation will retrieve rows from the database based on the executed query. Depending on how you want to iterate over the data results, there are a few operations available to retrieve them.

## Syntax
The `Select()` method syntax is very simple:
 * Call `Select()` as an alias of `SELECT (*)` to retrieve all columns from the query
 * Use `Select("field_name")` to select a specific column by string
 * Use `Select<TableClassType>(table => table.column)` Query Expression syntax to select a specific class property as the column to be selected
 * Use a `List<string>` as parameter to select multiple columns at once

## FirstOrDefault()
Retrieves the first result of the query.

```csharp
// Select first row with ordering
ExampleTable row = dbConnector.QueryBuilder().Build(query => query 
    .Select()
    .From<ExampleTable>()
    .OrderBy("id", OrderDirection.DESC)
    .Limit(1)
).FirstOrDefault<ExampleTable>();
```

## AsEnumerable()
Returns an `IEnumerable<T>` result of the query. This method won't automatically iterate the table, but the result set retrieved by the executed query.

```csharp
foreach (ExampleTable row in dbConnector.QueryBuilder().Build(query => query
    .Select()
    .From<ExampleTable>()
    .Limit(10)
).AsEnumerable<ExampleTable>()) {
    // Do something with row
}
```

### ToList()
Returns a `List<T>` result of the query.
```csharp
List<example_table> rows = dbConnector.QueryBuilder().Build(query => query
    .Select()
    .From("example_table")
    .OrderBy("id", OrderDirection.DESC)
).ToList<example_table>();
```

## Iterate()
The `Iterate()` method will iterate over all rows of the table by using the provided column as the AutoIncrement ID column, for incremental retrieval. LIMIT-OFFSET could be used for this operation, but the performance quickly degrades over large results.

```csharp
foreach (ExampleTable row in dbConnector.QueryBuilder().Iterate<ExampleTable>(row => row.Id)) {
    // Do things to row
}
```

The iterator supports building complex queries with existing conditions:
```csharp
foreach (ExampleTable row in dbConnector.QueryBuilder().Build(query => 
    query.WhereLike<ExampleTable>(row => row.MediumText, "%Edited%")
).Iterate<ExampleTable>(row => row.Id)) {
    // Do things to row
}
```

Customize iteration with offset and batch size parameters:
```csharp
long offset    = 100;
int  batchSize = 50;
foreach (ExampleTable row in dbConnector.QueryBuilder().Build(query => 
    query.WhereLike<ExampleTable>(row => row.MediumText, "%Edited%")
).Iterate<ExampleTable>(row => row.Id, offset, batchSize)) {
    // Do things to row
}
```
