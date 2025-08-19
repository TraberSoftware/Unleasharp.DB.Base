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

namespace Unleasharp.DB.Base.ExtensionMethods {
    public static class DataRowExtension {
        public static T GetObject<T>(this DataRow Row) where T : class {
            T Serialized = Activator.CreateInstance<T>();

            #region Field serialization
            foreach (FieldInfo Field in typeof(T).GetFields()) {
                string     DBFieldName = Field.Name;
                string  ClassFieldName = Field.Name;
                Type    FieldType      = Field.FieldType;

                if (Nullable.GetUnderlyingType(FieldType) != null) {
                    FieldType = Nullable.GetUnderlyingType(FieldType);
                }

                foreach (Attribute FieldAttribute in Field.GetCustomAttributes()) {
                    if (
                        FieldAttribute.GetType().BaseType == typeof(NamedStructure)
                        ||
                        FieldAttribute.GetType()          == typeof(NamedStructure)
                    ) { 
                        DBFieldName = ((NamedStructure) FieldAttribute).Name;
                    }
                }

                MethodInfo DataRowFieldMethod        = typeof(DataRowExtensions).GetMethod("Field", new[] { typeof(DataRow), typeof(string) });
                MethodInfo DataRowFieldGenericMethod = DataRowFieldMethod.MakeGenericMethod(FieldType);

                //.Invoke(Row.GetType(), new object[] { Row, FieldType, DBFieldName });

                if (FieldType.IsEnum) {
                    string EnumValueString = Row.Field<string>(DBFieldName);

                    if (EnumValueString == null) {
                        Field.SetValue(
                            Serialized,
                            null
                        );

                        continue;
                    }

                    bool ValueFound = false;
                    foreach (Enum EnumValue in Enum.GetValues(FieldType)) {
                        string EnumDescription = EnumValue.GetDescription();
                        if (
                            EnumDescription == EnumValueString
                        ) {
                            Field.SetValue(
                                Serialized,
                                EnumValue
                            );
                            ValueFound = true;

                            break;
                        }
                    }

                    if (ValueFound) {
                        continue;
                    }
                }

                Field.SetValue(
                    Serialized,
                    DataRowFieldGenericMethod.Invoke(null, new object[] { Row, DBFieldName })
                );
            }
            #endregion

            #region Property Serialization
            foreach (PropertyInfo Property in typeof(T).GetProperties()) {
                string     DBFieldName = Property.Name;
                string  ClassFieldName = Property.Name;
                Type    PropertyType   = Property.PropertyType;

                if (Nullable.GetUnderlyingType(PropertyType) != null) {
                    PropertyType = Nullable.GetUnderlyingType(PropertyType);
                }

                foreach (Attribute PropertyAttribute in Property.GetCustomAttributes()) {
                    if (
                        PropertyAttribute.GetType().BaseType == typeof(NamedStructure)
                        ||
                        PropertyAttribute.GetType() == typeof(NamedStructure)
                    ) {
                        DBFieldName = ((NamedStructure)PropertyAttribute).Name;
                    }
                }

                MethodInfo DataRowFieldMethod        = typeof(DataRowExtensions).GetMethod("Field", new[] { typeof(DataRow), typeof(string) });
                MethodInfo DataRowFieldGenericMethod = DataRowFieldMethod.MakeGenericMethod(PropertyType);

                if (PropertyType.IsEnum) {
                    string EnumValueString = Row.Field<string>(DBFieldName);

                    if (EnumValueString == null) {
                        Property.SetValue(
                            Serialized,
                            null
                        );

                        continue;
                    }

                    bool ValueFound = false;
                    foreach (Enum EnumValue in Enum.GetValues(PropertyType)) {
                        string EnumDescription = EnumValue.GetDescription();
                        if (
                            EnumDescription == EnumValueString
                        ) {
                            Property.SetValue(
                                Serialized,
                                EnumValue
                            );
                            ValueFound = true;

                            break;
                        }
                    }

                    if (ValueFound) {
                        continue;
                    }
                }

                Property.SetValue(
                    Serialized,
                    DataRowFieldGenericMethod.Invoke(null, new object[] { Row, DBFieldName })
                );
            }
            #endregion

            return Serialized;
        }
    }
}
