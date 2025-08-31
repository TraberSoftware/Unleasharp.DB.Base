using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.SchemaDefinition;

namespace Unleasharp.DB.Base.ExtensionMethods;
public static class MemberInfoExtensions {
    /// <summary>
    /// Determines whether the specified member is marked as a system column.
    /// </summary>
    /// <param name="member">The <see cref="MemberInfo"/> to inspect for the <see cref="SystemColumn"/> attribute.</param>
    /// <returns><see langword="true"/> if the <see cref="SystemColumn"/> attribute is applied to the specified member;
    /// otherwise, <see langword="false"/>.</returns>
    public static bool IsSystemColumn(this MemberInfo member) {
        return member.GetCustomAttribute<SystemColumn>() != null;
    }

    /// <summary>
    /// Determines whether the specified member represents a readable system column for the given database engine.
    /// </summary>
    /// <param name="member">The <see cref="MemberInfo"/> to evaluate. This represents a member of a type, such as a property or field.</param>
    /// <param name="engine">The <see cref="DatabaseEngine"/> to check against. This specifies the target database engine.</param>
    /// <returns><see langword="true"/> if the member is either not marked as a system column or is marked as a system column for
    /// the specified database engine; otherwise, <see langword="false"/>.</returns>
    public static bool IsReadableSystemColumn(this MemberInfo member, DatabaseEngine engine) {
        SystemColumn? column = member.GetCustomAttribute<SystemColumn>();

        return column == null || column.Engine == engine;
    }
}
