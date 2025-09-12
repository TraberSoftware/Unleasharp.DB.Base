---
outline: deep
---

# Joined Tables
When performing a Join between two or more tables, the results can be either retrieved in a Tuple or in a single Class that holds all the fields retrieved by the query. The following sample class provides the syntax to define a joined table for this purpose.

```csharp
public class JoinedTable {
    // Columns coming from ExampleTable
    [Column(typeof(ExampleTable), nameof(ExampleTable.Id))]
    public long? Id { get; set; }
    [Column(typeof(ExampleTable), nameof(ExampleTable.MediumText))]
    public string MediumText { get; set; }
    [Column(typeof(ExampleTable), nameof(ExampleTable.Varchar))]
    public string Varchar { get; set; }

    // Columns coming from ChildTable
    [Column(typeof(ChildTable), nameof(ChildTable.ParentId))]
    public long? ParentId { get; set; }
    [Column(typeof(ChildTable), nameof(ChildTable.ExampleField))]
    public string ExampleField { get; set; }
}
```

