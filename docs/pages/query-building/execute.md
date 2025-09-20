---
outline: deep
---

# Execute operation

When performing certain database operations, the `Execute<T>()` method provides a way to execute and determine the result of the provided query in database, which depends on the query type, the actions performed in the database and the result itself.

Use `Execute<T>()` to return specific results:

| Return Type                          | Use Case                                                    |
|--------------------------------------|-------------------------------------------------------------|
| `int` `long` `uint` `ulong` `string` | Last inserted ID (e.g., Auto-Increment or GUID)             |
| `int` `long` `uint` `ulong`          | Affected rows (useful for bulk inserts/updates/deletes)     |
| `bool`                               | Whether any row was affected (`AffectedRows > 0`) or exists |

## Last Insert ID
::: code-group
```csharp [C#]
string lastInsertId = dbConnector.QueryBuilder().Build(query => query
    .From<ChildTable>()
    .Value(new ChildTable {
        ParentId     = 1,
        ExampleField = "Example Field Value"
    })
    .Insert()
).Execute<string>();
```

```sql [SQL]
INSERT INTO child_table 
	(parent_id, example_field)
VALUES
	(1, 'Example Field Value')
```
:::

> 📝 **Note**: Some database engines can define a GUID filed as Identifier, you can either return a `string` or numeric types `int`,`long`,`uint`,`ulong` depending on the table definition itself.

## Affected rows
The affected rows count comes useful when performing multiple inserts, as well as updates or deletions.

::: code-group
```csharp [C#]
int affectedRows = dbConnector.QueryBuilder().Build(query => query
    .From<ExampleTable>()
    .Value(new ExampleTable {
        MediumText = "Medium text example value",
        Enum       = EnumExample.N
    })
    .Values(new List<ExampleTable> {
        new ExampleTable {
            Json            = @"{""sample_json_field"": ""sample_json_value""}",
            Enum            = EnumExample.Y,
            CustomFieldName = new byte[8] { 81,47,15,21,12,16,23,39 }
        },
        new ExampleTable {
            Longtext = "Long text example value",
            Enum     = EnumExample.N
        }
    })
    .Insert()
).Execute<int>();
```

```sql [SQL]
INSERT INTO example_table 
	(_mediumtext, _longtext, _json, _longblob,_enum, _varchar)
VALUES
	('Medium text example value', NULL, NULL, NULL, 'NEGATIVE', NULL),
	(NULL, NULL, '{"sample_json_field": "sample_json_value"}', '0x512F0F150C101727', 'Y', NULL),
	(NULL, 'Long text example value', NULL, NULL, 'NEGATIVE', NULL)
```
:::

## Success Check (Boolean)
When setting `T` as `boolean`, the query will check for success by checking the number of affected rows, the exceptions thrown during the execution, etc.

::: code-group

```csharp [C#]
bool inserted = dbConnector.QueryBuilder().Build(query => query
    .From<ChildTable>()
    .Value(new ChildTable {
        ParentId     = 1,
        ExampleField = "Example Field Value"
    })
    .Insert()
).Execute<bool>();
```

```sql [SQL]
INSERT INTO child_table 
	(parent_id, example_field)
VALUES
	(1, 'Example Field Value')
```

:::