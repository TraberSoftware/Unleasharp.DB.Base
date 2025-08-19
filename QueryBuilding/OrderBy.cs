using System;
using System.Collections.Generic;
using System.Text;

namespace Unleasharp.DB.Base.QueryBuilding
{
    public class OrderBy {
        public FieldSelector  Field;
        public OrderDirection Direction = OrderDirection.ASC;
    }
}
