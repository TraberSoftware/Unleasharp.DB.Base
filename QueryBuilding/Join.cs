using System;
using System.Collections.Generic;
using System.Text;

namespace Unleasharp.DB.Base.QueryBuilding;

/// <summary>
/// Represents a join operation in a database query, specifying the join direction, target table, and join condition.
/// </summary>
/// <typeparam name="DBQueryType">The type of the database query, constrained to a type derived from <see cref="Unleasharp.DB.Base.Query{T}"/>.</typeparam>
public class Join<DBQueryType> where DBQueryType : Unleasharp.DB.Base.Query<DBQueryType> {
    public JoinDirection Direction = JoinDirection.INNER;
    public string             Table;
    public Where<DBQueryType> Condition;
    public bool               EscapeTable;
}
