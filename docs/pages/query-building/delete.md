# Delete

Data deletion requires to specify at least:
* `From<T>()` or `From("table_name")` to select the target database table.
* One or more `Where()` sentences to determine the affected rows.


## Single Row

Delete a specific row using Query Expressions and retrieve the result as bool

```csharp
bool deleted = dbConnector.QueryBuilder().Build(query => query
    .From <ExampleTable>()
    .Where<ExampleTable>((row) => row.Id, row.Id)
    .Delete()
).Execute<bool>();
```

## Multiple Rows

Delete multiple rows by using a conditional where and retrieve the number of affected rows as result

```csharp
int affectedRows = dbConnector.QueryBuilder().Build(query => query
    .From <ExampleTable>()
    .Where<ExampleTable>((row) => row.Id, "< 100", false)
    .Delete()
).Execute<int>();
```
