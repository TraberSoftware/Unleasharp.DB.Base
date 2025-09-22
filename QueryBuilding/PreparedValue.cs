using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unleasharp.DB.Base.QueryBuilding;

/// <summary>
/// Represents a value that can be prepared for use in a specific context,  with an option to indicate whether the value
/// should be escaped.
/// </summary>
/// <remarks>This class is typically used to encapsulate a value and its associated  escaping behavior, allowing
/// consumers to handle the value appropriately based on the <see cref="EscapeValue"/> property.</remarks>
public class PreparedValue {
    public dynamic Value;
    public bool    EscapeValue;
}
