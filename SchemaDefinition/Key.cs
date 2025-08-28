using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.ExtensionMethods;
using Unleasharp.DB.Base.QueryBuilding;

namespace Unleasharp.DB.Base.SchemaDefinition;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public class TableKey : NamedStructure {
    public string[] Columns { get; set; } = new string[0];
    public string   Column  {
        get {
            return Columns.FirstOrDefault();
        }
        set {
            Columns = new string[] { value };
        }
    }
    public IndexType IndexType { get; set; } = IndexType.NONE;

    public TableKey(string name) : base(name) {
        Column = name;
    }

    public TableKey(string name, string column) : base(name) {
        Column = column;
    }

    public TableKey(string name, params string[] columns) : base(name) {
        Columns = columns;
    }

    public TableKey(string name, Type table, params string[] columns) : base(name) {
        Columns = columns.Select(column => table.GetColumnName(column)).ToArray();
    }

    public TableKey(string name, Type table, string column) : base(name) {
        Column = table.GetColumnName(column);
    }

    public TableKey() { }
}

// ----- Keys -----
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class Key : TableKey {
    public Key() {}

    public Key(string name)                                      : base(name) { }
    public Key(string name, string column)                       : base(name, column) { }
    public Key(string name, params string[] columns)             : base(name, columns) { }
    public Key(string name, Type table, params string[] columns) : base(name, table, columns) { }
    public Key(string name, Type table, string column)           : base(name, table, column) { }
    public Key(Type table, params string[] columns)              : base(columns.FirstOrDefault(), table, columns) { }
    public Key(Type table, string column)                        : base(column, table, column) { }
}

// ----- Primary keys -----
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PrimaryKey : TableKey {
    public PrimaryKey() {}

    public PrimaryKey(string name)                                      : base(name) { }
    public PrimaryKey(string name, string column)                       : base(name, column) { }
    public PrimaryKey(string name, params string[] columns)             : base(name, columns) { }
    public PrimaryKey(string name, Type table, params string[] columns) : base(name, table, columns) { }
    public PrimaryKey(string name, Type table, string column)           : base(name, table, column) { }
    public PrimaryKey(Type table, params string[] columns)              : base(columns.FirstOrDefault(), table, columns) { }
    public PrimaryKey(Type table, string column)                        : base(column, table, column) { }
}

// ----- Unique keys -----
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class UniqueKey : TableKey {
    public UniqueKey() { }

    public UniqueKey(string name)                                      : base(name) { }
    public UniqueKey(string name, string column)                       : base(name, column) { }
    public UniqueKey(string name, params string[] columns)             : base(name, columns) { }
    public UniqueKey(string name, Type table, params string[] columns) : base(name, table, columns) { }
    public UniqueKey(string name, Type table, string column)           : base(name, table, column) { }
    public UniqueKey(Type table, params string[] columns)              : base(columns.FirstOrDefault(), table, columns) { }
    public UniqueKey(Type table, string column)                        : base(column, table, column) { }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ForeignKey : TableKey {
    public string   ReferencedTable   { get; private set; }
    public string[] ReferencedColumns { get; private set; }
    public string   OnDelete          { get; set; } = "NO ACTION";
    public string   OnUpdate          { get; set; } = "NO ACTION";

    public string ReferencedColumn {
        get {
            return ReferencedColumns.FirstOrDefault();
        }
        set {
            ReferencedColumns = new string[] { value };
        }
    }

    public ForeignKey() { }

    public ForeignKey(string name) : base(name) { }

    public ForeignKey(string name, string column, string referencedTable, string referencedColumn) : base(name) {
        Column           = column;
        ReferencedTable  = referencedTable;
        ReferencedColumn = referencedColumn;
    }

    public ForeignKey(string column, string referencedTable, string referencedColumn) : base(column) {
        Column           = column;
        ReferencedTable  = referencedTable;
        ReferencedColumn = referencedColumn;
    }

    public ForeignKey(string name, Type table, string column, Type referencedTableType, string referencedColumnName) : base(name) {
        string referencedTable  = referencedTableType.GetTableName();
        string referencedColumn = referencedTableType.GetColumnName(referencedColumnName);

        Column           = table.GetColumnName(column);
        ReferencedTable  = referencedTable;
        ReferencedColumn = referencedColumn;
    }

    public ForeignKey(Type table, string column, Type referencedTableType, string referencedColumnName) : base(column) {
        string referencedTable  = referencedTableType.GetTableName();
        string referencedColumn = referencedTableType.GetColumnName(referencedColumnName);

        Column           = table.GetColumnName(column);
        ReferencedTable  = referencedTable;
        ReferencedColumn = referencedColumn;
    }
}

// ----- Indexes -----
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class Index : TableKey {
    public Index() { }
    public Index(string name)                                      : base(name) { }
    public Index(string name, params string[] columns)             : base(name, columns) { }
    public Index(string name, Type table, params string[] columns) : base(name, table, columns) { }
    public Index(string name, Type table, string column)           : base(name, table, column) { }
    public Index(Type table, params string[] columns)              : base(columns.FirstOrDefault(), table, columns) { }
    public Index(Type table, string column)                        : base(column, table, column) { }
}
