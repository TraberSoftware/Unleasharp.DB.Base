using System;
using System.Collections.Generic;
using System.Text;

namespace Unleasharp.DB.Base.QueryBuilding;

/// <summary>
/// Represents a limit configuration with a count and an offset.
/// </summary>
/// <remarks>This class is typically used to define pagination or range-based constraints, where <see
/// cref="Count"/> specifies the maximum number of items to include, and <see cref="Offset"/> specifies the starting
/// position.</remarks>
public class Limit {
    public long Count  = 0;
    public long Offset = 0;
}
