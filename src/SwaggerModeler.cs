// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using AutoRest.Core;
using AutoRest.Core.Model;
using AutoRest.Core.Logging;
using AutoRest.Core.Utilities;
using AutoRest.Core.Utilities.Collections;
using AutoRest.Modeler.Model;
using AutoRest.Modeler.Properties;
using ParameterLocation = AutoRest.Modeler.Model.ParameterLocation;
using static AutoRest.Core.Utilities.DependencyInjection;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using AutoRest.Swagger;

namespace AutoRest.Modeler
{
    public class SwaggerModeler
    {
        internal Dictionary<string, string> ExtendedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        internal Dictionary<string, CompositeType> GeneratedTypes = new Dictionary<string, CompositeType>();
        internal Dictionary<Schema, CompositeType> GeneratingTypes = new Dictionary<Schema, CompositeType>();

        public bool GenerateEmptyClasses { get; private set; }

        public SwaggerModeler(Settings settings = null, bool generateEmptyClasses = false)
        {
            this.settings = settings ?? new Settings();
            this.GenerateEmptyClasses = generateEmptyClasses;
        }

        /// <summary>
        /// Swagger service model.
        /// </summary>
        public ServiceDefinition ServiceDefinition { get; set; }

        private Settings settings;

        /// <summary>
        /// Client model.
        /// </summary>
        public CodeModel CodeModel { get; set; }

        /// <summary>
        /// Operations may have a content type parameter.
        /// We collect allowed values and create a dedicated enum for convenience.
        /// </summary>
        public HashSet<string> ContentTypeChoices { get; } = new HashSet<string>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        public CodeModel Build(ServiceDefinition serviceDefinition)
        {
            ServiceDefinition = serviceDefinition;

            // Update settings
            UpdateSettings();

            InitializeClientModel();
            BuildCompositeTypes();

            // Build client parameters
            foreach (var swaggerParameter in ServiceDefinition.Components.Parameters.Values)
            {
                var parameter = ((ParameterBuilder)swaggerParameter.GetBuilder(this)).Build();

                var clientProperty = New<Property>();
                clientProperty.LoadFrom(parameter);
                clientProperty.RealPath = new string[] { parameter.SerializedName };

                CodeModel.Add(clientProperty);
            }

            var baseErrorResponses = new List<Fixable<string>>();
            var methods = new List<Method>();
            // Build methods
            foreach (var path in ServiceDefinition.Paths.Concat(ServiceDefinition.CustomPaths))
            {
                foreach (var verb in path.Value.Keys)
                {
                    var operation = path.Value[verb];
                    if (string.IsNullOrWhiteSpace(operation.OperationId))
                    {
                        throw ErrorManager.CreateError(
                            string.Format(CultureInfo.InvariantCulture,
                                Resources.OperationIdMissing,
                                verb,
                                path.Key));
                    }
                    var methodName = GetMethodNameFromOperationId(operation.OperationId);
                    var methodGroup = GetMethodGroup(operation);
                    
                    if (verb.ToHttpMethod() != HttpMethod.Options)
                    {
                        string url = path.Key;
                        if (url.Contains("?"))
                        {
                            url = url.Substring(0, url.IndexOf('?'));
                        }
                        var method = BuildMethod(verb.ToHttpMethod(), url, methodName, operation);
                        method.Group = methodGroup;
                        methods.Add(method);

                        // Add error models marked by x-ms-error-response
                        var xmsErrorResponses = method.Responses.Values.Where(resp=>resp.Extensions.ContainsKey("x-ms-error-response") && (bool)resp.Extensions["x-ms-error-response"] && resp.Body is CompositeType)
                                                                       .Select(resp=>(CompositeType)resp.Body);
                        xmsErrorResponses.ForEach(errModel=>CodeModel.AddError(errModel));

                        // If marked error models have a polymorphic discriminator, include all models that allOf on them (at any level of inheritence)
                        baseErrorResponses = baseErrorResponses.Union(xmsErrorResponses.Where(errModel=>!string.IsNullOrEmpty(errModel.PolymorphicDiscriminator) && ExtendedTypes.ContainsKey(errModel.Name))
                                                                  .Select(errModel=>errModel.Name)).ToList();

                        // Add the default error model if exists
                        if (method.DefaultResponse.Body is CompositeType)
                        {
                            baseErrorResponses.Add(((CompositeType)method.DefaultResponse.Body).Name);
                            CodeModel.AddError((CompositeType)method.DefaultResponse.Body);
                        }
               
                    }
                    else
                    {
                        Logger.Instance.Log(Category.Warning, Resources.OptionsNotSupported);
                    }
                }
            }
            ProcessForwardToMethods(methods);

            
            // Set base type
            foreach (var typeName in GeneratedTypes.Keys)
            {
                var objectType = GeneratedTypes[typeName];
                if (ExtendedTypes.ContainsKey(typeName))
                {
                    objectType.BaseModelType = GeneratedTypes[ExtendedTypes[typeName]];
                }

                CodeModel.Add(objectType);
            }

            CodeModel.AddRange(methods);

            
            foreach(var k in GeneratedTypes.Keys)
            {
                var baseModelType = GeneratedTypes[k].BaseModelType;
                while(baseModelType != null && baseModelType is CompositeType && !baseErrorResponses.Contains(k))
                {
                    if(baseErrorResponses.Contains(baseModelType.Name))
                    {
                        CodeModel.AddError(GeneratedTypes[k]);
                        break;
                    }
                    baseModelType = baseModelType.BaseModelType;
                }
            }
            
            // What operation returns it decides whether an object is to be modeled as a 
            // regular model class or an exception class
            // Set base type
            var errorResponses = 
                ServiceDefinition.Paths.Values.SelectMany(pathObj=>pathObj.Values.SelectMany(opObj=>opObj.Responses.Values.Where(res=>res.Extensions?.ContainsKey("x-ms-error-response")==true && (bool)res.Extensions["x-ms-error-response"])));
            var errorModels = errorResponses.Select(resp=>resp.Schema?.Reference).Where(modelRef=>!string.IsNullOrEmpty(modelRef)).Select(modelRef=>GeneratedTypes[modelRef]);
            errorModels.ForEach(errorModel=>CodeModel.AddError(errorModel));

            // Build ContentType enum
            if (ContentTypeChoices.Count > 0)
            {
                var enumType = New<EnumType>();
                enumType.ModelAsString = true;
                enumType.SetName("ContentTypes");
                enumType.Values.AddRange(ContentTypeChoices.Select(v => new EnumValue { Name = v, SerializedName = v }));
                CodeModel.Add(enumType);
            }

            ProcessParameterizedHost();
            return CodeModel;
        }

        internal static void ProcessForwardToMethods(IEnumerable<Method> allMethods)
        {
            foreach (var method in allMethods)
            {
                if (method.ForwardTo?.SerializedName != null)
                {
                    // resolve target method
                    var target = allMethods.FirstOrDefault(m => m.SerializedName == method.ForwardTo.SerializedName);
                    if (target == null)
                    {
                        throw new CodeGenerationException($"Cannot forward to '{method.ForwardTo.SerializedName}'. No method with that name found.");
                    }
                    method.ForwardTo = target;
                }
            }
        }

        internal static void ProcessForwardToProperties(IEnumerable<Property> properties)
        {
            foreach (var prop in properties)
            {
                if (prop.ForwardTo?.SerializedName != null)
                {
                    // resolve target property
                    var target = properties.FirstOrDefault(m => m.SerializedName == prop.ForwardTo.SerializedName);
                    if (target == null)
                    {
                        throw new CodeGenerationException($"Cannot forward to '{prop.ForwardTo.SerializedName}'. No property with that name found.");
                    }
                    prop.ForwardTo = target;
                }
            }
        }

        private void UpdateSettings()
        {
            if (ServiceDefinition?.Info?.CodeGenerationSettings != null)
            {
                foreach (var key in ServiceDefinition.Info.CodeGenerationSettings.Extensions.Keys)
                {
                    //Don't overwrite settings that come in from the command line
                    if (!settings.CustomSettings.ContainsKey(key))
                        settings.CustomSettings[key] = ServiceDefinition.Info.CodeGenerationSettings.Extensions[key];
                }
                Settings.PopulateSettings(settings, settings.CustomSettings);
            }
        }

        /// <summary>
        /// Initialize the base service and populate global service properties
        /// </summary>
        /// <returns>The base ServiceModel Service</returns>
        private void InitializeClientModel()
        {
            if (ServiceDefinition.Info == null)
            {
                throw ErrorManager.CreateError(Resources.InfoSectionMissing);
            }

            CodeModel = New<CodeModel>();

            if (string.IsNullOrWhiteSpace(settings.ClientName) && ServiceDefinition.Info.Title == null)
            {
                throw ErrorManager.CreateError(Resources.TitleMissing);
            }

            CodeModel.Name = ServiceDefinition.Info.Title?.Replace(" ", "");

            CodeModel.Namespace = settings.Namespace;
            CodeModel.ModelsName = settings.ModelsName;
            CodeModel.ApiVersion = ServiceDefinition.Info.Version == "" // since info.version is required according to spec, swagger2openapi sets it to "" if missing
                ? null                                                  // ...but that mocks with our multi-api-version treatment of inlining the api-version
                : ServiceDefinition.Info.Version;
            CodeModel.Documentation = ServiceDefinition.Info.Description;
            CodeModel.BaseUrl = ServiceDefinition.Servers.FirstOrDefault()?.Url?.TrimEnd('/');
            if (string.IsNullOrEmpty(CodeModel.BaseUrl))
            {
                CodeModel.BaseUrl = "http://localhost";
            }

            // Copy extensions
            ServiceDefinition.Info?.CodeGenerationSettings?.Extensions.ForEach(extention => CodeModel.CodeGenExtensions.AddOrSet(extention.Key, extention.Value));
            ServiceDefinition.Extensions.ForEach(extention => CodeModel.Extensions.AddOrSet(extention.Key, extention.Value));
        }

        private void ProcessParameterizedHost()
        {
            var server = ServiceDefinition.Servers.FirstOrDefault();
            if ((server?.Variables?.Count ?? 0) > 0)
            {
                CodeModel.Extensions.Add("x-ms-parameterized-host", true); // TODO: generators look for presence of that extension

                var position = "first";

                var hostExtension = server.Extensions.GetValue<JObject>("x-ms-parameterized-host");
                if (hostExtension != null && hostExtension.TryGetValue("positionInOperation", out var textRaw))
                {
                    position = textRaw.ToString();
                }

                var jsonSettings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None,
                    MetadataPropertyHandling = MetadataPropertyHandling.Ignore
                };

                List<Parameter> hostParamList = new List<Parameter>();
                foreach (var serverVar in server.Variables)
                {
                    var swaggerParameter = new SwaggerParameter
                    {
                        In = ParameterLocation.Path,
                        Name = serverVar.Key,
                        Description = serverVar.Value.Description,
                        Schema = new Schema { Type = DataType.String, Default = serverVar.Value.Default, Enum = serverVar.Value.Enum?.StringsToTokens().ToList() },
                        Extensions = serverVar.Value.Extensions,
                        IsRequired = true
                    };
                    // Build parameter
                    var parameterBuilder = new ParameterBuilder(swaggerParameter, this);
                    var parameter = parameterBuilder.Build();

                    // check to see if the parameter exists in properties, and needs to have its name normalized
                    if (CodeModel.Properties.Any(p => p.SerializedName.EqualsIgnoreCase(parameter.SerializedName)))
                    {
                        parameter.ClientProperty =
                            CodeModel.Properties.Single(
                                p => p.SerializedName.Equals(parameter.SerializedName));
                    }
                    parameter.Extensions["hostParameter"] = true;
                    hostParamList.Add(parameter);
                }

                if (position.EqualsIgnoreCase("first"))
                {
                    CodeModel.HostParametersFront = hostParamList.AsEnumerable().Reverse();
                }
                else if (position.EqualsIgnoreCase("last"))
                {
                    CodeModel.HostParametersBack = hostParamList;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"The value '{position}' provided for property 'positionInOperation' of extension 'x-ms-parameterized-host' is invalid. Valid values are: 'first, last'.");
                }
            }
        }

        /// <summary>
        /// Build composite types from definitions
        /// </summary>
        public virtual void BuildCompositeTypes()
        {
            var schemas = ServiceDefinition.Components.Schemas;
            // Build service types and validate allOf
            if (schemas != null)
            {
                foreach (var schemaName in schemas.Keys.ToArray())
                {
                    var schema = schemas[schemaName];
                    schema.GetBuilder(this).BuildServiceType(schemaName, false);

                    Resolver.ExpandAllOf(schema);
                    var parent = string.IsNullOrEmpty(schema.Extends.StripComponentsSchemaPath())
                        ? null
                        : schemas[schema.Extends.StripComponentsSchemaPath()];

                    if (parent != null &&
                        !AncestorsHaveProperties(parent.Properties, parent.Extends) &&
                        !GenerateEmptyClasses)
                    {
                        throw ErrorManager.CreateError(Resources.InvalidAncestors, schemaName);
                    }
                }
            }
        }

        /// <summary>
        /// Recursively traverse the schema's extends to verify that it or one of it's parents
        /// has at least one property
        /// </summary>
        /// <param name="properties">The schema's properties</param>
        /// <param name="extends">The schema's extends</param>
        /// <returns>True if one or more properties found in this schema or in it's ancestors. False otherwise</returns>
        private bool AncestorsHaveProperties(Dictionary<string, Schema> properties, string extends)
        {
            if (properties.IsNullOrEmpty() && string.IsNullOrEmpty(extends))
            {
                return false;
            }

            if (!properties.IsNullOrEmpty())
            {
                return true;
            }
            var schemas = ServiceDefinition.Components.Schemas;

            extends = extends.StripComponentsSchemaPath();
            Debug.Assert(!string.IsNullOrEmpty(extends) && schemas.ContainsKey(extends));
            return AncestorsHaveProperties(schemas[extends].Properties,
                schemas[extends].Extends);
        }

        /// <summary>
        /// Builds method from swagger operation.
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="url"></param>
        /// <param name="name"></param>
        /// <param name="operation"></param>
        /// <returns></returns>
        public Method BuildMethod(HttpMethod httpMethod, string url, string name,
            Operation operation)
        {
            string methodGroup = GetMethodGroup(operation);
            var operationBuilder = new OperationBuilder(operation, this);
            Method method = operationBuilder.BuildMethod(httpMethod, url, name, methodGroup);
            return method;
        }

        /// <summary>
        /// Extracts method group from operation ID.
        /// </summary>
        /// <param name="operation">The swagger operation.</param>
        /// <returns>Method group name or null.</returns>
        public static string GetMethodGroup(Operation operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException("operation");
            }

            if (operation.OperationId == null || operation.OperationId.IndexOf('_') == -1)
            {
                return null;
            }

            var parts = operation.OperationId.Split('_');
            return parts[0];
        }

        public static string GetMethodNameFromOperationId(string operationId) => 
            (operationId?.IndexOf('_') != -1) ? operationId.Split('_').Last(): operationId;

        public SwaggerParameter Unwrap(SwaggerParameter swaggerParameter)
        {
            if (swaggerParameter == null)
            {
                throw new ArgumentNullException("swaggerParameter");
            }

            // If referencing global parameters serializationProperty
            if (swaggerParameter.Reference != null)
            {
                if (swaggerParameter.In == ParameterLocation.Body)
                {
                    string referenceKey = swaggerParameter.Reference.StripComponentsRequestBodyPath();
                    if (!ServiceDefinition.Components.RequestBodies.ContainsKey(referenceKey))
                    {
                        throw new ArgumentException(
                            string.Format(CultureInfo.InvariantCulture,
                            Resources.DefinitionDoesNotExist, referenceKey));
                    }

                    swaggerParameter = ServiceDefinition.Components.RequestBodies[referenceKey].AsParameters().First();
                }
                else
                {
                    string referenceKey = swaggerParameter.Reference.StripComponentsParameterPath();
                    if (!ServiceDefinition.Components.Parameters.ContainsKey(referenceKey))
                    {
                        throw new ArgumentException(
                            string.Format(CultureInfo.InvariantCulture,
                            Resources.DefinitionDoesNotExist, referenceKey));
                    }

                    swaggerParameter = ServiceDefinition.Components.Parameters[referenceKey];
                }
            }

            // Unwrap the schema if in "body"
            if (swaggerParameter.Schema != null && swaggerParameter.In == ParameterLocation.Body)
            {
                swaggerParameter.Schema = Resolver.Unwrap(swaggerParameter.Schema);
            }

            return swaggerParameter;
        }

        public SchemaResolver Resolver => new SchemaResolver(this);
    }
}
