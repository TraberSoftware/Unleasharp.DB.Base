using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Unleasharp.DB.Base.QueryBuilding;

public class From<DBQueryType> where DBQueryType : Unleasharp.DB.Base.Query<DBQueryType> {
    public Query<DBQueryType> Subquery;
    public string             Table       = string.Empty;
    public string             TableAlias  = string.Empty;
    public bool               EscapeTable = true;
}
