using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.QueryBuilding;

namespace Unleasharp.DB.Base.SchemaDefinition {
    public class IndexOption : Attribute {
        public string         Name;
        public IndexType      IndexType;
        public IndexExtension IndexExtension;
        public Key            IndexKey;
    }
}
