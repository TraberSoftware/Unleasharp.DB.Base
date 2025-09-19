using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.SchemaDefinition;

namespace Unleasharp.DB.Base.ExtensionMethods;

public static class ObjectExtensions {
    /// <summary>
    /// Converts the specified object into a dictionary with string keys and dynamic values.
    /// </summary>
    /// <remarks>This method iterates over all public fields and properties of the specified object and
    /// includes them in the resulting dictionary. Fields and properties with the same name will result in a single
    /// entry, with the property value taking precedence.</remarks>
    /// <param name="row">The object to be converted into a dictionary. Each field and property of the object will be added as a key-value
    /// pair.</param>
    /// <returns>A <see cref="Dictionary{TKey, TValue}"/> where the keys are the names of the fields and properties of the
    /// object, and the values are their corresponding values. If the object has no fields or properties, an empty
    /// dictionary is returned.</returns>
    public static Dictionary<string, dynamic> ToDynamicDictionaryForInsert(this object row) {
        Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();

        Type rowType = row.GetType();

        foreach (FieldInfo field in rowType.GetFields()) {
            __HandleObjectMemberInfo(row, field, result, true);
        }

        foreach (PropertyInfo property in rowType.GetProperties()) {
            __HandleObjectMemberInfo(row, property, result, true);
        }

        return result;
    }

    public static Dictionary<string, dynamic> ToDynamicDictionary(this object row) {
        Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();

        Type rowType = row.GetType();

        foreach (FieldInfo field in rowType.GetFields()) {
            __HandleObjectMemberInfo(row, field, result, false);
        }

        foreach (PropertyInfo property in rowType.GetProperties()) {
            __HandleObjectMemberInfo(row, property, result, false);
        }

        return result;
    }

    /// <summary>
    /// Processes the specified member of an object and adds its value to the result dictionary if it meets the required
    /// conditions.
    /// </summary>
    /// <remarks>This method skips processing for system columns and ensures that null values are not added
    /// for primary key columns marked as non-nullable. The database column name is determined using the <see
    /// cref="NamedStructure"/> attribute if present; otherwise, the member's name is used.</remarks>
    /// <param name="row">The object instance containing the member to process.</param>
    /// <param name="memberInfo">The metadata about the member (field or property) to be handled.</param>
    /// <param name="result">A dictionary where the database column name is used as the key, and the corresponding value from the object is
    /// added as the value.</param>
    private static void __HandleObjectMemberInfo(object row, MemberInfo memberInfo, Dictionary<string, dynamic> result, bool excludePrimaryKey = false) {
        // Disable writing to system columns
        if (memberInfo.IsSystemColumn()) {
            return;
        }

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
        if (excludePrimaryKey && ((column != null && (column.PrimaryKey && column.NotNull)) && value == null)) {
            return;
        }

        result.Add(dbColumnName, value);
    }

    /// <summary>
    /// Determines whether the specified object represents a whole number.
    /// </summary>
    /// <remarks>This method checks if the provided object is one of the integral numeric types in .NET.  If
    /// <paramref name="value"/> is <see langword="null"/>, the method returns <see langword="false"/>.</remarks>
    /// <param name="value">The object to evaluate. This can be any type.</param>
    /// <returns><see langword="true"/> if <paramref name="value"/> is a whole number  (e.g., an instance of <see cref="short"/>,
    /// <see cref="ushort"/>, <see cref="int"/>,  <see cref="uint"/>, <see cref="long"/>, or <see cref="ulong"/>);
    /// otherwise, <see langword="false"/>.</returns>
    public static bool IsWholeNumber(this object value) {
        if (value == null) {
            return false;
        }

        return value is short
            || value is ushort
            || value is int
            || value is uint
            || value is long
            || value is ulong
        ;
    }
}
