using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.ExtensionMethods;
using Unleasharp.DB.Base.QueryBuilding;

namespace Unleasharp.DB.Base.SchemaDefinition;

/// <summary>
/// Represents a database column with metadata such as data type, constraints, and additional attributes.
/// </summary>
/// <remarks>This attribute is applied to properties to define their mapping to database columns. It provides
/// metadata such as the column's data type, length, precision, constraints (e.g., primary key, unique), and other
/// attributes like default values and comments. The column can be defined using either a strongly-typed <see
/// cref="ColumnDataType"/> or a string representation of the data type.</remarks>
[AttributeUsage(AttributeTargets.Property)]
public class Column : NamedStructure {
    public ColumnDataType? DataType       { get; set; }
    public  string         DataTypeString { get; set; }
    public int             Length         { get; set; }
    public int             Precision      { get; set; }
    public bool            Unsigned       { get; set; } = false;
    public bool            NotNull        { get; set; } = false;
    public bool            AutoIncrement  { get; set; } = false;
    public bool            Unique         { get; set; } = false;
    public bool            PrimaryKey     { get; set; } = false;
    public string?         Default        { get; set; }
    public string?         Check          { get; set; }
    public string?         Comment        { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Column"/> class with the specified name and data type.
    /// </summary>
    /// <param name="name">The name of the column. This value cannot be null or empty.</param>
    /// <param name="dataType">The data type of the column, specifying the type of data the column will hold.</param>
    public Column(string name, ColumnDataType dataType) : base(name) {
        DataType       = dataType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Column"/> class with the specified name and data type.
    /// </summary>
    /// <remarks>The <paramref name="dataType"/> parameter should represent a valid data type in the context
    /// where the column is used.</remarks>
    /// <param name="name">The name of the column. This value cannot be null or empty.</param>
    /// <param name="dataType">The data type of the column as a string. This value cannot be null or empty.</param>
    public Column(string name, string dataType)         : base(name) {
        DataTypeString = dataType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Column"/> class, representing a database column associated with a
    /// specific table and property.
    /// </summary>
    /// <remarks>The constructor uses the provided <paramref name="tableType"/> and <paramref
    /// name="propertyName"/> to determine the fully qualified column name in the format "TableName::ColumnName". Ensure
    /// that the <paramref name="tableType"/> has appropriate metadata to resolve table and column names.</remarks>
    /// <param name="tableType">The type of the table that contains the column. This type must have metadata that provides the table name.</param>
    /// <param name="propertyName">The name of the property corresponding to the column. This must match a property defined in the specified table
    /// type.</param>
    public Column(Type tableType, string propertyName) {
        string tableName  = tableType.GetTableName();
        string columnName = tableType.GetColumnName(propertyName);

        this.Name = $"{tableName}::{columnName}";
    }
}

/// <summary>
/// Represents the data type of a column in a database or data structure.
/// </summary>
/// <remarks>This enumeration defines a comprehensive set of data types that can be used to describe the type of
/// data stored in a column. It includes common primitive types (e.g., <see cref="Int32"/>, <see cref="Boolean"/>), text
/// types (e.g., <see cref="Text"/>, <see cref="Varchar"/>), date and time types (e.g., <see cref="Date"/>, <see
/// cref="DateTime"/>), and specialized types (e.g., <see cref="Json"/>, <see cref="Xml"/>).</remarks>
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
