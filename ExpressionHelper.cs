using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.SchemaDefinition;

namespace Unleasharp.DB.Base;
public static class ExpressionHelper {
    /// <summary>
    /// Extracts the name of a database column from a specified property or field expression.
    /// </summary>
    /// <remarks>This method is typically used to retrieve the column name for use in database queries  or
    /// mappings. If the property or field is decorated with a <see cref="Column"/> attribute,  the value of the
    /// <c>Name</c> property of the attribute is returned. Otherwise, the  property or field name itself is
    /// returned.</remarks>
    /// <typeparam name="T">The type of the entity containing the column.</typeparam>
    /// <param name="columnExpression">An expression that specifies the property or field representing the column.  For example, <c>x =>
    /// x.PropertyName</c>.</param>
    /// <returns>The name of the column as defined by the <see cref="Column"/> attribute, if present;  otherwise, the name of the
    /// property or field. Returns <see langword="null"/> if the  expression is invalid or cannot be resolved.</returns>
    public static string ExtractColumnName<T>(Expression<Func<T, object>> columnExpression) {
        if (columnExpression?.Body == null) {
            return null;
        }

        var memberExpression = GetMemberExpression(columnExpression.Body);

        if (memberExpression != null) {
            Type       rowType   = typeof(T);
            MemberInfo rowMember = rowType.GetMember(memberExpression.Member.Name).FirstOrDefault();
            if (rowMember != null) {
                Column columnAttribute = rowMember.GetCustomAttribute<Column>();
                if (columnAttribute != null) {
                    return columnAttribute.Name;
                }
            }

            return memberExpression.Member.Name;
        }

        return null;
    }

    /// <summary>
    /// Extracts the name of a class field or property from a given expression.
    /// </summary>
    /// <typeparam name="T">The type of the class containing the field or property.</typeparam>
    /// <param name="columnExpression">An expression that specifies the field or property to extract the name from.  Typically, this is a lambda
    /// expression such as <c>x => x.PropertyName</c>.</param>
    /// <returns>The name of the field or property specified in the expression, or <see langword="null"/>  if the expression is
    /// <see langword="null"/> or does not represent a member access.</returns>
    public static string ExtractClassFieldName<T>(Expression<Func<T, object>> columnExpression) {
        if (columnExpression?.Body == null) {
            return null;
        }

        var memberExpression = GetMemberExpression(columnExpression.Body);

        if (memberExpression != null) {
            return memberExpression.Member.Name;
        }

        return null;
    }

    /// <summary>
    /// Extracts a <see cref="MemberExpression"/> from the given <see cref="Expression"/>.
    /// </summary>
    /// <param name="expression">The expression to analyze. This can be a <see cref="MemberExpression"/> or a <see cref="UnaryExpression"/>
    /// wrapping a <see cref="MemberExpression"/>.</param>
    /// <returns>A <see cref="MemberExpression"/> if the input expression is a <see cref="MemberExpression"/>  or a <see
    /// cref="UnaryExpression"/> wrapping a <see cref="MemberExpression"/>; otherwise, <see langword="null"/>.</returns>
    private static MemberExpression GetMemberExpression(Expression expression) {
        switch (expression) {
            case MemberExpression me:
                return me;
            case UnaryExpression ue when ue.Operand is MemberExpression me2:
                return me2;
            default:
                return null;
        }
    }
}
