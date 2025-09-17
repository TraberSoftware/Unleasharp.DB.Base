using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.ExtensionMethods;
using Unleasharp.DB.Base.QueryBuilding;
using Unleasharp.DB.Base.SchemaDefinition;

namespace Unleasharp.DB.Base;

public class ResultCacheRow {
    public Table                      Table;
    public FieldSelector              KeyColumn;
    public string                     KeyColumnName;
    public Dictionary<string, object> Data;

    public ResultCacheRow() { }

    public ResultCacheRow(object row) {
        Table    = row.GetType().GetCustomAttribute<Table>();
        Data     = row.ToDynamicDictionary();

        foreach (PropertyInfo property in row.GetType().GetProperties()) {
            Column column = property.GetCustomAttribute<Column>();
            if (column != null && column.PrimaryKey) {
                KeyColumn     = new FieldSelector(Table.Name, column.Name);
                KeyColumnName = column.Name;
                break;
            }
        }
    }
}
