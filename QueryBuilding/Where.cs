using System;
using System.Collections.Generic;
using System.Text;

namespace Unleasharp.DB.Base.QueryBuilding;

public class Where<DBQueryType> where DBQueryType : Unleasharp.DB.Base.Query<DBQueryType> {
    public Query<DBQueryType> Subquery;
    public FieldSelector      Field           = null;
    public FieldSelector      ValueField      = null;
    public dynamic            Value           = string.Empty;

    public WhereComparer      Comparer       = WhereComparer.EQUALS;
    public WhereOperator      Operator       = WhereOperator.AND;
                                        
    public bool               EscapeValue    = true;
}
