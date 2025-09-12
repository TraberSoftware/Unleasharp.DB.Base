---
outline: deep
---

# Table Operations

Although basic, tables can be created using Unleasharp.DB. It even handles some special fields like PostgreSQL Enums, but as stated before, in a very basic way, so use with caution. Unleasharp.DB should not be used to provide a consistent DB structure, we highly recommend to define the database structure first and the table classes afterhand.

## Create Table
Creating tables requires annotated classes:

```csharp
dbConnector.QueryBuilder().Build(query => query.Create<ExampleTable>()).Execute();
```

## Drop Table
As Unleasharp.DB does not aim to provide full support for migrations or structure management on a complete basis, table dropping is not supported yet, but this feature may be added in future versions.
