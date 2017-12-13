// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using AutoRest.Core;
using AutoRest.Core.Model;
using AutoRest.Core.Utilities;
using AutoRest.Modeler.Model;
using AutoRest.Modeler.Properties;
using static AutoRest.Core.Utilities.DependencyInjection;
using System.Linq;
using AutoRest.Swagger;

namespace AutoRest.Modeler
{
    /// <summary>
    /// The builder for building swagger schema into client model parameters, 
    /// service types or Json serialization types.
    /// </summary>
    public class SchemaBuilder : ObjectBuilder
    {
        private const string DiscriminatorValueExtension = "x-ms-discriminator-value";

        private Schema _schema;

        public SchemaBuilder(Schema schema, SwaggerModeler modeler)
            : base(schema, modeler)
        {
            _schema = schema;
        }

        public override IModelType BuildServiceType(string serviceTypeName, bool required)
        {
            _schema = Modeler.Resolver.Unwrap(_schema);

            // translate nullable back to what "code-model-v1"-gen generators expect
            if (_schema.Nullable.HasValue && !_schema.Extensions.ContainsKey("x-nullable"))
            {
                _schema.Extensions["x-nullable"] = _schema.Nullable.Value;
            }

            IModelType result = null;

            // If it's a primitive type, let the parent build service handle it
            if (_schema.IsPrimitiveType())
            {
                result = _schema.GetBuilder(Modeler).ParentBuildServiceType(serviceTypeName, required);
            }
            else
            {
                // If it's known primary type, return that type
                var primaryType = _schema.GetSimplePrimaryType(Modeler.GenerateEmptyClasses);
                if (primaryType != KnownPrimaryType.None)
                {
                    result = New<PrimaryType>(primaryType);
                }
                else
                {
                    // Otherwise create new object type
                    var objectType = New<CompositeType>(serviceTypeName,new 
                    {
                        SerializedName = serviceTypeName,
                        Documentation = _schema.Description,
                        ExternalDocsUrl = _schema.ExternalDocs?.Url,
                        Summary = _schema.Title
                    });

                    // associate this type with its schema (by reference) in order to allow recursive models to terminate
                    // (e.g. if `objectType` type has property of type `objectType[]`)
                    if (Modeler.GeneratingTypes.ContainsKey(_schema))
                    {
                        return Modeler.GeneratingTypes[_schema];
                    }
                    Modeler.GeneratingTypes[_schema] = objectType;

                    if (_schema.Type == DataType.Object && _schema.AdditionalProperties != null)
                    {
                        // this schema is defining 'additionalProperties' which expects to create an extra
                        // property that will catch all the unbound properties during deserialization.
                        var name = "additionalProperties";
                        var propertyType = New<DictionaryType>(new
                        {
                            ValueType = _schema.AdditionalProperties.GetBuilder(Modeler).BuildServiceType(
                                    _schema.AdditionalProperties.Reference != null
                                    ? _schema.AdditionalProperties.Reference.StripComponentsSchemaPath()
                                    : serviceTypeName + "Value", false),
                            Extensions = _schema.AdditionalProperties.Extensions,
                            SupportsAdditionalProperties = true
                        });

                        // now add the extra property to the type.
                        objectType.Add(New<Property>(new
                        {
                            Name = name,
                            ModelType = propertyType,
                            Documentation = "Unmatched properties from the message are deserialized this collection",
                            XmlProperties = _schema.AdditionalProperties.Xml,
                            RealPath = new string[0]
                        }));
                    }

                    if (_schema.Properties != null)
                    {
                        // Visit each property and recursively build service types
                        foreach (var property in _schema.Properties)
                        {
                            string name = property.Key;
                            if (name != _schema.Discriminator?.PropertyName)
                            {
                                string propertyServiceTypeName;
                                Schema refSchema = null;
                                var propertyValue = property.Value;

                                if (propertyValue.Reference != null)
                                {
                                    propertyServiceTypeName = propertyValue.Reference.StripComponentsSchemaPath();
                                    var unwrappedSchema = Modeler.Resolver.Unwrap(propertyValue);

                                    // For Enums use the referenced schema in order to set the correct property Type and Enum values
                                    if (unwrappedSchema.Enum != null)
                                    {
                                        refSchema = new Schema().LoadFrom(unwrappedSchema);
                                        //Todo: Remove the following when referenced descriptions are correctly ignored (Issue https://github.com/Azure/autorest/issues/1283)
                                        refSchema.Description = propertyValue.Description;
                                    }
                                }
                                else
                                {
                                    propertyServiceTypeName = serviceTypeName + "_" + property.Key;
                                }
                                var isRequired = _schema.Required?.Contains(property.Key) ?? false;
                                var propertyType = refSchema != null
                                                ? refSchema.GetBuilder(Modeler).BuildServiceType(propertyServiceTypeName, isRequired)
                                                : propertyValue.GetBuilder(Modeler).BuildServiceType(propertyServiceTypeName, isRequired);

                                var forwardToTarget = propertyValue.ForwardTo();
                                var propertyObj = New<Property>(new
                                {
                                    Name = name,
                                    SerializedName = propertyValue.IsTaggedAsNoWire() ? null : name,
                                    RealPath = new string[] { name },
                                    ModelType = propertyType,
                                    IsReadOnly = propertyValue.ReadOnly,
                                    Summary = propertyValue.Title,
                                    XmlProperties = propertyValue.Xml,
                                    IsRequired = isRequired,
                                    ForwardTo = forwardToTarget == null ? null : New<Property>(new { SerializedName = forwardToTarget }),
                                    Implementation = propertyValue.Implementation()
                                });

                                PopulateProperty(propertyObj, refSchema != null ? refSchema : propertyValue);
                                propertyObj.Deprecated = propertyValue.Deprecated || (refSchema?.Deprecated ?? false);
                                var propertyCompositeType = propertyType as CompositeType;
                                if (propertyObj.IsConstant || true == propertyCompositeType?.ContainsConstantProperties)
                                {
                                    objectType.ContainsConstantProperties = true;
                                }

                                objectType.Add(propertyObj);
                            }
                            else
                            {
                                objectType.PolymorphicDiscriminator = name;
                            }
                        }
                        // wire up forwarded properties
                        SwaggerModeler.ProcessForwardToProperties(objectType.Properties);
                    }

                    // Copy over extensions
                    _schema.Extensions.ForEach(e => objectType.Extensions[e.Key] = e.Value);

                    // Optionally override the discriminator value for polymorphic types. We expect this concept to be
                    // added to Swagger at some point, but until it is, we use an extension.
                    object discriminatorValueExtension;
                    if (objectType.Extensions.TryGetValue(DiscriminatorValueExtension, out discriminatorValueExtension))
                    {
                        string discriminatorValue = discriminatorValueExtension as string;
                        if (discriminatorValue != null)
                        {
                            objectType.SerializedName = discriminatorValue;
                        }
                    }

                    if (_schema.Extends != null)
                    {
                        // Put this in the extended type serializationProperty for building method return type in the end
                        Modeler.ExtendedTypes[serviceTypeName] = _schema.Extends.StripComponentsSchemaPath();
                    }
                    
                    // Put this in already generated types serializationProperty
                    string localName = serviceTypeName;
                    while (Modeler.GeneratedTypes.ContainsKey(localName))
                    {
                        var existing = Modeler.GeneratedTypes[localName];
                        if (objectType.StructurallyEquals(existing))
                        {
                            objectType = existing;
                            break;
                        }
                        localName = localName + "_";
                    }
                    Modeler.GeneratedTypes[localName] = objectType;

                    result = objectType;
                }
            }
            // xml properties
            result.XmlProperties = _schema.Xml;
            result.Deprecated = _schema.Deprecated;
            return result;
        }

        public override IModelType ParentBuildServiceType(string serviceTypeName, bool required)
        {
            return base.BuildServiceType(serviceTypeName, required);
        }
    }
}