---
outline: deep
---

# Transactions

Transactions provide a way to execute multiple database operations as a single unit of work, ensuring data consistency and integrity. All operations within a transaction are either committed together or rolled back together if any operation fails.

## Transaction Methods

The Query Builder supports the following transaction methods:

- **Begin()** or **BeginTransaction()** - Starts a new transaction
- **Commit()** or **CommitTransaction()** - Commits the current transaction
- **Rollback()** or **RollbackTransaction()** - Rolls back the current transaction

## Basic Usage

```csharp
dbConnector.QueryBuilder().Begin();
// Insert data
if (dbConnector.QueryBuilder().Build(query => query
    .Into<ExampleTable>()
    .Value(new ExampleTable {
        MediumText = "Inserted from a transaction",
    })
    .Insert()
).Execute<bool>()) {
    // Commit on query success
    dbConnector.QueryBuilder().Commit();
}
else {
    // Rollback on query failure
    dbConnector.QueryBuilder().Rollback();
}
```

## MSSQL

### Named Transactions
In MSSQL, named transactions provide a way to explicitly name transaction blocks, making it easier to manage and debug complex transactional operations. When using named transactions, you can reference specific transaction names for commit or rollback operations.

```csharp
dbConnector.QueryBuilder().Begin("transaction_name");
// Insert data
if (dbConnector.QueryBuilder().Build(query => query
    .Into<ExampleTable>()
    .Value(new ExampleTable {
        MediumText = "Inserted from a transaction",
    })
    .Insert()
).Execute<bool>()) {
    // Commit on query success
    dbConnector.QueryBuilder().Commit("transaction_name");
}
else {
    // Rollback on query failure
    dbConnector.QueryBuilder().Rollback("transaction_name");
}
```
