using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.QueryBuilding;

namespace Unleasharp.DB.Base.SchemaDefinition {
    public class Key : NamedStructure {
        public string Field{
            get { return Fields.FirstOrDefault();  }
            set { Fields = new string[] { value }; }
        }
        public string[]      Fields;
        public FieldSelector References;
        public KeyType       KeyType;
        public IndexType     IndexType = IndexType.BTREE;
        public string        OnDelete  = "NO ACTION";
        public string        OnUpdate  = "NO ACTION";

        public Key(string KeyName) : base(KeyName) {
        }

        public Key() { }
    }
}
