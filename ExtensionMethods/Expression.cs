using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.QueryBuilding;
using Unleasharp.DB.Base.SchemaDefinition;

namespace Unleasharp.DB.Base.ExtensionMethods;
public static class ExpressionExtensions {
    /// <summary>
    /// Retrieves a custom attribute of the specified type that is applied to a member of the specified table type.
    /// </summary>
    /// <remarks>This method uses reflection to locate the member specified by the expression and retrieve the
    /// custom attribute of the specified type. Ensure that the member exists and that the attribute is applied to it;
    /// otherwise, the method will return <see langword="null"/>.</remarks>
    /// <typeparam name="TableType">The type representing the table containing the member.</typeparam>
    /// <typeparam name="AttributeType">The type of the attribute to retrieve. Must derive from <see cref="System.Attribute"/>.</typeparam>
    /// <param name="expression">An expression identifying the member of the table type for which the attribute is to be retrieved. Typically,
    /// this is a lambda expression selecting a property or field, such as <c>x => x.PropertyName</c>.</param>
    /// <returns>The attribute of type <typeparamref name="AttributeType"/> applied to the specified member, or <see
    /// langword="null"/> if no such attribute is found.</returns>
    public static AttributeType GetAttribute<TableType, AttributeType>(this Expression<Func<TableType, object>> expression)
        where TableType     : class
        where AttributeType : Attribute 
    {
        string tableName  = ReflectionCache.GetTableName<TableType>();
        string memberName = ExpressionHelper.ExtractClassMemberName<TableType>(expression);

        MemberInfo? member = typeof(TableType).GetMember(memberName)?.FirstOrDefault();
        if (member != null) {
            return member.GetCustomAttribute<AttributeType>();
        }

        return null;
    }

    /// <summary>
    /// Retrieves metadata information about a member of the specified type based on the provided expression.
    /// </summary>
    /// <remarks>This method is typically used to retrieve metadata about a property or field of a type for
    /// scenarios such as reflection-based mapping or dynamic queries.</remarks>
    /// <typeparam name="T">The type that contains the member to retrieve.</typeparam>
    /// <param name="expression">An expression that specifies the member to retrieve. The expression should represent a property or field of the
    /// type <typeparamref name="T"/>.</param>
    /// <returns>A <see cref="MemberInfo"/> object representing the member specified in the expression, or <see langword="null"/>
    /// if no matching member is found.</returns>
    public static MemberInfo GetMember<T>(this Expression<Func<T, object>> expression) where T : class {
        string tableName  = ReflectionCache.GetTableName<T>();
        string columnName = ReflectionCache.GetColumnName<T>(expression);

        return typeof(T).GetMember(columnName)?.FirstOrDefault();
    }

    /// <summary>
    /// Creates a <see cref="FieldSelector"/> for the specified column in the table associated with the type
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>This method uses the type <typeparamref name="T"/> to determine the table name and the
    /// provided expression to extract the column name. The resulting <see cref="FieldSelector"/> is configured to
    /// include the table name in the selection.</remarks>
    /// <typeparam name="T">The type representing the table from which the column is selected.</typeparam>
    /// <param name="expression">An expression that specifies the column to select. The expression should reference a property or field of
    /// <typeparamref name="T"/>.</param>
    /// <returns>A <see cref="FieldSelector"/> representing the selected column, including its table name and column name.</returns>
    public static FieldSelector GetFieldSelector<T>(this Expression<Func<T, object>> expression) where T : class {
        string tableName  = ReflectionCache.GetTableName<T>();
        string columnName = ReflectionCache.GetColumnName<T>(expression);

        return new FieldSelector(tableName, columnName, true);
    }
}
