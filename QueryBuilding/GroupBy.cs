using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unleasharp.DB.Base.QueryBuilding;

/// <summary>
/// Represents a grouping operation based on a specified field.
/// </summary>
/// <remarks>This class is used to define the field by which data should be grouped. The <see cref="Field"/>
/// property specifies the field to group by.</remarks>
public class GroupBy {
    public FieldSelector Field;
}
