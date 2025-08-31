using System;
using System.Collections.Generic;
using System.Text;

namespace Unleasharp.DB.Base.QueryBuilding;

/// <summary>
/// Represents a condition used to filter results in a database query.
/// </summary>
/// <remarks>The <see cref="Where{DBQueryType}"/> class allows specifying a field, a comparison operator, and a
/// value to construct a filtering condition. It also supports logical operators to combine multiple
/// conditions.</remarks>
/// <typeparam name="DBQueryType">The type of the database query, which must inherit from <see cref="Unleasharp.DB.Base.Query{DBQueryType}"/>.</typeparam>
public class Where<DBQueryType> where DBQueryType : Unleasharp.DB.Base.Query<DBQueryType> {
    public Query<DBQueryType> Subquery;
    public FieldSelector      Field           = null;
    public FieldSelector      ValueField      = null;
    public dynamic            Value           = string.Empty;

    public WhereComparer      Comparer       = WhereComparer.EQUALS;
    public WhereOperator      Operator       = WhereOperator.AND;
                                        
    public bool               EscapeValue    = true;
}
