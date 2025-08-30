using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unleasharp.DB.Base.SchemaDefinition;
public class SystemColumn : NamedStructure {

    public DatabaseEngine Engine { get; private set; }

    public SystemColumn(string name, DatabaseEngine engine) : base(name) {
        Engine = engine;
    }
}
