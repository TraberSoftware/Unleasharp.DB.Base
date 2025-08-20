using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.SchemaDefinition;

namespace Unleasharp.DB.Base.ExtensionMethods;

public static class Object {
    public static Dictionary<string, dynamic> ToDynamicDictionary(this object row) {
        Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();

        Type rowType = row.GetType();

        #region Field serialization
        foreach (FieldInfo field in rowType.GetFields()) {
            string dbFieldName = field.Name;

            foreach (Attribute fieldAttribute in field.GetCustomAttributes()) {
                if (
                    fieldAttribute.GetType().BaseType == typeof(NamedStructure)
                    ||
                    fieldAttribute.GetType()          == typeof(NamedStructure)
                ) {
                    dbFieldName = ((NamedStructure)fieldAttribute).Name;
                }
            }

            result.Add(dbFieldName, field.GetValue(row));
        }
        #endregion

        #region Property Serialization
        foreach (PropertyInfo property in rowType.GetProperties()) {
            string dbFieldName = property.Name;

            foreach (Attribute propertyAttribute in property.GetCustomAttributes()) {
                if (
                    propertyAttribute.GetType().BaseType == typeof(NamedStructure)
                    ||
                    propertyAttribute.GetType()          == typeof(NamedStructure)
                ) {
                    dbFieldName = ((NamedStructure)propertyAttribute).Name;
                }
            }

            result.Add(dbFieldName, property.GetValue(row));
        }
        #endregion

        return result;
    }
}
