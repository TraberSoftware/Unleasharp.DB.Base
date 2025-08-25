using System;
using System.Data;
using System.Reflection;
using Unleasharp.DB.Base.SchemaDefinition;
using Unleasharp.ExtensionMethods;

namespace Unleasharp.DB.Base.ExtensionMethods;

public static class DataRowExtension {
    public static T GetObject<T>(this DataRow row) where T : class {
        T serialized = Activator.CreateInstance<T>();

        foreach (FieldInfo field in typeof(T).GetFields()) {
            __HandleRowMemberInfo<T>(row, field, serialized);
        }
        foreach (PropertyInfo property in typeof(T).GetProperties()) {
            __HandleRowMemberInfo<T>(row, property, serialized);
        }

        return serialized;
    }

    #region Helpers
    public static Type GetDataType(this MemberInfo memberInfo) {
        return true switch {
            true when  memberInfo == null          => null,
            true when (memberInfo is FieldInfo)    => ((FieldInfo)    memberInfo).FieldType,
            true when (memberInfo is PropertyInfo) => ((PropertyInfo) memberInfo).PropertyType,

            // If anything else than a FieldInfo or PropertyInfo, return null
            _ => null
        };
    }

    private static void __HandleRowMemberInfo<T>(DataRow row, MemberInfo memberInfo, T serialized) {
        string  dbFieldName    = memberInfo.Name;
        string  classFieldName = memberInfo.Name;
        Type    memberInfoType = memberInfo.GetDataType();
        object  value          = null;
        
        // Not a Field nor a Property
        // Shouldn't handle
        if (memberInfoType == null) {
            return;
        }

        if (Nullable.GetUnderlyingType(memberInfoType) != null) {
            memberInfoType = Nullable.GetUnderlyingType(memberInfoType);
        }

        Attribute propertyAttribute = memberInfo.GetCustomAttribute<NamedStructure>();
        if (propertyAttribute != null) {
            dbFieldName = ((NamedStructure)propertyAttribute).Name;
        }

        // If table does not contain the given field, it makes no sense to try to set it
        if (!row.Table.Columns.Contains(dbFieldName)) {
            return;
        }

        MethodInfo dataRowFieldMethod        = typeof(DataRowExtensions).GetMethod("Field", new[] { typeof(DataRow), typeof(string) });
        MethodInfo dataRowFieldGenericMethod = dataRowFieldMethod.MakeGenericMethod(memberInfoType);

        if (memberInfoType.IsEnum) {
            try {
                string enumValueString = row.Field<string>(dbFieldName);

                if (enumValueString != null) {
                    foreach (Enum enumValue in Enum.GetValues(memberInfoType)) {
                        string enumDescription = enumValue.GetDescription();
                        if (
                            enumDescription == enumValueString
                        ) {
                            value = enumValue;
                            break;
                        }
                    }
                }
            }
            catch(InvalidCastException ex) {
                // When value of enum comes from an integer, we can't retrieve the string value
                // we should try to cast to enum by using the integer value
                // If this fails, I don't know what could we do
                value = Enum.ToObject(memberInfoType, row.Field<int>(dbFieldName));
            }
        }
        else {
            try {
                value = dataRowFieldGenericMethod.Invoke(null, new object[] { row, dbFieldName });
            }
            // Row.Field<T> is a strongly-typed function, it types do not match 100% it will fail
            // As types can differ between database engines, we try to convert the data types
            catch (Exception ex) {
                try {
                    value = Convert.ChangeType(row[dbFieldName], memberInfoType);
                }
                catch (Exception cex) {
                    // We tried, but
                    throw cex;
                }
            }
        }

        if (memberInfo is FieldInfo) {
            ((FieldInfo) memberInfo).SetValue(
                serialized,
                value
            );
        }
        if (memberInfo is PropertyInfo) {
            ((PropertyInfo) memberInfo).SetValue(
                serialized,
                value
            );
        }
    }
    #endregion
}
