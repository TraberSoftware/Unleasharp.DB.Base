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

    public FieldSelector(string Field, bool Escape = true) {
        this.Field  = Field;
        this.Escape = Escape;
    }

    public FieldSelector(string Table, string Field, bool Escape = true) {
        this.Table  = Table;
        this.Field  = Field;
        this.Escape = Escape;
    }
}
