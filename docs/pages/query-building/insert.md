---
outline: deep
---

# Insert
Data insertion can be strictly typed using annotated classes or very lax by using standard classes without any special configuration. 

To perform an insert, there must be a call to at least two methods:

* `Into<T>()` or `Into("table_name")` to select the target database table.
* `Value<T>(row)`, `Value(row)`, `Values<T>(row)` or `Values(row)` to set the row data to insert.

## Single Row

Insert a single row and retrieve the Auto-Increment ID as `long`.

::: code-group
```csharp [C#]
long insertedId = dbConnector.QueryBuilder().Build(query => query
    .Insert()
    .Into<ExampleTable>()
    .Value(new ExampleTable {
        MediumText = "Medium text example value",
        Enum       = EnumExample.N
    })
).Execute<long>();
```

```sql
INSERT INTO example_table 
    (_mediumtext, _longtext, _json, _longblob, _enum, _varchar)
VALUES
    ('Medium text example value', NULL, NULL, NULL, 'NEGATIVE', NULL)
```
:::

## Multiple Rows

You can either call `Value()` multiple times or call `Values()` and provide multiple values at the same time, or call both!

::: code-group
```csharp [C#]
dbConnector.QueryBuilder().Build(query => query
    .Into<ExampleTable>()
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
).Execute();
```

```sql [SQL]
INSERT INTO example_table 
    (_mediumtext, _longtext, _json, _longblob, _enum, _varchar)
VALUES
    ('Medium text example value', NULL, NULL, NULL, 'NEGATIVE', NULL),
    (NULL, NULL, '{"sample_json_field": "sample_json_value"}', '0x512F0F150C101727', 'Y', NULL),
    (NULL, 'Long text example value', NULL, NULL, 'NEGATIVE', NULL)
```
:::

> ⚠️ **Critical**: Do **not mix rows with and without primary key values** in the same batch.

❌ **Dangerous**:
::: code-group
```csharp [C#]
.Values(new List<ExampleTable> {
    new ExampleTable { Id = 1, Longtext = "Bad!"  }, // ❌ Don't set ID manually!
    new ExampleTable {         Longtext = "Good!" }
})
```

```sql [SQL]
INSERT INTO example_table 
    (id, _mediumtext, _longtext, _json, _longblob, _enum, _varchar)
VALUES
  (1, NULL, 'Bad!', NULL, NULL, NULL, NULL),
  (NULL, NULL, 'Good!', NULL, NULL, NULL, NULL)
```
:::
