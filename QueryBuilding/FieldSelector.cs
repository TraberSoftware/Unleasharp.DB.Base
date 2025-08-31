using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Unleasharp.DB.Base.QueryBuilding;

/// <summary>
/// Represents a field selector that specifies a database table and field, with optional escaping for identifiers.
/// </summary>
/// <remarks>This class is used to define a field in a database query, optionally including the table name and
/// whether the field name should be escaped. Escaping is typically used to ensure that field names are treated as
/// literals in database queries, avoiding conflicts with reserved keywords or special characters.</remarks>
public class FieldSelector : Renderable {
    public string Table;
    public string Field;
    public bool   Escape = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldSelector"/> class.
    /// </summary>
    public FieldSelector() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldSelector"/> class with the specified field name and an
    /// optional escape flag.
    /// </summary>
    /// <param name="field">The name of the field to be selected. This value cannot be null or empty.</param>
    /// <param name="escape">A value indicating whether the field name should be escaped to prevent SQL injection or reserved
    /// keyword conflicts. Defaults to <see langword="true"/>.</param>
    public FieldSelector(string field, bool escape = true) {
        this.Field  = field;
        this.Escape = escape;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldSelector"/> class, representing a field selection in a
    /// database query.
    /// </summary>
    /// <param name="table">The name of the database table containing the field. Cannot be null or empty.</param>
    /// <param name="field">The name of the field to select. Cannot be null or empty.</param>
    /// <param name="escape">A value indicating whether the table and field names should be escaped to prevent SQL injection or reserved
    /// keyword conflicts. Defaults to <see langword="true"/>.</param>
    public FieldSelector(string table, string field, bool escape = true) {
        this.Table  = table;
        this.Field  = field;
        this.Escape = escape;
    }
}
