using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.SchemaDefinition;

namespace Unleasharp.DB.Base.ExtensionMethods;

public static class Object {
    public static Dictionary<string, dynamic> ToDynamicDictionary(this object row) {
        Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();

        Type rowType = row.GetType();

        foreach (FieldInfo field in rowType.GetFields()) {
            __HandleObjectMemberInfo(row, field, result);
        }

        foreach (PropertyInfo property in rowType.GetProperties()) {
            __HandleObjectMemberInfo(row, property, result);
        }

        return result;
    }

    private static void __HandleObjectMemberInfo(object row, MemberInfo memberInfo, Dictionary<string, dynamic> result) {
        string         dbColumnName = memberInfo.Name;
        NamedStructure attribute    = memberInfo.GetCustomAttribute<NamedStructure>();
        Column         column       = memberInfo.GetCustomAttribute<Column>();
        object?        value        = null;

        if (attribute != null) {
            dbColumnName = attribute.Name;
        }

        if (memberInfo is FieldInfo) {
            value = ((FieldInfo)   memberInfo).GetValue(row);
        }
        if (memberInfo is PropertyInfo) {
            value = ((PropertyInfo)memberInfo).GetValue(row);
        }

        // Don't set null values to Primary Key columns
        // HOWEVER, be careful when mixing null and not-null values of Primary Key columns in the same insert
        if ((column != null && (column.PrimaryKey && column.NotNull)) && value == null) {
            return;
        }

        result.Add(dbColumnName, value);
    }
}
