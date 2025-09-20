---
outline: deep
---

# Delete

Data deletion requires to specify at least:
* `From<T>()` or `From("table_name")` to select the target database table.
* One or more `Where()` sentences to determine the affected rows.


## Single Row

Delete a specific row using Query Expressions and retrieve the result as bool

::: code-group
```csharp [C#]
bool deleted = dbConnector.QueryBuilder().Build(query => query
    .From <ExampleTable>()
    .Where<ExampleTable>((row) => row.Id, 5)
    .Delete()
).Execute<bool>();
```

```sql [MySQL]
DELETE FROM
    example_table
WHERE
    `example_table`.`id`=5
```

```sql [SQLite]
DELETE FROM
    example_table
WHERE
    "example_table"."id"=5
```

```sql [PostgreSQL]
DELETE FROM
    example_table
WHERE
    "example_table"."id"=5
```

```sql [MSSQL]
DELETE FROM
	example_table
WHERE
	[example_table].[id] = 5
```

```sql [DuckDB]
DELETE FROM
    example_table
WHERE
    "example_table"."id"=5
```
:::

## Multiple Rows

Delete multiple rows by using a conditional where and retrieve the number of affected rows as result

::: code-group
```csharp [C#]
bool deleted = dbConnector.QueryBuilder().Build(query => query
    .From <ExampleTable>()
    .Where<ExampleTable>((row) => row.Id, 5)
    .Delete()
).Execute<bool>();
```

```sql [MySQL]
DELETE FROM
    example_table
WHERE
    `example_table`.`id`<100
```

```sql [SQLite]
DELETE FROM
    example_table
WHERE
    "example_table"."id"<100
```

```sql [PostgreSQL]
DELETE FROM 
    example_table
WHERE
    "example_table"."id"<100
```

```sql [MSSQL]
DELETE FROM
	example_table
WHERE
	[example_table].[id] < 100
```

```sql [DuckDB]
DELETE FROM 
    example_table
WHERE
    "example_table"."id"<100
```
:::

