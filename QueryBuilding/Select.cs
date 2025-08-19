using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Unleasharp.DB.Base.QueryBuilding {
    public class Select<DBQueryType> where DBQueryType : Unleasharp.DB.Base.Query<DBQueryType> {
        public FieldSelector      Field;
        public Query<DBQueryType> Subquery;
        public string             Alias;
    }
}
