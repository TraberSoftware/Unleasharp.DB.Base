using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.QueryBuilding;

namespace Unleasharp.DB.Base.SchemaDefinition;

[AttributeUsage(AttributeTargets.Property)]
public class Column : NamedStructure {
    public ColumnDataType DataType       { get; set; }
    public string         DataTypeString { get; set; }
    public int            Length         { get; set; }
    public int            Precision      { get; set; }
    public bool           Unsigned       { get; set; } = false;
    public bool           NotNull        { get; set; } = false;
    public bool           AutoIncrement  { get; set; } = false;
    public bool           Unique         { get; set; } = false;
    public bool           PrimaryKey     { get; set; } = false;
    public string?        Default        { get; set; }
    public string?        Check          { get; set; }
    public string?        Comment        { get; set; }

    public Column(string name, ColumnDataType dataType) : base(name) {
		DataType       = dataType;
    }

    public Column(string name, string dataType)         : base(name) {
        DataTypeString = dataType;
    }
}

public enum ColumnDataType {
	Boolean,
	Int,
	Int16,
	Int32,
	Int64,
	UInt,
	UInt16,
	UInt32,
	UInt64,
	Decimal,
	Float,
	Double,
	Text,
	Char,
	Varchar,
	Enum,
	Date,
	DateTime,
	Time,
	Timestamp,
	Binary,
	Guid,
	Json,
	Xml
}
