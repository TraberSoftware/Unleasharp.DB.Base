using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unleasharp.DB.Base.SchemaDefinition;

public class NamedStructure : Attribute {
    public string Name { get; }

    public NamedStructure() { }

    public NamedStructure(string name) {
        this.Name = name;
    }
}
