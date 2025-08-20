using System;
using Unleasharp.DB.Base.SchemaDefinition;

namespace Unleasharp.DB.Base.ExtensionMethods;
public static class TypeExtensions {
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
            //Type t when t == typeof(object)    => ColumnType.Text,

            _ => throw new NotSupportedException(
                     $"No DbColumnType mapping defined for CLR type {underlyingType.FullName}")
        };
    }

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
