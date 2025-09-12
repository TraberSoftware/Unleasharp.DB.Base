# Join
When joining multiple tables, results are not automatically mapped to a single class. You must either:

1. Use a **custom `JoinedTable` class** (see [Joined Tables](../data-mapping/joined-tables.html)), or  
2. Return a **Tuple** of table types.

> 📝 **Note**: C# `Tuple<T1,T2,...>` supports up to 8 types. While `ValueTuple` allows more, support is not planned.

## Using Joined Table Class
When defined properly, the following query will map the selected fields to the corresponding properties of the class.

```csharp
// Select single row as joined type
JoinedTable row = dbConnector.QueryBuilder().Build(query => query
    .Select<ExampleTable>()
    .Select<ChildTable>()
    .From<ExampleTable>()
    .Join<ChildTable, ExampleTable>(
        child  => child.ParentId,
        parent => parent.Id
    )
    .OrderBy<ExampleTable>(
        row => row.Id,
        OrderDirection.DESC
    )
    .Limit(1)
    .Select()
).FirstOrDefault<JoinedTable>();
```

## Using Tuple
When returning a `Tuple<T1, T2,...T8>` the `QueryBuilder` will try to determine to which class do belong the returned columns, and assign them properly.

```csharp
// Select single row as a class tuple
Tuple<ExampleTable, ChildTable> joinedTypeRow = dbConnector.QueryBuilder().Build(query => query
    .Select<ExampleTable>()
    .Select<ChildTable>()
    .From<ExampleTable>()
    .Join<ChildTable, ExampleTable>(
        child  => child.ParentId,
        parent => parent.Id
    )
    .OrderBy<ExampleTable>(
        row => row.Id,
        OrderDirection.DESC
    )
    .Limit(1)
    .Select()
).FirstOrDefault<ExampleTable, ChildTable>();
```
