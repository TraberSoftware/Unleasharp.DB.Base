using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.SchemaDefinition;

namespace Unleasharp.DB.Base.ExtensionMethods;
public static class MemberInfoExtensions {
    public static bool IsSystemColumn(this MemberInfo member) {
        return member.GetCustomAttribute<SystemColumn>() != null;
    }

    public static bool IsReadableSystemColumn(this MemberInfo member, DatabaseEngine engine) {
        SystemColumn? column = member.GetCustomAttribute<SystemColumn>();

        return column == null || column.Engine == engine;
    }
}
