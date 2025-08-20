using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unleasharp.DB.Base.QueryBuilding;

public class WhereIn<DBQueryType> where DBQueryType : Unleasharp.DB.Base.Query<DBQueryType> {
    public Query<DBQueryType> Subquery;
    public FieldSelector      Field       = null;
    public List<dynamic>      Values      = new List<dynamic>();

    public WhereComparer      Comparer    = WhereComparer.EQUALS;
    public WhereOperator      Operator    = WhereOperator.AND;

    public bool               EscapeValue = true;
}
