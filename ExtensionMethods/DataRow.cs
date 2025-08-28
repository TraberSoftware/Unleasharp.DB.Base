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
        string  dbTableName    = typeof(T) .GetCustomAttribute<Table>         ()?.Name ?? typeof(T) .Name;
        string  dbFieldName    = memberInfo.GetCustomAttribute<NamedStructure>()?.Name ?? memberInfo.Name;
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

        // When performing the select, we try to add the table as the prefix to the column name
        // However, we look for the default name without the table prefix for plain queries
        // (this means, RAW queries not built by the query builder)
        string rowFieldName = $"{dbTableName}::{dbFieldName}";
        if (!row.Table.Columns.Contains(rowFieldName)) {
            rowFieldName = dbFieldName;
        }

        // If table does not contain the given field, it makes no sense to try to set it
        if (!row.Table.Columns.Contains(rowFieldName)) {
            return;
        }

        MethodInfo dataRowFieldMethod        = typeof(DataRowExtensions).GetMethod("Field", new[] { typeof(DataRow), typeof(string) });
        MethodInfo dataRowFieldGenericMethod = dataRowFieldMethod.MakeGenericMethod(memberInfoType);

        if (memberInfoType.IsEnum) {
            try {
                string enumValueString = row.Field<string>(rowFieldName);

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
                value = Enum.ToObject(memberInfoType, row.Field<int>(rowFieldName));
            }
        }
        else {
            try {
                value = dataRowFieldGenericMethod.Invoke(null, new object[] { row, rowFieldName });
            }
            // Row.Field<T> is a strongly-typed function, it types do not match 100% it will fail
            // As types can differ between database engines, we try to convert the data types
            catch (Exception ex) {
                try {
                    value = Convert.ChangeType(row[rowFieldName], memberInfoType);
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

    public static Tuple<T1, T2> GetTuple<T1, T2>(this DataRow row)
        where T1 : class
        where T2 : class
    {
        return new Tuple<T1, T2>(
            row.GetObject<T1>(),
            row.GetObject<T2>()
        );
    }

    public static Tuple<T1, T2, T3> GetTuple<T1, T2, T3>(this DataRow row)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        return new Tuple<T1, T2, T3>(
            row.GetObject<T1>(),
            row.GetObject<T2>(),
            row.GetObject<T3>()
        );
    }

    public static Tuple<T1, T2, T3, T4> GetTuple<T1, T2, T3, T4>(this DataRow row)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
    {
        return new Tuple<T1, T2, T3, T4>(
            row.GetObject<T1>(),
            row.GetObject<T2>(),
            row.GetObject<T3>(),
            row.GetObject<T4>()
        );
    }

    public static Tuple<T1, T2, T3, T4, T5> GetTuple<T1, T2, T3, T4, T5>(this DataRow row)
    where T1 : class
    where T2 : class
    where T3 : class
    where T4 : class
    where T5 : class {
        return new Tuple<T1, T2, T3, T4, T5>(
            row.GetObject<T1>(),
            row.GetObject<T2>(),
            row.GetObject<T3>(),
            row.GetObject<T4>(),
            row.GetObject<T5>()
        );
    }

    public static Tuple<T1, T2, T3, T4, T5, T6> GetTuple<T1, T2, T3, T4, T5, T6>(this DataRow row)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class {
        return new Tuple<T1, T2, T3, T4, T5, T6>(
            row.GetObject<T1>(),
            row.GetObject<T2>(),
            row.GetObject<T3>(),
            row.GetObject<T4>(),
            row.GetObject<T5>(),
            row.GetObject<T6>()
        );
    }

    public static Tuple<T1, T2, T3, T4, T5, T6, T7> GetTuple<T1, T2, T3, T4, T5, T6, T7>(this DataRow row)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class {
        return new Tuple<T1, T2, T3, T4, T5, T6, T7>(
            row.GetObject<T1>(),
            row.GetObject<T2>(),
            row.GetObject<T3>(),
            row.GetObject<T4>(),
            row.GetObject<T5>(),
            row.GetObject<T6>(),
            row.GetObject<T7>()
        );
    }

    public static Tuple<T1, T2, T3, T4, T5, T6, T7, T8> GetTuple<T1, T2, T3, T4, T5, T6, T7, T8>(this DataRow row)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class
        where T8 : class {
        return new Tuple<T1, T2, T3, T4, T5, T6, T7, T8>(
            row.GetObject<T1>(),
            row.GetObject<T2>(),
            row.GetObject<T3>(),
            row.GetObject<T4>(),
            row.GetObject<T5>(),
            row.GetObject<T6>(),
            row.GetObject<T7>(),
            row.GetObject<T8>()
        );
    }
    #endregion
}
