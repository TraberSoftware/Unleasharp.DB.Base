using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Unleasharp.DB.Base.QueryBuilding;

/// <summary>
/// Represents a selection operation in a database query, allowing the specification of fields, subqueries, and an
/// alias.
/// </summary>
/// <typeparam name="DBQueryType">The type of the database query, constrained to be a subclass of <see cref="Unleasharp.DB.Base.Query{T}"/>.</typeparam>
public class Select<DBQueryType> where DBQueryType : Unleasharp.DB.Base.Query<DBQueryType> {
    public FieldSelector      Field;
    public Query<DBQueryType> Subquery;
    public string             Alias;
}
