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
