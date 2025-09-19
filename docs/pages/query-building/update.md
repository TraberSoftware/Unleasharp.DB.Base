---
outline: deep
---

# Update
Data update can be strictly typed by using Query Expressions or very lax by using plain strings to set the column values.

To perform an update, there must be a call to at least two methods:

* `From<T>()` or `From("table_name")` to select the target database table.
* `Set<T>(expression, value)` or `Set("field", value)` as many times as needed to set the column values to update.

One or more `Where()` sentences are also welcome to determine the affected rows.

## Single Row

Update a specific row using Query Expressions and retrieve the result as a boolean.

```csharp
bool updated = dbConnector.QueryBuilder().Build(query => query
    .From <ExampleTable>()
    .Set  <ExampleTable>((row) => row.MediumText,      "Edited medium text")
    .Set  <ExampleTable>((row) => row.Longtext,        "Edited long text")
    .Set  <ExampleTable>((row) => row.Json,            @"{""json_field"": ""json_edited_value""}")
    .Set  <ExampleTable>((row) => row.CustomFieldName, new byte[8] { 12, 13, 14, 15, 12, 13, 14, 15 })
    .Set  <ExampleTable>((row) => row.Enum,            EnumExample.N)
    .Set  <ExampleTable>((row) => row.Varchar,         "Edited varchar")
    .Where<ExampleTable>((row) => row.Id,              row.Id)
    .Update()
).Execute<bool>();
```

## Multiple Rows

Update multiple rows by using a conditional `Where` clause. 
The example returns a boolean indicating whether the update executed successfully; adjust the `Execute<T>()` type if you need a different kind of result (for example, an integer count of affected rows).

```csharp
bool updated = dbConnector.QueryBuilder().Build(query => query
    .From <ExampleTable>()
    .Set  <ExampleTable>((row) => row.MediumText,      "Edited medium text")
    .Set  <ExampleTable>((row) => row.Longtext,        "Edited long text")
    .Set  <ExampleTable>((row) => row.Json,            @"{""json_field"": ""json_edited_value""}")
    .Set  <ExampleTable>((row) => row.CustomFieldName, new byte[8] { 12, 13, 14, 15, 12, 13, 14, 15 })
    .Set  <ExampleTable>((row) => row.Enum,            EnumExample.N)
    .Set  <ExampleTable>((row) => row.Varchar,         "Edited varchar")
    .Where<ExampleTable>((row) => row.Id,              "> 1000", false)
    .Update()
).Execute<bool>();
```

## Automatic Update

Objects loaded via the Query Builder (for example with `FirstOrDefault<T>()` or `ToList<T>()`) can be updated directly by passing the object back to `QueryBuilder.Update(row)`. The object must map to a table and include the primary or unique key value so the correct row can be targeted.

```csharp
ExampleTable toUpdate = dbConnector.QueryBuilder().Build(query => query
    .From<ExampleTable>()
    .OrderBy<ExampleTable>(row => row.Id, OrderDirection.DESC)
    .Limit(1)
    .Select()
).FirstOrDefault<ExampleTable>();

toUpdate.MediumText = "Updated medium text";
bool updated = dbConnector.QueryBuilder().Update(toUpdate);
```

This will typically translate into a SQL UPDATE statement similar to:

```sql
UPDATE
    "example_table" 
SET 
    "_mediumtext"='Updated medium text' 
WHERE 
    "example_table"."id"=1
```