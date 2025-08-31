using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unleasharp.DB.Base.SchemaDefinition;

/// Represents a system-defined column in a database, identified by name and associated with a specific database engine.
/// </summary>
/// <remarks>This class is used to define columns that are part of the system schema in a database.  It provides
/// the name of the column and the database engine it is associated with.</remarks>
public class SystemColumn : NamedStructure {
    /// <summary>
    /// Gets the database engine that this system column is associated with.
    /// </summary>
    /// <remarks>
    /// The <c>Engine</c> property indicates the specific database engine context for the column.
    /// For example, if <c>Engine</c> is <see cref="DatabaseEngine.PostgreSQL"/>, this column is only relevant for PostgreSQL databases.
    /// When executing queries, columns whose <c>Engine</c> does not match the current database engine (e.g., a PostgreSQL column on a SQLite engine)
    /// will not be selected or included in the operation.
    /// </remarks>
    public DatabaseEngine Engine { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemColumn"/> class with the specified name and database engine.
    /// </summary>
    /// <remarks>The <paramref name="name"/> parameter is used to uniquely identify the column, while the
    /// <paramref name="engine"/> parameter specifies the database engine context.</remarks>
    /// <param name="name">The name of the system column. Cannot be null or empty.</param>
    /// <param name="engine">The <see cref="DatabaseEngine"/> associated with the system column.</param>
    public SystemColumn(string name, DatabaseEngine engine) : base(name) {
        Engine = engine;
    }
}
