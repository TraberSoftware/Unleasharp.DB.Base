using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unleasharp.DB.Base.SchemaDefinition;
using Unleasharp.ExtensionMethods;

namespace Unleasharp.DB.Base.ExtensionMethods;

public static class DataRowExtension {
    public static T GetObject<T>(this DataRow row) where T : class {
        T serialized = Activator.CreateInstance<T>();

        #region Field serialization
        foreach (FieldInfo field in typeof(T).GetFields()) {
            string     dbFieldName = field.Name;
            string  classFieldName = field.Name;
            Type    fieldType      = field.FieldType;

            if (Nullable.GetUnderlyingType(fieldType) != null) {
                fieldType = Nullable.GetUnderlyingType(fieldType);
            }

            foreach (Attribute fieldAttribute in field.GetCustomAttributes()) {
                if (
                    fieldAttribute.GetType().BaseType == typeof(NamedStructure)
                    ||
                    fieldAttribute.GetType()          == typeof(NamedStructure)
                ) { 
                    dbFieldName = ((NamedStructure) fieldAttribute).Name;
                }
            }

            MethodInfo dataRowFieldMethod        = typeof(DataRowExtensions).GetMethod("Field", new[] { typeof(DataRow), typeof(string) });
            MethodInfo dataRowFieldGenericMethod = dataRowFieldMethod.MakeGenericMethod(fieldType);

            //.Invoke(Row.GetType(), new object[] { Row, FieldType, DBFieldName });

            if (fieldType.IsEnum) {
                string enumValueString = row.Field<string>(dbFieldName);

                if (enumValueString == null) {
                    field.SetValue(
                        serialized,
                        null
                    );

                    continue;
                }

                bool valueFound = false;
                foreach (Enum enumValue in Enum.GetValues(fieldType)) {
                    string enumDescription = enumValue.GetDescription();
                    if (
                        enumDescription == enumValueString
                    ) {
                        field.SetValue(
                            serialized,
                            enumValue
                        );
                        valueFound = true;

                        break;
                    }
                }

                if (valueFound) {
                    continue;
                }
            }

            field.SetValue(
                serialized,
                dataRowFieldGenericMethod.Invoke(null, new object[] { row, dbFieldName })
            );
        }
        #endregion

        #region Property Serialization
        foreach (PropertyInfo property in typeof(T).GetProperties()) {
            string     dbFieldName = property.Name;
            string  classFieldName = property.Name;
            Type    propertyType   = property.PropertyType;

            if (Nullable.GetUnderlyingType(propertyType) != null) {
                propertyType = Nullable.GetUnderlyingType(propertyType);
            }

            foreach (Attribute propertyAttribute in property.GetCustomAttributes()) {
                if (
                    propertyAttribute.GetType().BaseType == typeof(NamedStructure)
                    ||
                    propertyAttribute.GetType() == typeof(NamedStructure)
                ) {
                    dbFieldName = ((NamedStructure)propertyAttribute).Name;
                }
            }

            MethodInfo dataRowFieldMethod        = typeof(DataRowExtensions).GetMethod("Field", new[] { typeof(DataRow), typeof(string) });
            MethodInfo dataRowFieldGenericMethod = dataRowFieldMethod.MakeGenericMethod(propertyType);

            if (propertyType.IsEnum) {
                string enumValueString = row.Field<string>(dbFieldName);

                if (enumValueString == null) {
                    property.SetValue(
                        serialized,
                        null
                    );

                    continue;
                }

                bool valueFound = false;
                foreach (Enum enumValue in Enum.GetValues(propertyType)) {
                    string enumDescription = enumValue.GetDescription();
                    if (
                        enumDescription == enumValueString
                    ) {
                        property.SetValue(
                            serialized,
                            enumValue
                        );
                        valueFound = true;

                        break;
                    }
                }

                if (valueFound) {
                    continue;
                }
            }

            property.SetValue(
                serialized,
                dataRowFieldGenericMethod.Invoke(null, new object[] { row, dbFieldName })
            );
        }
        #endregion

        return serialized;
    }
}
