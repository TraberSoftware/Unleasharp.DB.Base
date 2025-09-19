using System;
using System.Collections.Generic;
using System.Data.Common;
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
        Table = row.GetType().GetCustomAttribute<Table>();
        Data  = row.ToDynamicDictionary();

        #region Key Column - Primary Key
        PrimaryKey pkey     = row.GetType().GetCustomAttribute<PrimaryKey>();
        string     pkeyName = (pkey?.Columns?.Count() ?? 0) == 1 ? pkey?.Column : null;

        if (pkeyName == null) {
            foreach (PropertyInfo property in row.GetType().GetProperties()) {
                Column column = property.GetCustomAttribute<Column>();
                if (column != null && column.PrimaryKey) {
                    pkeyName = column.Name;
                    break;
                }
            }
        }

        if (pkeyName != null) {
            KeyColumn     = new FieldSelector(Table.Name, pkeyName);
            KeyColumnName = pkeyName;

            return;
        }
        #endregion

        #region Key Column - Unique Key
        UniqueKey uKey     = row.GetType().GetCustomAttribute<UniqueKey>();
        string    ukeyName = (uKey?.Columns?.Count() ?? 0) == 1 ? uKey?.Column : null;

        if (ukeyName == null) {
            foreach (PropertyInfo property in row.GetType().GetProperties()) {
                Column column = property.GetCustomAttribute<Column>();
                if (column != null && column.Unique) {
                    pkeyName = column.Name;
                    break;
                }
            }
        }

        if (ukeyName != null) {
            KeyColumn     = new FieldSelector(Table.Name, ukeyName);
            KeyColumnName = ukeyName;

            return;
        }
        #endregion
    }
}
