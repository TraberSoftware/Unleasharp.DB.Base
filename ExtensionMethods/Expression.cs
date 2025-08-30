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
    public static AttributeType GetAttribute<TableType, AttributeType>(this Expression<Func<TableType, object>> expression) where AttributeType : Attribute {
        string tableName  = typeof(TableType).GetTableName();
        string columnName = ExpressionHelper.ExtractColumnName<TableType>(expression);

        MemberInfo? member = typeof(TableType).GetMember(columnName)?.FirstOrDefault();
        if (member != null) {
            return member.GetCustomAttribute<AttributeType>();
        }

        return null;
    }

    public static MemberInfo GetMember<T>(this Expression<Func<T, object>> expression) {
        string tableName  = typeof(T).GetTableName();
        string columnName = ExpressionHelper.ExtractColumnName<T>(expression);

        return typeof(T).GetMember(columnName)?.FirstOrDefault();
    }

    public static FieldSelector GetFieldSelector<T>(this Expression<Func<T, object>> expression) {
        string tableName  = typeof(T).GetTableName();
        string columnName = ExpressionHelper.ExtractColumnName<T>(expression);

        return new FieldSelector(tableName, columnName, true);
    }
}
