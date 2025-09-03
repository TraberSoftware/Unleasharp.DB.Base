using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Unleasharp.DB.Base.QueryBuilding;


/// <summary>
/// Represents a union operation in a database query.
/// </summary>
/// <typeparam name="DBQueryType">The type of the database query, which must inherit from <see cref="Unleasharp.DB.Base.Query{DBQueryType}"/>.</typeparam>
public class Union<DBQueryType> where DBQueryType : Unleasharp.DB.Base.Query<DBQueryType> {
    public Query<DBQueryType> Query;
    public UnionType          Type;
}
