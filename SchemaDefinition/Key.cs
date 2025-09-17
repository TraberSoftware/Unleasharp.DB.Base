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

/// <summary>
/// Represents a key for a database table, including its associated columns and index type.
/// </summary>
/// <remarks>This class is used to define a key for a database table, such as a primary key or unique key. It
/// supports specifying one or more columns that make up the key, as well as the type of index associated with the key.
/// The key can be initialized with a single column or multiple columns.</remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public class TableKey : NamedStructure {
    /// <summary>
    /// Gets or sets the collection of column names.
    /// </summary>
    public string[] Columns { get; set; } = new string[0];

    /// <summary>
    /// Gets or sets the first column in the collection.
    /// </summary>
    /// <remarks>Setting this property replaces the entire collection with a single column containing the
    /// specified value.</remarks>
    public string   Column  {
        get {
            return Columns.FirstOrDefault();
        }
        set {
            Columns = new string[] { value };
        }
    }

    /// <summary>
    /// Gets or sets the type of index to be used for the operation.
    /// </summary>
    public IndexType IndexType { get; set; } = IndexType.NONE;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableKey"/> class with the specified column name.
    /// </summary>
    /// <param name="name">The name of the column that serves as the key.</param>
    public TableKey(string name) : base(name) {
        Column = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableKey"/> class with the specified name and column.
    /// </summary>
    /// <param name="name">The name of the table key. Cannot be null or empty.</param>
    /// <param name="column">The name of the column associated with the table key. Cannot be null or empty.</param>
    public TableKey(string name, string column) : base(name) {
        Column = column;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableKey"/> class with the specified name and columns.
    /// </summary>
    /// <remarks>The <paramref name="columns"/> parameter specifies the columns that make up the key. This
    /// constructor is typically used to define primary keys, unique keys, or other table constraints.</remarks>
    /// <param name="name">The name of the table key. Cannot be null or empty.</param>
    /// <param name="columns">An array of column names that define the key. Must contain at least one column.</param>
    public TableKey(string name, params string[] columns) : base(name) {
        Columns = columns;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableKey"/> class with the specified name, table type, and column
    /// names.
    /// </summary>
    /// <remarks>The column names provided in <paramref name="columns"/> are resolved to their corresponding
    /// table-specific column names using the <paramref name="table"/> type.</remarks>
    /// <param name="name">The name of the key. Cannot be null or empty.</param>
    /// <param name="table">The type of the table associated with the key. Cannot be null.</param>
    /// <param name="columns">An array of column names that define the key. Cannot be null or contain null or empty values.</param>
    public TableKey(string name, Type table, params string[] columns) : base(name) {
        Columns = columns.Select(column => ReflectionCache.GetColumnName(table, column)).ToArray();
    }

    public TableKey(string name, Type table, string column) : base(name) {
        Column = ReflectionCache.GetColumnName(table, column);
    }

    public TableKey() { }
}

/// <summary>
/// Represents a database key that can be applied to a table, supporting various configurations such as naming,
/// associated columns, and related table types.
/// </summary>
/// <remarks>This attribute is used to define keys for database tables, such as primary keys or unique keys. It
/// supports specifying the key name, associated columns, and optionally the related table type. Multiple instances of
/// this attribute can be applied to a single class.</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class Key : TableKey {
    /// <summary>
    /// Initializes a new instance of the <see cref="Key"/> class.
    /// </summary>
    public Key() {}

    /// <summary>
    /// Initializes a new instance of the <see cref="Key"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name associated with the key. This value cannot be null or empty.</param>
    public Key(string name)                                      : base(name) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Key"/> class with the specified name and column.
    /// </summary>
    /// <remarks>The <see cref="Key"/> class represents a database key that is associated with a specific
    /// column.</remarks>
    /// <param name="name">The name of the key. This value cannot be null or empty.</param>
    /// <param name="column">The name of the column associated with the key. This value cannot be null or empty.</param>
    public Key(string name, string column)                       : base(name, column) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Key"/> class with the specified name and columns.
    /// </summary>
    /// <remarks>The <see cref="Key"/> class represents a database key, such as a primary key or unique key,
    /// defined by a name and a set of columns. Ensure that the <paramref name="columns"/> parameter includes valid
    /// column names relevant to the context in which the key is used.</remarks>
    /// <param name="name">The name of the key. This value cannot be null or empty.</param>
    /// <param name="columns">An array of column names that define the key. This value cannot be null and must contain at least one element.</param>
    public Key(string name, params string[] columns)             : base(name, columns) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Key"/> class with the specified name, table, and columns.
    /// </summary>
    /// <param name="name">The name of the key. This value cannot be null or empty.</param>
    /// <param name="table">The type representing the table associated with the key. This value cannot be null.</param>
    /// <param name="columns">An array of column names that define the key. This value cannot be null or empty, and each column name must be
    /// non-null and non-empty.</param>
    public Key(string name, Type table, params string[] columns) : base(name, table, columns) { }

    /// <summary>
    /// Represents a key that uniquely identifies a record in a table.
    /// </summary>
    /// <param name="name">The name of the key.</param>
    /// <param name="table">The type of the table associated with the key.</param>
    /// <param name="column">The name of the column associated with the key.</param>
    public Key(string name, Type table, string column)           : base(name, table, column) { }

    /// <summary>
    /// Represents a key that is associated with a specific table and one or more columns.
    /// </summary>
    /// <param name="table">The type of the table to which the key belongs. This parameter cannot be null.</param>
    /// <param name="columns">An array of column names that define the key. At least one column must be specified.</param>
    public Key(Type table, params string[] columns)              : base(columns.FirstOrDefault(), table, columns) { }

    /// <summary>
    /// Represents a key that is associated with a specific table and column.
    /// </summary>
    /// <param name="table">The type of the table to which the key belongs. This parameter cannot be null.</param>
    /// <param name="column">The name of the column associated with the key. This parameter cannot be null or empty.</param>
    public Key(Type table, string column)                        : base(column, table, column) { }
}

/// <summary>
/// Represents a primary key constraint for a database table.
/// </summary>
/// <remarks>This attribute is used to define a primary key for a table in a database schema.  A primary key
/// uniquely identifies each record in a table and can consist of one or more columns.</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PrimaryKey : TableKey {
    /// <summary>
    /// Initializes a new instance of the <see cref="PrimaryKey"/> class.
    /// </summary>
    public PrimaryKey() {}

    /// <summary>
    /// Initializes a new instance of the <see cref="PrimaryKey"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the primary key. This value cannot be null or empty.</param>
    public PrimaryKey(string name)                                      : base(name) { }

    /// <summary>
    /// Represents a primary key constraint for a database table.
    /// </summary>
    /// <param name="name">The name of the primary key constraint.</param>
    /// <param name="column">The name of the column that serves as the primary key.</param>
    public PrimaryKey(string name, string column)                       : base(name, column) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PrimaryKey"/> class with the specified name and columns.
    /// </summary>
    /// <param name="name">The name of the primary key. This value cannot be null or empty.</param>
    /// <param name="columns">An array of column names that define the primary key. At least one column must be specified.</param>
    public PrimaryKey(string name, params string[] columns)             : base(name, columns) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PrimaryKey"/> class, representing the primary key of a database
    /// table.
    /// </summary>
    /// <param name="name">The name of the primary key.</param>
    /// <param name="table">The type of the table to which the primary key belongs.</param>
    /// <param name="columns">The columns that make up the primary key. At least one column must be specified.</param>
    public PrimaryKey(string name, Type table, params string[] columns) : base(name, table, columns) { }

    /// <summary>
    /// Represents the primary key of a database table, defined by its name, associated table type, and column name.
    /// </summary>
    /// <param name="name">The name of the primary key. Cannot be null or empty.</param>
    /// <param name="table">The type of the table to which the primary key belongs. Cannot be null.</param>
    /// <param name="column">The name of the column that serves as the primary key. Cannot be null or empty.</param>
    public PrimaryKey(string name, Type table, string column)           : base(name, table, column) { }

    /// <summary>
    /// Represents the primary key of a database table, defined by one or more column names.
    /// </summary>
    /// <param name="table">The type representing the database table associated with the primary key. Cannot be null.</param>
    /// <param name="columns">An array of column names that constitute the primary key. At least one column name must be provided.</param>
    public PrimaryKey(Type table, params string[] columns)              : base(columns.FirstOrDefault(), table, columns) { }

    /// <summary>
    /// Represents the primary key of a database table, defined by the table type and column name.
    /// </summary>
    /// <param name="table">The type of the table to which the primary key belongs. Cannot be null.</param>
    /// <param name="column">The name of the column that serves as the primary key. Cannot be null or empty.</param>
    public PrimaryKey(Type table, string column)                        : base(column, table, column) { }
}

/// <summary>
/// Represents a unique key constraint for a database table, ensuring that the specified column or combination of
/// columns contains unique values.
/// </summary>
/// <remarks>This attribute can be applied to a class to define one or more unique key constraints for the
/// corresponding database table. Use this attribute to enforce uniqueness on specific columns or sets of columns in the
/// table schema.</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class UniqueKey : TableKey {
    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueKey"/> class.
    /// </summary>
    public UniqueKey() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueKey"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name associated with the unique key. Cannot be null or empty.</param>
    public UniqueKey(string name)                                      : base(name) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueKey"/> class with the specified name and column.
    /// </summary>
    /// <remarks>The <see cref="UniqueKey"/> class represents a unique constraint on a specific column in a
    /// database or data structure.</remarks>
    /// <param name="name">The name of the unique key. This value cannot be null or empty.</param>
    /// <param name="column">The column associated with the unique key. This value cannot be null or empty.</param>
    public UniqueKey(string name, string column)                       : base(name, column) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueKey"/> class with the specified name and columns.
    /// </summary>
    /// <param name="name">The name of the unique key. This value cannot be null or empty.</param>
    /// <param name="columns">An array of column names that define the unique key. This value cannot be null or empty, and must contain at
    /// least one column.</param>
    public UniqueKey(string name, params string[] columns)             : base(name, columns) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueKey"/> class, representing a unique key constraint on a
    /// database table.
    /// </summary>
    /// <remarks>The <see cref="UniqueKey"/> class is used to define a unique key constraint, ensuring that
    /// the specified columns in the table have unique values across all rows.</remarks>
    /// <param name="name">The name of the unique key constraint. Cannot be null or empty.</param>
    /// <param name="table">The type of the table to which the unique key constraint applies. Cannot be null.</param>
    /// <param name="columns">The columns that are part of the unique key constraint. Must contain at least one column and cannot be null.</param>
    public UniqueKey(string name, Type table, params string[] columns) : base(name, table, columns) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueKey"/> class with the specified name, table, and column.
    /// </summary>
    /// <param name="name">The name of the unique key. This value cannot be null or empty.</param>
    /// <param name="table">The type of the table to which the unique key belongs. This value cannot be null.</param>
    /// <param name="column">The name of the column that the unique key is associated with. This value cannot be null or empty.</param>
    public UniqueKey(string name, Type table, string column)           : base(name, table, column) { }

    /// <summary>
    /// Represents a unique key constraint for a database table, defined by one or more columns.
    /// </summary>
    /// <param name="table">The type representing the database table to which the unique key applies. Cannot be <see langword="null"/>.</param>
    /// <param name="columns">An array of column names that define the unique key. At least one column must be specified.  The first column in
    /// the array is used as the primary identifier for the key.</param>
    public UniqueKey(Type table, params string[] columns)              : base(columns.FirstOrDefault(), table, columns) { }

    /// <summary>
    /// Represents a unique key constraint for a specific table and column in a database.
    /// </summary>
    /// <param name="table">The type representing the table to which the unique key constraint applies.</param>
    /// <param name="column">The name of the column that is part of the unique key constraint.</param>
    public UniqueKey(Type table, string column)                        : base(column, table, column) { }
}

/// <summary>
/// Represents a foreign key constraint in a database table, defining a relationship between the current table and a
/// referenced table.
/// </summary>
/// <remarks>This attribute can be applied to a class to specify one or more foreign key constraints. A foreign
/// key establishes a link between a column (or columns) in the current table and a column (or columns) in a referenced
/// table. It also defines optional behaviors for cascading updates and deletions.</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ForeignKey : TableKey {
    /// <summary>
    /// Gets the name of the referenced table in the database.
    /// </summary>
    public string   ReferencedTable   { get; private set; }

    /// <summary>
    /// Gets the names of the columns referenced by this entity.
    /// </summary>
    public string[] ReferencedColumns { get; private set; }

    /// <summary>
    /// Gets or sets the action to be performed when a related entity is deleted.
    /// </summary>
    public string   OnDelete          { get; set; } = "NO ACTION";

    /// <summary>
    /// Gets or sets the action to be performed during an update operation.
    /// </summary>
    public string   OnUpdate          { get; set; } = "NO ACTION";

    /// <summary>
    /// Gets or sets the first referenced column in the collection.
    /// </summary>
    /// <remarks>Setting this property replaces the entire collection of referenced columns with a single
    /// column.</remarks>
    public string ReferencedColumn {
        get {
            return ReferencedColumns.FirstOrDefault();
        }
        set {
            ReferencedColumns = new string[] { value };
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForeignKey"/> class.
    /// </summary>
    public ForeignKey() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForeignKey"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the foreign key. This value cannot be null or empty.</param>
    public ForeignKey(string name) : base(name) { }

    /// <summary>
    /// Represents a foreign key constraint in a database schema, linking a column in the current table to a column in a
    /// referenced table.
    /// </summary>
    /// <param name="name">The name of the foreign key constraint.</param>
    /// <param name="column">The name of the column in the current table that is part of the foreign key.</param>
    /// <param name="referencedTable">The name of the table that the foreign key references.</param>
    /// <param name="referencedColumn">The name of the column in the referenced table that the foreign key points to.</param>
    public ForeignKey(string name, string column, string referencedTable, string referencedColumn) : base(name) {
        Column           = column;
        ReferencedTable  = referencedTable;
        ReferencedColumn = referencedColumn;
    }

    /// <summary>
    /// Represents a foreign key relationship between a column in the current table and a column in a referenced table.
    /// </summary>
    /// <param name="column">The name of the column in the current table that serves as the foreign key.</param>
    /// <param name="referencedTable">The name of the table that the foreign key references.</param>
    /// <param name="referencedColumn">The name of the column in the referenced table that the foreign key points to.</param>
    public ForeignKey(string column, string referencedTable, string referencedColumn) : base(column) {
        Column           = column;
        ReferencedTable  = referencedTable;
        ReferencedColumn = referencedColumn;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForeignKey"/> class, representing a foreign key relationship
    /// between two database tables.
    /// </summary>
    /// <remarks>This constructor resolves the table and column names for both the foreign key and the
    /// referenced table using the provided types and column names. Ensure that the types provided have the appropriate
    /// methods to retrieve table and column names.</remarks>
    /// <param name="name">The name of the foreign key constraint.</param>
    /// <param name="table">The type representing the table that contains the foreign key column.</param>
    /// <param name="column">The name of the column in the table that acts as the foreign key.</param>
    /// <param name="referencedTableType">The type representing the table that the foreign key references.</param>
    /// <param name="referencedColumnName">The name of the column in the referenced table that the foreign key points to.</param>
    public ForeignKey(string name, Type table, string column, Type referencedTableType, string referencedColumnName) : base(name) {
        string referencedTable  = ReflectionCache.GetTableName(referencedTableType);
        string referencedColumn = ReflectionCache.GetColumnName(referencedTableType, referencedColumnName);

        Column           = ReflectionCache.GetColumnName(table, column);
        ReferencedTable  = referencedTable;
        ReferencedColumn = referencedColumn;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForeignKey"/> class, representing a foreign key relationship
    /// between two database tables.
    /// </summary>
    /// <remarks>This constructor establishes a foreign key relationship by associating a column in the source
    /// table with a column in the referenced table. The <paramref name="table"/> and <paramref
    /// name="referencedTableType"/> parameters are expected to provide metadata about the respective tables, such as
    /// their names and column mappings.</remarks>
    /// <param name="table">The type representing the table that contains the foreign key column.</param>
    /// <param name="column">The name of the column in the table that acts as the foreign key.</param>
    /// <param name="referencedTableType">The type representing the table that the foreign key references.</param>
    /// <param name="referencedColumnName">The name of the column in the referenced table that the foreign key points to.</param>
    public ForeignKey(Type table, string column, Type referencedTableType, string referencedColumnName) : base(column) {
        string referencedTable  = ReflectionCache.GetTableName(referencedTableType);
        string referencedColumn = ReflectionCache.GetColumnName(referencedTableType, referencedColumnName);

        Column           = ReflectionCache.GetColumnName(table, column);
        ReferencedTable  = referencedTable;
        ReferencedColumn = referencedColumn;
    }
}

/// <summary>
/// Represents an index on a database table, which can be used to optimize query performance or enforce uniqueness
/// constraints.
/// </summary>
/// <remarks>This attribute is applied to a class to define one or more indexes for the corresponding database
/// table. An index can be defined with a name, a target table, and one or more columns.  Use this attribute to specify
/// indexes that should be created in the database schema.</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class Index : TableKey {
    /// <summary>
    /// Initializes a new instance of the <see cref="Index"/> class.
    /// </summary>
    public Index() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Index"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the index. This value cannot be null or empty.</param>
    public Index(string name)                                      : base(name) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Index"/> class with the specified name and columns.
    /// </summary>
    /// <param name="name">The name of the index. This value cannot be null or empty.</param>
    /// <param name="columns">An array of column names that define the index. This value cannot be null and must contain at least one element.</param>
    public Index(string name, params string[] columns)             : base(name, columns) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Index"/> class with the specified name, table, and columns.
    /// </summary>
    /// <remarks>The <see cref="Index"/> class represents a database index, which is used to improve query
    /// performance by organizing data based on the specified columns.</remarks>
    /// <param name="name">The name of the index. This value cannot be null or empty.</param>
    /// <param name="table">The type representing the table to which the index belongs. This value cannot be null.</param>
    /// <param name="columns">An array of column names that are included in the index. This value cannot be null or empty, and each column
    /// name must be unique.</param>
    public Index(string name, Type table, params string[] columns) : base(name, table, columns) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Index"/> class with the specified name, table, and column.
    /// </summary>
    /// <param name="name">The name of the index. This value cannot be null or empty.</param>
    /// <param name="table">The type representing the table to which the index belongs. This value cannot be null.</param>
    /// <param name="column">The name of the column on which the index is created. This value cannot be null or empty.</param>
    public Index(string name, Type table, string column)           : base(name, table, column) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Index"/> class, representing an index on a table with the specified
    /// columns.
    /// </summary>
    /// <param name="table">The type of the table on which the index is defined. Cannot be <see langword="null"/>.</param>
    /// <param name="columns">An array of column names that define the index. Must contain at least one column.</param>
    public Index(Type table, params string[] columns)              : base(columns.FirstOrDefault(), table, columns) { }

    /// <summary>
    /// Represents an index on a specific column within a table.
    /// </summary>
    /// <remarks>This class is used to define an index for a specific column in a table.  It ensures that the
    /// column is indexed for optimized querying or other database operations.</remarks>
    /// <param name="table">The type of the table that the index is associated with. Cannot be null.</param>
    /// <param name="column">The name of the column that the index is created for. Cannot be null or empty.</param>
    public Index(Type table, string column)                        : base(column, table, column) { }
}
