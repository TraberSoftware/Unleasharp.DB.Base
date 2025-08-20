using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.QueryBuilding;

namespace Unleasharp.DB.Base.SchemaDefinition;

[AttributeUsage(AttributeTargets.Class)]
public class Table : NamedStructure {
    public string Name        { get; }
    public bool   Temporary   { get; set; }
    public bool   IfNotExists { get; set; }

    public Table(string name) : base(name) {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CheckConstraint : NamedStructure {
    public string Expression { get; }
    public bool   Enforced   { get; set; } = true;

    public CheckConstraint(string name, string expression) : base(name) {
        Expression = expression;
    }
}

// ----- Table Options -----
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class TableOption : NamedStructure {
    public string Value { get; }

    public TableOption(string name, string value) : base(name) {
        Value = value;
    }
}
