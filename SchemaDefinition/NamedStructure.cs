using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unleasharp.DB.Base.SchemaDefinition;

/// <summary>
/// Represents a custom attribute that associates a name with a structure.
/// </summary>
/// <remarks>This attribute can be applied to classes, methods, or other members to provide a descriptive name.
/// The name can be used for identification or metadata purposes.</remarks>
public class NamedStructure : Attribute {
    /// <summary>
    /// Gets the name associated with the current instance.
    /// </summary>
    public string Name { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NamedStructure"/> class.
    /// </summary>
    public NamedStructure() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NamedStructure"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name to assign to the structure. Cannot be null or empty.</param>
    public NamedStructure(string name) {
        this.Name = name;
    }
}
