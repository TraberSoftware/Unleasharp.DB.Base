using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.QueryBuilding;

namespace Unleasharp.DB.Base.SchemaDefinition;

/// <summary>
/// Represents an attribute that defines options for configuring an index in a database or data structure.
/// </summary>
/// <remarks>This attribute is used to specify metadata for an index, including its name, type, extension, and
/// key. It can be applied to members to indicate how they should be indexed in the underlying system.</remarks>
public class IndexOption : Attribute {
    public string         Name;
    public IndexType      IndexType;
    public IndexExtension IndexExtension;
    public Key            IndexKey;
}
