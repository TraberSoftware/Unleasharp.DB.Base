---
outline: deep
---

# Annotated Classes
There is a set of Attributes that can be applied to both the table class and the column properties to have a better definition of the table.

* `Table`: defines the class as a database table with the specified name.
* `Column`: defines a property as a table column, with the specified name and data type. The data type can be defined as a `string` or as a `ColumnDataType`.

```csharp
[Table("example_table")]
public class ExampleTable {
    [Column("id", ColumnDataType.Int64, Unsigned = true, PrimaryKey = true, AutoIncrement = true, NotNull = true)]
    public long? Id { get; set; }

    [Column("_mediumtext", ColumnDataType.Text)]
    public string MediumText { get; set; }

    [Column("_longtext", ColumnDataType.Text)]
    public string Longtext { get; set; }

    [Column("_json", ColumnDataType.Json)]
    public string Json { get; set; }

    [Column("_longblob", ColumnDataType.Binary)]
    public byte[] CustomFieldName { get; set; }

    [Column("_enum", ColumnDataType.Enum)]
    public EnumExample? Enum { get; set; }

    [Column("_varchar", "varchar", Length = 255)]
    public string Varchar { get; set; }
}
```

## Keys
When defining tables, `Keys` can be defined by using different attributes:

* `PrimaryKey`: defines the column to be used as primary key
* `Foreign`: defines the column to be used as foreign key for the referenced table and property.
* `UniqueKey`: defines one or multiple columns to be used as a unique key.

### Key Definitions

This example shows how to define `Keys` for a `Table` class. The `UniqueKey` also features a custom key name instead of an auto-generated one.

```csharp
[Table("example_table")]
[PrimaryKey(typeof(ExampleTable), nameof(ExampleTable.Id))]
[UniqueKey ("my_custom_key_name", typeof(ChildTable), nameof(ChildTable.Id),       nameof(ChildTable.ParentId))]
public class ExampleTable {
    [Column("id", ColumnDataType.Int64, Unsigned = true, PrimaryKey = true, AutoIncrement = true, NotNull = true)]
    public long? Id { get; set; }

    [Column("_mediumtext", ColumnDataType.Text)]
    public string MediumText { get; set; }

    [Column("_longtext", ColumnDataType.Text)]
    public string Longtext { get; set; }

    [Column("_json", ColumnDataType.Json)]
    public string Json { get; set; }

    [Column("_longblob", ColumnDataType.Binary)]
    public byte[] CustomFieldName { get; set; }

    [Column("_enum", ColumnDataType.Enum)]
    public EnumExample? Enum { get; set; }

    [Column("_varchar", "varchar", Length = 255)]
    public string Varchar { get; set; }
}
```

### Foreign Keys Definition

The following sample illustrates how to reference a foreign key with the `ForeignKey` attribute.

```csharp
[Table("child_table")]
[PrimaryKey(typeof(ChildTable), nameof(ChildTable.Id))]
[ForeignKey(typeof(ChildTable), nameof(ChildTable.ParentId), typeof(ExampleTable), nameof(ExampleTable.Id))]
[UniqueKey (typeof(ExampleTable), nameof(ExampleTable.Id), nameof(ExampleTable.Enum))]
public class ChildTable {
    [Column("id", ColumnDataType.Int64, Unsigned = true, PrimaryKey = true, AutoIncrement = true, NotNull = true)]
    public long? Id { get; set; }

    [Column("parent_id", ColumnDataType.Int64, Unsigned = true)]
    public long? ParentId { get; set; }

    [Column("example_field", ColumnDataType.Varchar, Length = 255)]
    public string ExampleField { get; set; }
}
```

## System Columns
Engines like PostgreSQL implement hidden system columns that can be retrieved along the columns defined in the table. When defining a table, you can also define these fields to be retrieved for convenience. This fields are read-only and should be used carefully.

```csharp
[Table("example_table")]
[PrimaryKey(typeof(ExampleTable), nameof(ExampleTable.Id))]
[UniqueKey (typeof(ExampleTable), nameof(ExampleTable.Id), nameof(ExampleTable.Enum))]
public class ExampleTable {
    [SystemColumn("xmin", DatabaseEngine.PostgreSQL)]
    public long? xmin { get; set; }

    [Column("id", ColumnDataType.Int64, Unsigned = true, PrimaryKey = true, AutoIncrement = true, NotNull = true)]
    public long? Id { get; set; }

    [Column("_mediumtext", ColumnDataType.Text, Length = -1)]
    public string MediumText { get; set; }
}
```