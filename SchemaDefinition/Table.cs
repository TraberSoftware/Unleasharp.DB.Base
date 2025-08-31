using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.QueryBuilding;

namespace Unleasharp.DB.Base.SchemaDefinition;

/// <summary>
/// Represents a database table with optional configuration for temporary status and conditional creation.
/// </summary>
/// <remarks>This attribute is used to define metadata for a database table, including its name, and whether it is
/// temporary. It is applied to classes to associate them with a specific table structure.</remarks>
[AttributeUsage(AttributeTargets.Class)]
public class Table : NamedStructure {
    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string Name        { get; }


    /// <summary>
    /// Gets or sets a value indicating whether the table is temporary.
    /// </summary>
    public bool   Temporary   { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the create operation should proceed only if the target table does not already exist.
    /// </summary>
    public bool   IfNotExists { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Table"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the table. This value cannot be null or empty.</param>
    public Table(string name) : base(name) {
        Name = name;
    }
}

/// <summary>
/// Represents a check constraint that can be applied to a class, typically used to enforce a condition on data.
/// </summary>
/// <remarks>A check constraint defines a logical expression that must evaluate to <see langword="true"/> for the
/// data to be considered valid. The <see cref="Expression"/> property specifies the condition, and the
/// <see cref="Enforced"/> property determines whether the constraint is actively enforced.</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CheckConstraint : NamedStructure {
    /// <summary>
    /// Gets the SQL expression represented as a string.
    /// </summary>
    public string Expression { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the associated rule or policy is enforced.
    /// </summary>
    public bool   Enforced   { get; set; } = true;

    /// <summary>
    /// Represents a database check constraint, which enforces a condition that must be true for all rows in a table.
    /// </summary>
    /// <param name="name">The name of the check constraint. Cannot be null or empty.</param>
    /// <param name="expression">The SQL expression that defines the condition for the check constraint. Cannot be null or empty.</param>
    public CheckConstraint(string name, string expression) : base(name) {
        Expression = expression;
    }
}

/// <summary>
/// Represents a configuration option for a table, defined by a name and a value.
/// </summary>
/// <remarks>This attribute can be applied to classes to specify custom table options.  Multiple instances of this
/// attribute can be applied to the same class.</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class TableOption : NamedStructure {
    /// <summary>
    /// Gets the option represented as a string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableOption"/> class with the specified name and value.
    /// </summary>
    /// <remarks>The <paramref name="name"/> parameter is passed to the base class constructor, while the
    /// <paramref name="value"/> parameter is assigned to the <see cref="Value"/> property.</remarks>
    /// <param name="name">The name of the table option. This value cannot be null or empty.</param>
    /// <param name="value">The value associated with the table option. This value cannot be null or empty.</param>
    public TableOption(string name, string value) : base(name) {
        Value = value;
    }
}
