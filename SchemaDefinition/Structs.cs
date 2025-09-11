using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unleasharp.DB.Base.SchemaDefinition;

/// <summary>
/// Represents the supported database engines for use in database-related operations.
/// </summary>
/// <remarks>This enumeration defines the types of database engines that can be used in the library.</remarks>
public enum DatabaseEngine {
    MySQL,
    SQLite,
    PostgreSQL,
    MSSQL,
    DuckDB
}
