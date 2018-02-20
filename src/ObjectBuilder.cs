// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using AutoRest.Core;
using AutoRest.Core.Model;
using AutoRest.Core.Utilities;
using AutoRest.Core.Utilities.Collections;
using AutoRest.Modeler.Model;
using static AutoRest.Core.Utilities.DependencyInjection;
using Newtonsoft.Json.Linq;
using AutoRest.Swagger;

namespace AutoRest.Modeler
{
    /// <summary>
    /// The builder for building a generic swagger object into parameters, 
    /// service types or Json serialization types.
    /// </summary>
    public class ObjectBuilder
    {
        protected SwaggerObject SwaggerObject { get; set; }
        protected SwaggerModeler Modeler { get; set; }

        public ObjectBuilder(SwaggerObject swaggerObject, SwaggerModeler modeler)
        {
            SwaggerObject = swaggerObject;
            Modeler = modeler;
        }

        public virtual IModelType ParentBuildServiceType(string serviceTypeName, bool required)
        {
            // Should not try to get parent from generic swagger object builder
            throw new InvalidOperationException();
        }

        /// <summary>
        /// The visitor method for building service types. This is called when an instance of this class is
        /// visiting a _swaggerModeler to build a service type.
        /// </summary>
        /// <param name="serviceTypeName">name for the service type</param>
        /// <returns>built service type</returns>
        public virtual IModelType BuildServiceType(string serviceTypeName, bool required)
        {
            PrimaryType type = SwaggerObject.ToType();
            Debug.Assert(type != null);

            if (type.KnownPrimaryType == KnownPrimaryType.Object && SwaggerObject.KnownFormat == KnownFormat.file)
            {
                type = New<PrimaryType>(KnownPrimaryType.Stream);
            }
            type.XmlProperties = (SwaggerObject as Schema)?.Xml;
            type.Format = SwaggerObject.Format;
            var xMsEnum = SwaggerObject.Extensions.GetValue<JToken>(Core.Model.XmsExtensions.Enum.Name);
            if (xMsEnum != null && SwaggerObject.Enum == null)
            {
                throw new InvalidOperationException($"Found 'x-ms-enum' without 'enum' on the same level. Please either add an 'enum' restriction or remove 'x-ms-enum'.");
            }
            if (SwaggerObject.Enum != null && type.KnownPrimaryType == KnownPrimaryType.String && !(IsSwaggerObjectConstant(SwaggerObject, required)))
            {
                if (SwaggerObject.Enum.Count == 0)
                {
                    throw new InvalidOperationException($"Found an 'enum' with no values. Please remove this (unsatisfiable) restriction or add values.");
                }

                var enumType = New<EnumType>();
                // Set the underlying type. This helps to determine whether the values in EnumValue are of type string, number, etc.
                enumType.UnderlyingType = type;
                SwaggerObject.Enum.OfType<JValue>().Select(x => (string)x).ForEach(v => enumType.Values.Add(new EnumValue { Name = v, SerializedName = v }));
                if (xMsEnum is JContainer enumObject)
                {
                    var enumName = "" + enumObject["name"];
                    if (string.IsNullOrEmpty(enumName))
                    {
                        throw new InvalidOperationException($"{Core.Model.XmsExtensions.Enum.Name} extension needs to specify an enum name.");
                    }
                    enumType.SetName(enumName);

                    if (enumObject["modelAsString"] != null)
                    {
                        enumType.ModelAsString = bool.Parse(enumObject["modelAsString"].ToString());
                    }
                    
                    enumType.OldModelAsString = (enumObject["oldModelAsString"] != null)? bool.Parse(enumObject["oldModelAsString"].ToString()) : false;
                    if(enumType.OldModelAsString)
                    {
                        enumType.ModelAsString = true;
                    }
                    
                    var valueOverrides = enumObject["values"] as JArray;
                    if (valueOverrides != null)
                    {
                        var valuesBefore = new HashSet<string>(enumType.Values.Select(x => x.SerializedName));
                        enumType.Values.Clear();
                        foreach (var valueOverride in valueOverrides)
                        {
                            var value = valueOverride["value"];
                            var description = valueOverride["description"];
                            var name = valueOverride["name"] ?? value;
                            
                            var enumVal = new EnumValue
                            {
                                Name = (string)name,
                                SerializedName = (string)value,
                                Description = (string)description
                            };

                            if(valueOverride["allowedValues"] is JArray allowedValues)
                            {
                                // set the allowedValues if any
                                foreach(var allowedValue in allowedValues)
                                {
                                    enumVal.AllowedValues.Add(allowedValue.ToString());
                                }
                            }
                            
                            enumType.Values.Add(enumVal);
                        }
                        var valuesAfter = new HashSet<string>(enumType.Values.Select(x => x.SerializedName));
                        // compare values
                        if (!valuesBefore.SetEquals(valuesAfter))
                        {
                            throw new InvalidOperationException($"Values specified by 'enum' mismatch those specified by 'x-ms-enum' (name: '{enumName}'): "
                                + string.Join(", ", valuesBefore.Select(x => $"'{x}'"))
                                + " vs "
                                + string.Join(", ", valuesAfter.Select(x => $"'{x}'")));
                        }
                    }

                    var existingEnum =
                        Modeler.CodeModel.EnumTypes.FirstOrDefault(
                            e => e.Name.RawValue.EqualsIgnoreCase(enumType.Name.RawValue));
                    if (existingEnum != null)
                    {
                        if (!existingEnum.StructurallyEquals(enumType))
                        {
                            throw new InvalidOperationException(
                                string.Format(CultureInfo.InvariantCulture,
                                    "Swagger document contains two or more {0} extensions with the same name '{1}' and different values: {2} vs. {3}",
                                    Core.Model.XmsExtensions.Enum.Name,
                                    enumType.Name,
                                    string.Join(",", existingEnum.Values.Select(x => x.SerializedName)),
                                    string.Join(",", enumType.Values.Select(x => x.SerializedName))));
                        }
                        // Use the existing one!
                        enumType = existingEnum;
                    }
                    else
                    {
                        Modeler.CodeModel.Add(enumType);
                    }
                }
                else
                {
                    enumType.ModelAsString = true;
                    enumType.SetName(string.Empty);
                }
                enumType.XmlProperties = (SwaggerObject as Schema)?.Xml;
                return enumType;
            }
            if (SwaggerObject.Type == DataType.Array)
            {
                if (SwaggerObject.Items == null)
                {
                    throw new Exception($"Invalid Swagger: Missing 'items' definition of an 'array' type.");
                }

                string itemServiceTypeName;
                if (SwaggerObject.Items.Reference != null)
                {
                    itemServiceTypeName = SwaggerObject.Items.Reference.StripComponentsSchemaPath();
                }
                else
                {
                    itemServiceTypeName = serviceTypeName + "Item";
                }

                var elementType =
                    SwaggerObject.Items.GetBuilder(Modeler).BuildServiceType(itemServiceTypeName, false);
                return New<SequenceType>(new
                {
                    ElementType = elementType,
                    Extensions = SwaggerObject.Items.Extensions,
                    XmlProperties = (SwaggerObject as Schema)?.Xml,
                    ElementXmlProperties = SwaggerObject.Items?.Xml
                });
            }
            if (SwaggerObject.AdditionalProperties != null)
            {
                string dictionaryValueServiceTypeName;
                if (SwaggerObject.AdditionalProperties.Reference != null)
                {
                    dictionaryValueServiceTypeName = SwaggerObject.AdditionalProperties.Reference.StripComponentsSchemaPath();
                }
                else
                {
                    dictionaryValueServiceTypeName = serviceTypeName + "Value";
                }
                return New<DictionaryType>(new
                {
                    ValueType =
                        SwaggerObject.AdditionalProperties.GetBuilder(Modeler)
                            .BuildServiceType(dictionaryValueServiceTypeName, false),
                    Extensions = SwaggerObject.AdditionalProperties.Extensions,
                    XmlProperties = (SwaggerObject as Schema)?.Xml
                });
            }

            return type;
        }

        public static void PopulateProperty(Property property, SwaggerObject swaggerObject)
        {
            if (swaggerObject == null)
            {
                throw new ArgumentNullException("swaggerObject");
            }
            if (property == null)
            {
                throw new ArgumentNullException("property");
            }
            property.DefaultValue = swaggerObject.Default;

            if (IsSwaggerObjectConstant(swaggerObject, property.IsRequired))
            {
                property.DefaultValue = swaggerObject.Enum.TokensToStrings().First();
                property.IsConstant = true;
            }

            property.Documentation = swaggerObject.Description;

            // tag the paramter with all the extensions from the swagger object
            property.Extensions.AddRange(swaggerObject.Extensions);

            SetConstraints(property.Constraints, swaggerObject);
        }

        public static void PopulateParameter(IVariable parameter, SwaggerParameter swaggerObject)
        {
            if (swaggerObject == null)
            {
                throw new ArgumentNullException("swaggerObject");
            }
            if (parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }
            parameter.IsRequired = swaggerObject.IsRequired;
            parameter.DefaultValue = swaggerObject.Schema?.Default;

            if (IsSwaggerObjectConstant(swaggerObject.Schema, parameter.IsRequired))
            {
                parameter.DefaultValue = swaggerObject.Schema.Enum.TokensToStrings().First();
                parameter.IsConstant = true;
            }

            parameter.Documentation = swaggerObject.Description;
            parameter.CollectionFormat = swaggerObject.CollectionFormat;

            // tag the paramter with all the extensions from the swagger object
            parameter.Extensions.AddRange(swaggerObject.Extensions);

            SetConstraints(parameter.Constraints, swaggerObject.Schema);
        }

        private static bool IsSwaggerObjectConstant(SwaggerObject swaggerObject, bool isRequired)
            => swaggerObject.Enum != null && swaggerObject.Enum.Count == 1 && isRequired;

        public static void SetConstraints(Dictionary<Constraint, string> constraints, SwaggerObject swaggerObject)
        {
            if (constraints == null)
            {
                throw new ArgumentNullException("constraints");
            }
            if (swaggerObject == null)
            {
                throw new ArgumentNullException("swaggerObject");
            }

            if (!string.IsNullOrEmpty(swaggerObject.Maximum)
                && swaggerObject.IsConstraintSupported(nameof(swaggerObject.Maximum))
                && !swaggerObject.ExclusiveMaximum)

            {
                constraints[Constraint.InclusiveMaximum] = swaggerObject.Maximum;
            }
            if (!string.IsNullOrEmpty(swaggerObject.Maximum)
                && swaggerObject.IsConstraintSupported(nameof(swaggerObject.Maximum))
                && swaggerObject.ExclusiveMaximum
                && swaggerObject.IsConstraintSupported(nameof(swaggerObject.ExclusiveMaximum)))
            {
                constraints[Constraint.ExclusiveMaximum] = swaggerObject.Maximum;
            }
            if (!string.IsNullOrEmpty(swaggerObject.Minimum)
                && swaggerObject.IsConstraintSupported(nameof(swaggerObject.Minimum))
                && !swaggerObject.ExclusiveMinimum)
            {
                constraints[Constraint.InclusiveMinimum] = swaggerObject.Minimum;
            }
            if (!string.IsNullOrEmpty(swaggerObject.Minimum)
                && swaggerObject.IsConstraintSupported(nameof(swaggerObject.Minimum))
                && swaggerObject.ExclusiveMinimum
                && swaggerObject.IsConstraintSupported(nameof(swaggerObject.ExclusiveMinimum)))
            {
                constraints[Constraint.ExclusiveMinimum] = swaggerObject.Minimum;
            }
            if (!string.IsNullOrEmpty(swaggerObject.MaxLength)
                && swaggerObject.IsConstraintSupported(nameof(swaggerObject.MaxLength)))
            {
                constraints[Constraint.MaxLength] = swaggerObject.MaxLength;
            }
            if (!string.IsNullOrEmpty(swaggerObject.MinLength)
                && swaggerObject.IsConstraintSupported(nameof(swaggerObject.MinLength)))
            {
                constraints[Constraint.MinLength] = swaggerObject.MinLength;
            }
            if (!string.IsNullOrEmpty(swaggerObject.Pattern)
                && swaggerObject.IsConstraintSupported(nameof(swaggerObject.Pattern)))
            {
                constraints[Constraint.Pattern] = swaggerObject.Pattern;
            }
            if (!string.IsNullOrEmpty(swaggerObject.MaxItems)
                && swaggerObject.IsConstraintSupported(nameof(swaggerObject.MaxItems)))
            {
                constraints[Constraint.MaxItems] = swaggerObject.MaxItems;
            }
            if (!string.IsNullOrEmpty(swaggerObject.MinItems)
                && swaggerObject.IsConstraintSupported(nameof(swaggerObject.MinItems)))
            {
                constraints[Constraint.MinItems] = swaggerObject.MinItems;
            }
            if (!string.IsNullOrEmpty(swaggerObject.MultipleOf)
                && swaggerObject.IsConstraintSupported(nameof(swaggerObject.MultipleOf)))
            {
                constraints[Constraint.MultipleOf] = swaggerObject.MultipleOf;
            }
            if (swaggerObject.UniqueItems
                && swaggerObject.IsConstraintSupported(nameof(swaggerObject.UniqueItems)))
            {
                constraints[Constraint.UniqueItems] = "true";
            }
        }
    }
}
