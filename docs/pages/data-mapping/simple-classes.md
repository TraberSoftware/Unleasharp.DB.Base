---
outline: deep
---

# Simple classes
This is the most simple way to define a database table as a class. It will be a 1:1 representation of the table:
 * The class name is the table name.
 * The field/property name is the column name.

```csharp
public class example_table {
    public long?        id          { get; set; }
    public string       _mediumtext { get; set; }
    public string       _longtext   { get; set; }
    public string       _json       { get; set; }
    public byte[]       _longblob   { get; set; }
    public EnumExample? _enum       { get; set; }
    public string       _varchar    { get; set; }
}
```

> ⚠️ **Warning**: This approach is not recommended for production use as it's fragile to column name changes.
