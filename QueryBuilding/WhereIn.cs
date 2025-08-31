using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unleasharp.DB.Base.QueryBuilding;

/// <summary>
/// Represents a condition used in database queries to filter results based on a field and a set of values.
/// </summary>
/// <remarks>This class is typically used to construct "WHERE IN" clauses in database queries, allowing the caller
/// to specify a field and a collection of values to match against. The behavior of the condition can be customized
/// using the <see cref="Comparer"/>, <see cref="Operator"/>, and <see cref="EscapeValue"/> properties.</remarks>
/// <typeparam name="DBQueryType">The type of the database query, which must inherit from <see cref="Unleasharp.DB.Base.Query{DBQueryType}"/>.</typeparam>
public class WhereIn<DBQueryType> where DBQueryType : Unleasharp.DB.Base.Query<DBQueryType> {
    public Query<DBQueryType> Subquery;
    public FieldSelector      Field       = null;
    public List<dynamic>      Values      = new List<dynamic>();

    public WhereComparer      Comparer    = WhereComparer.EQUALS;
    public WhereOperator      Operator    = WhereOperator.AND;

    public bool               EscapeValue = true;
}
