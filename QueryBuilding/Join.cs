using System;
using System.Collections.Generic;
using System.Text;

namespace Unleasharp.DB.Base.QueryBuilding
{
    public class Join<DBQueryType> where DBQueryType : Unleasharp.DB.Base.Query<DBQueryType> {
        public JoinDirection Direction = JoinDirection.INNER;
        public string             Table;
        public Where<DBQueryType> Condition;
        public bool               EscapeTable;
    }
}
