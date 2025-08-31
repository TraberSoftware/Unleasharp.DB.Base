using System;
using System.Collections.Generic;
using System.Text;

namespace Unleasharp.DB.Base.QueryBuilding;

/// <summary>
/// Represents the criteria for ordering a collection of items by a specific field and direction.
/// </summary>
/// <remarks>Use this class to specify the field and direction for sorting operations.  The <see cref="Field"/>
/// property determines the field to sort by, and the <see cref="Direction"/>  property specifies whether the sorting is
/// in ascending or descending order.</remarks>
public class OrderBy {
    public FieldSelector  Field;
    public OrderDirection Direction = OrderDirection.ASC;
}
