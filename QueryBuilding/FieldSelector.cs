using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Unleasharp.DB.Base.QueryBuilding;

public class FieldSelector : Renderable {
    public string Table;
    public string Field;
    public bool   Escape = false;

    public FieldSelector() { }

    public FieldSelector(string field, bool escape = true) {
        this.Field  = field;
        this.Escape = escape;
    }

    public FieldSelector(string table, string field, bool escape = true) {
        this.Table  = table;
        this.Field  = field;
        this.Escape = escape;
    }
}
