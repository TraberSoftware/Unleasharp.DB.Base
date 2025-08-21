using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
		this.Column = name;
	}

	public TableKey(string name, string column) : base(name) {
        this.Column = column;
	}

	public TableKey(string name, params string[] columns) : base(name) {
		Columns = columns;
	}

	public TableKey() { }
}

// ----- Keys -----
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class Key : TableKey {
	public Key() { }

	public Key(string name) : base(name) { }

	public Key(string name, string column) : base(name) {
        this.Column = column;
	}

	public Key(string name, params string[] columns) : base(name) {
		Columns = columns;
	}
}

// ----- Primary keys -----
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PrimaryKey : TableKey {
	public PrimaryKey() {}

	public PrimaryKey(string name) : base(name) { }

	public PrimaryKey(string name, string column) : base(name) {
        this.Column = column;
	}

	public PrimaryKey(string name, params string[] columns) : base(name) {
		Columns = columns;
	}
}

// ----- Unique keys -----
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class UniqueKey : TableKey {
	public UniqueKey() { }

	public UniqueKey(string name) : base(name) { }

	public UniqueKey(string name, string column) : base(name) {
		this.Column = column;
	}

	public UniqueKey(string name, params string[] columns) : base(name) {
        Columns = columns;
    }
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
}

// ----- Indexes -----
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class Index : TableKey {
	public Index() { }

	public Index(string name) : base(name) { }

	public Index(string name, params string[] columns) : base(name) {
		Columns = columns;
	}
}
