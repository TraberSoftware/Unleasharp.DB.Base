---
outline: deep
---

# Upsert

An **upsert** (short for "insert or update") operation combines the functionality of INSERT and UPDATE operations into a single atomic operation. When inserting data into a table, if a conflict occurs due to a PRIMARY KEY or UNIQUE constraint violation, the upsert behavior determines how to handle the conflicting row.

The `OnConflict` method provides a mechanism to define the specific behavior when such conflicts occur during insertion operations. This method accepts two parameters:

1. **OnConflict behavior** - Specifies how to handle conflicting rows:
   - `OnInsertConflict.IGNORE` - When a conflict occurs, skip the conflicting row and continue inserting other rows
   - `OnInsertConflict.UPDATE` - When a conflict occurs, update the existing row with the values from the INSERT operation
2. **Key column**: The conflict resolution is based on this column.

## On Conflict Ignore

When using `OnInsertConflict.IGNORE`, conflicting rows are silently skipped while the operation continues with non-conflicting rows. This is useful when you want to avoid duplicate entries but don't need to modify existing data.

::: code-group
```csharp [C#]
int insertedOrUpdatedItems = dbConnector.QueryBuilder().Build(query => { query
    .Into<ExampleTable>()
    .Value(new ExampleTable {
        Id         = 1234,
        MediumText = "InsertOrUpdated",
        Enum       = EnumExample.N
    })
    .Insert()
    .OnConflict<ExampleTable>(OnInsertConflict.IGNORE, row => row.Id);
}).Execute<int>();
```

```sql [MySQL]
INSERT INTO example_table 
    (id, _mediumtext, _longtext, _json, _longblob, _enum, _varchar)
VALUES
    (1234, 'InsertOrUpdated', NULL, NULL, NULL, 'NEGATIVE', NULL) 
ON DUPLICATE KEY UPDATE 
    `id` = VALUES (`id`)
```

```sql [SQLite]
INSERT OR IGNORE INTO example_table 
    (id, _mediumtext, _longtext, _json, _longblob, _enum, _varchar)
VALUES
    (1234, 'InsertOrUpdated', NULL, NULL, NULL, 'NEGATIVE', NULL)
```

```sql [PostgreSQL]
INSERT INTO "example_table" 
    ("id", "_mediumtext", "_longtext", "_json", "_longblob", "_enum", "_varchar")
VALUES
    (1234, 'InsertOrUpdated', NULL, NULL, NULL, 'N', NULL) 
ON CONFLICT ("id") 
    DO NOTHING 
RETURNING id
```

```sql [MSSQL]
SET IDENTITY_INSERT example_table ON;
MERGE 
	example_table AS target 
USING (
	VALUES (1234, 'InsertOrUpdated', NULL, NULL, NULL, 'NEGATIVE', NULL)
) 
AS 
	source(id, _mediumtext, _longtext, _json, _longblob, _enum, _varchar)
	ON 
	target.[id] = source.[id] 
	WHEN NOT MATCHED THEN INSERT 
        ([id], [_mediumtext], [_longtext], [_json], [_longblob], [_enum], [_varchar])
    VALUES 
        (source.[id], source.[_mediumtext], source.[_longtext], source.[_json], source.[_longblob], source.[_enum], source.[_varchar])
;
```

```sql [DuckDB]
INSERT INTO "example_table" 
    ("id", "_mediumtext", "_longtext", "_json", "_longblob", "_enum", "_varchar")
VALUES
    (1234, 'InsertOrUpdated', NULL, NULL, NULL, 'N', NULL) 
ON CONFLICT ("id") 
    DO NOTHING 
RETURNING id
```
:::

## On Conflict Update

When using `OnInsertConflict.UPDATE`, conflicting rows are updated with the values specified in the INSERT operation. This is particularly useful for scenarios where you want to maintain data consistency by updating existing records rather than failing the entire operation.

::: code-group
```csharp [C#]
int insertedOrUpdatedItems = dbConnector.QueryBuilder().Build(query => { query
    .Into<ExampleTable>()
    .Value(new ExampleTable {
        Id         = 1234,
        MediumText = "InsertOrUpdated",
        Enum       = EnumExample.N
    })
    .Insert()
    .OnConflict<ExampleTable>(OnInsertConflict.UPDATE, row => row.Id);
}).Execute<int>();
```


```sql [MySQL]
INSERT INTO example_table
    (id, _mediumtext, _longtext, _json, _longblob, _enum, _varchar)
VALUES
    (1234, 'InsertOrUpdated', NULL, NULL, NULL, 'NEGATIVE', NULL)
ON DUPLICATE KEY UPDATE 
    `id`          = VALUES(`id`),
    `_mediumtext` = VALUES(`_mediumtext`),
    `_longtext`   = VALUES (`_longtext`),
    `_json`       = VALUES(`_json`),
    `_longblob`   = VALUES(`_longblob`),
    `_enum`       = VALUES(`_enum`),
    `_varchar`    = VALUES(`_varchar`)
```

```sql [SQLite]
INSERT OR REPLACE INTO example_table 
    (id, _mediumtext, _longtext, _json, _longblob, _enum, _varchar)
VALUES
    (1234, 'InsertOrUpdated', NULL, NULL, NULL, 'NEGATIVE', NULL)
```

```sql [PostgreSQL]
INSERT INTO "example_table" 
    ("id", "_mediumtext", "_longtext", "_json", "_longblob", "_enum", "_varchar")
VALUES
    (1234, 'InsertOrUpdated', NULL, NULL, NULL, 'N', NULL)
ON CONFLICT (id) DO UPDATE
SET
    "id"          = EXCLUDED."id",
    "_mediumtext" = EXCLUDED."_mediumtext",
    "_longtext"   = EXCLUDED."_longtext",
    "_json"       = EXCLUDED."_json",
    "_longblob"   = EXCLUDED."_longblob",
    "_enum"       = EXCLUDED."_enum",
    "_varchar"    = EXCLUDED."_varchar" RETURNING id
```

```sql [MSSQL]
SET IDENTITY_INSERT example_table ON;
MERGE example_table AS target 
USING (
	VALUES (1234, 'InsertOrUpdated', NULL, NULL, NULL, 'NEGATIVE', NULL)
) 
AS 
    source(id, _mediumtext, _longtext, _json, _longblob, _enum, _varchar) 
    ON
    target.[id] = source.[id]
    WHEN MATCHED THEN UPDATE
    SET
	    [_mediumtext] = source.[_mediumtext],
	    [_longtext]   = source.[_longtext],
	    [_json]       = source.[_json],
	    [_longblob]   = source.[_longblob],
	    [_enum]       = source.[_enum],
	    [_varchar]    = source.[_varchar] 
    WHEN NOT MATCHED THEN INSERT 
        ([id], [_mediumtext], [_longtext], [_json], [_longblob], [_enum], [_varchar])
    VALUES
	    (source.[id], source.[_mediumtext], source.[_longtext], source.[_json], source.[_longblob], source.[_enum], source.[_varchar])
;
```

```sql [DuckDB]
INSERT INTO "example_table" 
    ("id", "_mediumtext", "_longtext", "_json", "_longblob", "_enum", "_varchar")
VALUES
    (1234, 'InsertOrUpdated', NULL, NULL, NULL, 'N', NULL)
ON CONFLICT (id) DO UPDATE
SET
    "id"          = EXCLUDED."id",
    "_mediumtext" = EXCLUDED."_mediumtext",
    "_longtext"   = EXCLUDED."_longtext",
    "_json"       = EXCLUDED."_json",
    "_longblob"   = EXCLUDED."_longblob",
    "_enum"       = EXCLUDED."_enum",
    "_varchar"    = EXCLUDED."_varchar" RETURNING id
```
:::

**Warning**: When using `OnInsertConflict.UPDATE`, be cautious about potential data loss if the INSERT values contain null or default values that might overwrite existing non-null values in the database. Consider using conditional updates or explicit column selection to avoid unintended modifications.
