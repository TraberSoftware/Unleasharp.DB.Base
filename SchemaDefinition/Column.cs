using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.QueryBuilding;

namespace Unleasharp.DB.Base.SchemaDefinition;

[AttributeUsage(AttributeTargets.Property)]
public class Column : NamedStructure {
    public string  DataType      { get; }
    public int     Length        { get; set; }
    public int     Precision     { get; set; }
    public bool    Unsigned      { get; set; } = false;
    public bool    NotNull       { get; set; } = false;
    public bool    AutoIncrement { get; set; } = false;
    public bool    Unique        { get; set; } = false;
    public bool    PrimaryKey    { get; set; } = false;
    public string? Default       { get; set; }
    public string? Comment       { get; set; }

    public Column(string name, string dataType) : base(name) {
        DataType = dataType;
    }
}

// ----- Indexes -----
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class Index : NamedStructure {
    public string[] Columns { get; }
    public string   Type    { get; set; } = "BTREE";

    public Index(string name, params string[] columns) : base(name) {
        Columns = columns;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class UniqueKey : NamedStructure {
    public string[] Columns { get; }

    public UniqueKey(string name, params string[] columns) : base(name) {
        Columns = columns;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ForeignKey : NamedStructure {
    public string[] Columns           { get; private set; }
    public string   ReferencedTable   { get; private set; }
    public string[] ReferencedColumns { get; private set; }
    public string   OnDelete          { get; set; } = "NO ACTION";
    public string   OnUpdate          { get; set; } = "NO ACTION";

	public string Column {
        get {
            return Columns.FirstOrDefault();
        }
		set {
            Columns = new string[] { value };
		}
	}

	public string ReferencedColumn {
        get {
            return ReferencedColumns.FirstOrDefault();
        }
		set {
			ReferencedColumns = new string[] { value };
		}
	}

	public ForeignKey(string name, string column, string referencedTable, string referencedColumn) : base(name) {
        Column           = column;
        ReferencedTable  = referencedTable;
        ReferencedColumn = referencedColumn;
    }
}
