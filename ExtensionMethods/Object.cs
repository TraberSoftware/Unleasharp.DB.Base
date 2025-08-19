using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.SchemaDefinition;

namespace Unleasharp.DB.Base.ExtensionMethods {
    public static class Object {
        public static Dictionary<string, dynamic> ToDynamicDictionary(this object Row) {
            Dictionary<string, dynamic> Result = new Dictionary<string, dynamic>();

            Type RowType = Row.GetType();

            #region Field serialization
            foreach (FieldInfo Field in RowType.GetFields()) {
                string DBFieldName = Field.Name;

                foreach (Attribute FieldAttribute in Field.GetCustomAttributes()) {
                    if (
                        FieldAttribute.GetType().BaseType == typeof(NamedStructure)
                        ||
                        FieldAttribute.GetType()          == typeof(NamedStructure)
                    ) {
                        DBFieldName = ((NamedStructure)FieldAttribute).Name;
                    }
                }

                Result.Add(DBFieldName, Field.GetValue(Row));
            }
            #endregion

            #region Property Serialization
            foreach (PropertyInfo Property in RowType.GetProperties()) {
                string DBFieldName = Property.Name;

                foreach (Attribute PropertyAttribute in Property.GetCustomAttributes()) {
                    if (
                        PropertyAttribute.GetType().BaseType == typeof(NamedStructure)
                        ||
                        PropertyAttribute.GetType()          == typeof(NamedStructure)
                    ) {
                        DBFieldName = ((NamedStructure)PropertyAttribute).Name;
                    }
                }

                Result.Add(DBFieldName, Property.GetValue(Row));
            }
            #endregion

            return Result;
        }
    }
}
