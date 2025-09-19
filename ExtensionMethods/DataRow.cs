using System;
using System.Data;
using System.IO;
using System.Reflection;
using Unleasharp.DB.Base.SchemaDefinition;
using Unleasharp.ExtensionMethods;

namespace Unleasharp.DB.Base.ExtensionMethods;

public static class DataRowExtension {
    /// <summary>
    /// Creates an instance of the specified class type and populates its fields and properties with values from the
    /// provided <see cref="DataRow"/>.
    /// </summary>
    /// <remarks>This method uses reflection to map the column names in the <see cref="DataRow"/> to the
    /// fields and properties of the specified type <typeparamref name="T"/>. Only public fields and properties are
    /// considered. The column names in the <see cref="DataRow"/> must match the names of the fields or properties in
    /// <typeparamref name="T"/> (case-insensitive).</remarks>
    /// <typeparam name="T">The type of the object to create. Must be a reference type with a parameterless constructor.</typeparam>
    /// <param name="row">The <see cref="DataRow"/> containing the data to populate the object's fields and properties.</param>
    /// <returns>An instance of type <typeparamref name="T"/> with its fields and properties populated from the corresponding
    /// column values in the <paramref name="row"/>.</returns>
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
    /// <summary>
    /// Retrieves the data type associated with the specified member.
    /// </summary>
    /// <remarks>This method supports fields and properties. If the provided <paramref name="memberInfo"/> is
    /// neither a <see cref="FieldInfo"/> nor a <see cref="PropertyInfo"/>, the method returns <see
    /// langword="null"/>.</remarks>
    /// <param name="memberInfo">The <see cref="MemberInfo"/> instance representing the member whose data type is to be retrieved. This can be a
    /// field or property.</param>
    /// <returns>The <see cref="Type"/> of the member if it is a field or property; otherwise, <see langword="null"/>.</returns>
    public static Type GetDataType(this MemberInfo memberInfo) {
        return true switch {
            true when  memberInfo == null          => null,
            true when (memberInfo is FieldInfo)    => ((FieldInfo)    memberInfo).FieldType,
            true when (memberInfo is PropertyInfo) => ((PropertyInfo) memberInfo).PropertyType,

            // If anything else than a FieldInfo or PropertyInfo, return null
            _ => null
        };
    }

    /// <summary>
    /// Populates a field or property of a serialized object with the value from a corresponding column in a 
    /// <see cref="DataRow"/>.
    /// </summary>
    /// <remarks>This method attempts to map a column in the <see cref="DataRow"/> to a field or property of
    /// the serialized object based on custom attributes or default naming conventions. It handles nullable types,
    /// enums, and type conversions to ensure compatibility between the database column type and the target member
    /// type.</remarks>
    /// <typeparam name="T">The type of the serialized object being populated.</typeparam>
    /// <param name="row">The <see cref="DataRow"/> containing the data to populate the object.</param>
    /// <param name="memberInfo">The metadata about the field or property to be populated.</param>
    /// <param name="serialized">The object instance whose field or property is being populated.</param>
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
                    // Handle edge cases or assing object as-is. If data types don't match, it will throw an exception on SetValue()
                    value = true switch {
                        true when row[rowFieldName] is DateTime     && memberInfoType == typeof(DateOnly) =>   DateOnly.FromDateTime((DateTime)row[rowFieldName]),
                        true when row[rowFieldName] is DateOnly     && memberInfoType == typeof(DateTime) => ((DateOnly)row[rowFieldName]).ToDateTime(TimeOnly.Parse("00:00:00")),
                        true when row[rowFieldName] is MemoryStream && memberInfoType == typeof(byte[])   => ((MemoryStream)row[rowFieldName]).ToByteArray(),
                        _ => row[rowFieldName]
                    };
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

    /// <summary>
    /// Creates a tuple containing two objects of specified types extracted from the given <see cref="DataRow"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the first object in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T2">The type of the second object in the tuple. Must be a reference type.</typeparam>
    /// <param name="row">The <see cref="DataRow"/> from which the objects are extracted.</param>
    /// <returns>A <see cref="Tuple{T1, T2}"/> containing the extracted objects.  The first item is of type <typeparamref
    /// name="T1"/> and the second item is of type <typeparamref name="T2"/>.</returns>
    public static Tuple<T1, T2> GetTuple<T1, T2>(this DataRow row)
        where T1 : class
        where T2 : class
    {
        return new Tuple<T1, T2>(
            row.GetObject<T1>(),
            row.GetObject<T2>()
        );
    }

    /// <summary>
    /// Creates a tuple containing three objects of specified types from the given <see cref="DataRow"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the first object in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T2">The type of the second object in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T3">The type of the third object in the tuple. Must be a reference type.</typeparam>
    /// <param name="row">The <see cref="DataRow"/> from which the objects are retrieved.</param>
    /// <returns>A <see cref="Tuple{T1, T2, T3}"/> containing three objects of types <typeparamref name="T1"/>,  <typeparamref
    /// name="T2"/>, and <typeparamref name="T3"/> retrieved from the specified <paramref name="row"/>.</returns>
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

    /// <summary>
    /// Creates a <see cref="Tuple{T1, T2, T3, T4}"/> containing values of the specified types extracted from the given
    /// <see cref="DataRow"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the first element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T2">The type of the second element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T3">The type of the third element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T4">The type of the fourth element in the tuple. Must be a reference type.</typeparam>
    /// <param name="row">The <see cref="DataRow"/> from which to extract the values.</param>
    /// <returns>A <see cref="Tuple{T1, T2, T3, T4}"/> containing the extracted values. Each element in the tuple corresponds to
    /// a value of the specified type retrieved from the <paramref name="row"/>.</returns>
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

    /// <summary>
    /// Creates a <see cref="Tuple{T1, T2, T3, T4, T5}"/> containing values of the specified types extracted from the
    /// given <see cref="DataRow"/>.
    /// </summary>
    /// <remarks>This method uses the <c>GetObject&lt;T&gt;</c> extension method on <see cref="DataRow"/> to
    /// retrieve each value. Ensure that the <see cref="DataRow"/> contains values compatible with the specified
    /// types.</remarks>
    /// <typeparam name="T1">The type of the first element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T2">The type of the second element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T3">The type of the third element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T4">The type of the fourth element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T5">The type of the fifth element in the tuple. Must be a reference type.</typeparam>
    /// <param name="row">The <see cref="DataRow"/> from which to extract the values.</param>
    /// <returns>A <see cref="Tuple{T1, T2, T3, T4, T5}"/> containing the extracted values. Each element in the tuple corresponds
    /// to a value of the specified type retrieved from the <paramref name="row"/>.</returns>
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

    /// <summary>
    /// Creates a tuple containing six elements of specified types from the values in the given <see cref="DataRow"/>.
    /// </summary>
    /// <remarks>This method uses the <c>GetObject&lt;T&gt;</c> extension method to retrieve and cast the
    /// values  from the <paramref name="row"/>. Ensure that the <paramref name="row"/> contains values that can  be
    /// cast to the specified types for all tuple elements.</remarks>
    /// <typeparam name="T1">The type of the first element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T2">The type of the second element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T3">The type of the third element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T4">The type of the fourth element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T5">The type of the fifth element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T6">The type of the sixth element in the tuple. Must be a reference type.</typeparam>
    /// <param name="row">The <see cref="DataRow"/> from which to retrieve the values for the tuple elements.</param>
    /// <returns>A <see cref="Tuple{T1, T2, T3, T4, T5, T6}"/> containing six elements of the specified types,  populated from
    /// the corresponding values in the <paramref name="row"/>.</returns>
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

    /// <summary>
    /// Creates a tuple containing seven elements by extracting values from the specified <see cref="DataRow"/>.
    /// </summary>
    /// <remarks>This method uses the <c>GetObject&lt;T&gt;</c> extension method to retrieve and cast values
    /// from the <paramref name="row"/>. Ensure that the <paramref name="row"/> contains values that can be cast to the
    /// specified types; otherwise, an exception will be thrown.</remarks>
    /// <typeparam name="T1">The type of the first element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T2">The type of the second element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T3">The type of the third element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T4">The type of the fourth element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T5">The type of the fifth element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T6">The type of the sixth element in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T7">The type of the seventh element in the tuple. Must be a reference type.</typeparam>
    /// <param name="row">The <see cref="DataRow"/> from which to extract the values for the tuple.</param>
    /// <returns>A <see cref="Tuple{T1, T2, T3, T4, T5, T6, T7}"/> containing the extracted values from the <paramref
    /// name="row"/>. Each element in the tuple corresponds to a value retrieved from the <paramref name="row"/> and
    /// cast to the specified type.</returns>
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

    /// <summary>
    /// Creates a tuple containing up to eight strongly-typed objects retrieved from the specified <see
    /// cref="DataRow"/>.
    /// </summary>
    /// <remarks>This method is an extension method for <see cref="DataRow"/> and allows retrieving multiple
    /// strongly-typed objects from a single row in a database result set. Ensure that the types specified in the
    /// generic parameters match the types of the corresponding columns in the <paramref name="row"/>.</remarks>
    /// <typeparam name="T1">The type of the first object in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T2">The type of the second object in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T3">The type of the third object in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T4">The type of the fourth object in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T5">The type of the fifth object in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T6">The type of the sixth object in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T7">The type of the seventh object in the tuple. Must be a reference type.</typeparam>
    /// <typeparam name="T8">The type of the eighth object in the tuple. Must be a reference type.</typeparam>
    /// <param name="row">The <see cref="DataRow"/> from which the objects are retrieved.</param>
    /// <returns>A <see cref="Tuple{T1, T2, T3, T4, T5, T6, T7, T8}"/> containing the objects retrieved from the <paramref
    /// name="row"/>. Each object is retrieved using the <c>GetObject&lt;T&gt;</c> method of the <paramref name="row"/>.</returns>
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
