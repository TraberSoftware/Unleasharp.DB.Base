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

```csharp
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

## On Conflict Update

When using `OnInsertConflict.UPDATE`, conflicting rows are updated with the values specified in the INSERT operation. This is particularly useful for scenarios where you want to maintain data consistency by updating existing records rather than failing the entire operation.

```csharp
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

**Warning**: When using `OnInsertConflict.UPDATE`, be cautious about potential data loss if the INSERT values contain null or default values that might overwrite existing non-null values in the database. Consider using conditional updates or explicit column selection to avoid unintended modifications.
