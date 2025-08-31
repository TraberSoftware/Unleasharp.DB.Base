using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Unleasharp.DB.Base.QueryBuilding;

/// <summary>
/// Represents a database query source, including table information and subquery details.
/// </summary>
/// <typeparam name="DBQueryType">The type of the database query, constrained to types derived from <see
/// cref="Unleasharp.DB.Base.Query{DBQueryType}"/>.</typeparam>
public class From<DBQueryType> where DBQueryType : Unleasharp.DB.Base.Query<DBQueryType> {
    public Query<DBQueryType> Subquery;
    public string             Table       = string.Empty;
    public string             TableAlias  = string.Empty;
    public bool               EscapeTable = true;
}
