using System;
using System.Linq;
using System.Reflection;
using Unleasharp.DB.Base.SchemaDefinition;

namespace Unleasharp.DB.Base.ExtensionMethods;
public static class TypeExtensions {
    /// <summary>
    /// Retrieves the table name associated with the specified type.
    /// </summary>
    /// <param name="type">The type for which to retrieve the table name. This type is expected to have a <see cref="Table"/> attribute.</param>
    /// <returns>The name of the table as specified by the <see cref="Table"/> attribute,  or the name of the type if the
    /// attribute is not present.</returns>
    public static string GetTableName(this Type type) {
        return type.GetCustomAttribute<Table>()?.Name ?? type.Name;
    }

    /// <summary>
    /// Retrieves the database column name associated with the specified member of the given type.
    /// </summary>
    /// <remarks>This method uses reflection to check for a <see cref="Column"/> attribute on the specified
    /// member of the given type. If the attribute is present, its <c>Name</c> property is returned. Otherwise, the
    /// original <paramref name="columnName"/> is returned.</remarks>
    /// <param name="type">The type containing the member whose column name is to be retrieved. Cannot be <see langword="null"/>.</param>
    /// <param name="columnName">The name of the member for which the column name is being retrieved. Cannot be <see langword="null"/> or empty.</param>
    /// <returns>The database column name defined by the <see cref="Column"/> attribute on the specified member,  or the original
    /// <paramref name="columnName"/> if no <see cref="Column"/> attribute is found.</returns>
    public static string GetColumnName(this Type type, string columnName) {
        return type.GetMember(columnName)?.FirstOrDefault()?.GetCustomAttribute<Column>()?.Name ?? columnName;
    }

    /// <summary>
    /// Maps a CLR <see cref="Type"/> to its corresponding <see cref="ColumnDataType"/> representation.
    /// </summary>
    /// <remarks>This method supports common CLR types such as <see langword="bool"/>, numeric types (e.g.,
    /// <see langword="int"/>,  <see langword="double"/>), <see langword="string"/>, <see cref="DateTime"/>, <see
    /// cref="TimeSpan"/>, <see cref="Guid"/>,  and <see langword="byte[]"/>. It also supports nullable types and
    /// enumerations. If the type is nullable, the underlying  type is unwrapped before mapping.</remarks>
    /// <param name="type">The CLR type to map. This can be a nullable or non-nullable type.</param>
    /// <returns>The <see cref="ColumnDataType"/> that corresponds to the specified CLR type.</returns>
    /// <exception cref="NotSupportedException">Thrown if the specified <paramref name="type"/> does not have a defined mapping to a <see
    /// cref="ColumnDataType"/>.</exception>
    public static ColumnDataType GetColumnType(this Type type) {
        // unwrap Nullable<T>
        Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType switch {
            Type t when t == typeof(bool)      => ColumnDataType.Boolean,
            Type t when t == typeof(short)     => ColumnDataType.Int16,
            Type t when t == typeof(int)       => ColumnDataType.Int32,
            Type t when t == typeof(long)      => ColumnDataType.Int64,
            Type t when t == typeof(ushort)    => ColumnDataType.UInt16,
            Type t when t == typeof(uint)      => ColumnDataType.UInt32,
            Type t when t == typeof(ulong)     => ColumnDataType.UInt64,
            Type t when t == typeof(float)     => ColumnDataType.Float,
            Type t when t == typeof(double)    => ColumnDataType.Double,
            Type t when t == typeof(decimal)   => ColumnDataType.Decimal,
            Type t when t == typeof(string)    => ColumnDataType.Varchar,
            Type t when t == typeof(DateTime)  => ColumnDataType.DateTime,
            Type t when t == typeof(TimeSpan)  => ColumnDataType.Time,
            Type t when t == typeof(Guid)      => ColumnDataType.Guid,
            Type t when t == typeof(byte[])    => ColumnDataType.Binary,
            Type t when t.IsEnum               => ColumnDataType.Enum,

            _ => throw new NotSupportedException(
                     $"No DbColumnType mapping defined for CLR type {underlyingType.FullName}")
        };
    }

    /// <summary>
    /// Determines whether the specified <see cref="Type"/> represents a signed numeric type.
    /// </summary>
    /// <remarks>This method evaluates both nullable and non-nullable numeric types. If the type is nullable,
    /// it unwraps the underlying type before determining whether it is signed. Signed numeric types include <see
    /// cref="short"/>, <see cref="int"/>, and <see cref="long"/>, while unsigned numeric types include <see
    /// cref="ushort"/>, <see cref="uint"/>, and <see cref="ulong"/>.</remarks>
    /// <param name="type">The <see cref="Type"/> to evaluate. This can be a nullable or non-nullable numeric type.</param>
    /// <returns><see langword="true"/> if the specified <see cref="Type"/> is a signed numeric type; otherwise, <see
    /// langword="false"/>. For non-numeric types, the method defaults to <see langword="true"/>.</returns>
    public static bool IsSigned(this Type type) {
        // unwrap Nullable<T>
        Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType switch {
            Type t when t == typeof(short)     => true,
            Type t when t == typeof(int)       => true,
            Type t when t == typeof(long)      => true,
            Type t when t == typeof(ushort)    => false,
            Type t when t == typeof(uint)      => false,
            Type t when t == typeof(ulong)     => false,
        };

        // If value is not handled, return TRUE by default to avoid "UNSIGNED" keyword
        return true;
    }
}
