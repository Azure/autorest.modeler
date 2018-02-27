// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using AutoRest.Core.Model;
using AutoRest.Core.Logging;
using AutoRest.Core.Utilities;
using AutoRest.Modeler.Model;
using AutoRest.Modeler.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParameterLocation = AutoRest.Modeler.Model.ParameterLocation;
using AutoRest.Swagger;
using static AutoRest.Core.Utilities.DependencyInjection;

namespace AutoRest.Modeler
{
    /// <summary>
    /// The builder for building swagger operations into client model methods.
    /// </summary>
    public class OperationBuilder
    {
        private readonly IReadOnlyList<string> _effectiveProduces;
        private readonly IReadOnlyList<string> _effectiveConsumes;
        private readonly SwaggerModeler _swaggerModeler;
        private readonly Operation _operation;
        private const string APP_JSON_MIME = "application/json";
        private const string APP_XML_MIME = "application/xml";

        public OperationBuilder(Operation operation, SwaggerModeler swaggerModeler)
        {
            _operation = operation ?? throw new ArgumentNullException("operation");
            _swaggerModeler = swaggerModeler ?? throw new ArgumentNullException("swaggerModeler");
            _effectiveProduces = operation.GetProduces().ToList();
            _effectiveConsumes = operation.GetConsumes(swaggerModeler.ServiceDefinition.Components.RequestBodies).ToList();
        }

        public Method BuildMethod(HttpMethod httpMethod, string url, string methodName, string methodGroup)
        {
            EnsureUniqueMethodName(methodName, methodGroup);

            var method = New<Method>(new
            {
                HttpMethod = httpMethod,
                Url = url,
                Name = methodName,
                SerializedName = _operation.OperationId
            });

            // non-REST operations:
            {
                if (_operation.IsTaggedAsNoWire())
                {
                    method.Url = null;
                }
                string forwardToTarget = _operation.ForwardTo();
                if (forwardToTarget != null)
                {
                    method.Url = null;
                    method.ForwardTo = New<Method>(new { SerializedName = forwardToTarget });
                }
                method.Implementation = _operation.Implementation();
            }

            // assume that without specifying Consumes, that a service will consume JSON
            method.RequestContentType = _effectiveConsumes.FirstOrDefault() ?? APP_JSON_MIME;

            // does the method Consume JSON or XML?
            string serviceConsumes = _effectiveConsumes.FirstOrDefault(s => s.StartsWith(APP_JSON_MIME, StringComparison.OrdinalIgnoreCase)) ?? _effectiveConsumes.FirstOrDefault(s => s.StartsWith(APP_XML_MIME, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(serviceConsumes))
            {
                method.RequestContentType = serviceConsumes;
            }


            // if they accept JSON or XML, and don't specify the charset, lets default to utf-8
            if ((method.RequestContentType.StartsWith(APP_JSON_MIME, StringComparison.OrdinalIgnoreCase) ||
                method.RequestContentType.StartsWith(APP_XML_MIME, StringComparison.OrdinalIgnoreCase)) &&
                method.RequestContentType.IndexOf("charset=", StringComparison.OrdinalIgnoreCase) == -1)
            {
                // Enable UTF-8 charset
                method.RequestContentType += "; charset=utf-8";
            }

            // if the method produces xml, make sure that the method knows that.
            method.ResponseContentTypes = _effectiveProduces.ToArray();

            method.Description = _operation.Description;
            method.Summary = _operation.Summary;
            method.ExternalDocsUrl = _operation.ExternalDocs?.Url;
            method.DeprecationMessage = _operation.GetDeprecationMessage(EntityType.Operation);

            // Service parameters
            BuildMethodParameters(method);

            // Directly requested header types (x-ms-headers)
            var headerTypeReferences = new List<IModelType>();
            var headerTypeName = $"{methodGroup}-{methodName}-Headers".Trim('-');

            // Build header object
            var responseHeaders = new Dictionary<string, Header>();
            _operation.Responses = _operation.Responses ?? new Dictionary<string, OperationResponse>();
            foreach (var response in _operation.Responses.Values)
            {
                var xMsHeaders = response.Extensions?.GetValue<JObject>("x-ms-headers");
                if (xMsHeaders != null)
                {
                    var schema =
                        xMsHeaders.ToObject<Schema>(JsonSerializer.Create(new JsonSerializerSettings
                        {
                            MetadataPropertyHandling = MetadataPropertyHandling.Ignore
                        }));
                    headerTypeReferences.Add(schema.GetBuilder(_swaggerModeler).BuildServiceType(headerTypeName, false));
                }
                else
                {
                    response.Headers?.ForEach(h => responseHeaders[h.Key] = h.Value);
                }
            }
            headerTypeReferences = headerTypeReferences.Distinct().ToList();

            CompositeType headerType;
            if (headerTypeReferences.Count == 0)
            {
                headerType = New<CompositeType>(headerTypeName, new
                {
                    SerializedName = headerTypeName,
                    RealPath = new[] {headerTypeName},
                    Documentation = $"Defines headers for {methodName} operation."
                });
                foreach (var h in responseHeaders)
                {
                    var hv = h.Value;
                    if (hv.Extensions.ContainsKey("x-ms-enum") && !hv.Schema.Extensions.ContainsKey("x-ms-enum"))
                    {
                        hv.Schema.Extensions["x-ms-enum"] = hv.Extensions["x-ms-enum"];
                    }
                    if (h.Value.Extensions != null && h.Value.Extensions.ContainsKey("x-ms-header-collection-prefix"))
                    {
                        var property = New<Property>(new
                        {
                            Name = h.Key,
                            SerializedName = h.Key,
                            RealPath = new[] {h.Key},
                            Extensions = hv.Extensions,
                            ModelType = New<DictionaryType>(new
                            {
                                ValueType = hv.Schema.GetBuilder(this._swaggerModeler).BuildServiceType(h.Key, false)
                            })
                        });
                        headerType.Add(property);
                    }
                    else
                    {
                        var property = New<Property>(new
                        {
                            Name = h.Key,
                            SerializedName = h.Key,
                            RealPath = new[] {h.Key},
                            Extensions = hv.Extensions,
                            ModelType = hv.Schema.GetBuilder(this._swaggerModeler).BuildServiceType(h.Key, false),
                            Documentation = hv.Description
                        });
                        headerType.Add(property);
                    }
                };
            }
            else if (headerTypeReferences.Count == 1 
                && headerTypeReferences[0] is CompositeType singleType
                && responseHeaders.Count == 0)
            {
                headerType = singleType;
            }
            else
            {
                Logger.Instance.Log(Category.Error, "Detected invalid reference(s) to response header types." +
                                                    " 1) All references must point to the very same type." +
                                                    " 2) That type must be an object type (i.e. no array or primitive type)." +
                                                    " 3) No response may only define classical `headers`.");
                throw new CodeGenerationException("Invalid response header types.");
            }

            if (!headerType.Properties.Any())
            {
                headerType = null;
            }

            // Response format
            List<Stack<IModelType>> typesList = BuildResponses(method, headerType);

            method.ReturnType = BuildMethodReturnType(typesList, headerType);
            if (method.Responses.Count == 0)
            {
                method.ReturnType = method.DefaultResponse;
            }

            if (method.ReturnType.Headers != null)
            {
                _swaggerModeler.CodeModel.AddHeader(method.ReturnType.Headers as CompositeType);
            }

            // Copy extensions
            _operation.Extensions.ForEach(extention => method.Extensions.Add(extention.Key, extention.Value));

            return method;
        }

        private static IEnumerable<SwaggerParameter> DeduplicateParameters(IEnumerable<SwaggerParameter> parameters)
        {
            return parameters
                .Select(s =>
                {
                    // if parameter with the same name exists in Body and Path/Query then we need to give it a unique name
                    if (s.In == ParameterLocation.Body)
                    {
                        string newName = s.Name;

                        while (parameters.Any(t => t.In != ParameterLocation.Body &&
                                                   string.Equals(t.Name, newName,
                                                       StringComparison.OrdinalIgnoreCase)))
                        {
                            newName += "Body";
                        }
                        s.Name = newName;
                    }
                    // if parameter with same name exists in Query and Path, make Query one required
                    if (s.In == ParameterLocation.Query &&
                        parameters.Any(t => t.In == ParameterLocation.Path &&
                                            t.Name.EqualsIgnoreCase(s.Name)))
                    {
                        s.IsRequired = true;
                    }

                    return s;
                });
        }

        private static void BuildMethodReturnTypeStack(IModelType type, List<Stack<IModelType>> types)
        {
            var typeStack = new Stack<IModelType>();
            typeStack.Push(type);
            types.Add(typeStack);
        }

        private void BuildMethodParameters(Method method)
        {
            foreach (var swaggerParameter in DeduplicateParameters(_operation.Parameters))
            {
                var actualSwaggerParameter = _swaggerModeler.Unwrap(swaggerParameter);
                // As per the OpenAPI spec, some header parameters shall be ignored (https://github.com/OAI/OpenAPI-Specification/blob/master/versions/3.0.0.md#fixed-fields-10):
                if (actualSwaggerParameter.In == ParameterLocation.Header) {
                    switch (actualSwaggerParameter.Name) {
                        case "Accept":
                        case "Authorization":
                            continue; // ignore these
                        case "Content-Type": // special treatment for data-plane
                            // enrich Content-Type header with "consumes"
                            if (actualSwaggerParameter.Schema.Enum == null && 
                                _effectiveConsumes.Count > 1)
                            {
                                swaggerParameter.Description = actualSwaggerParameter.Description;
                                swaggerParameter.Extensions = actualSwaggerParameter.Extensions;
                                swaggerParameter.In = actualSwaggerParameter.In;
                                swaggerParameter.IsRequired = actualSwaggerParameter.IsRequired;
                                swaggerParameter.Name = actualSwaggerParameter.Name;
                                swaggerParameter.Schema = actualSwaggerParameter.Schema;
                                swaggerParameter.Schema.Enum = _effectiveConsumes.StringsToTokens().ToList();

                                // if not treated explicitly, add choices to the global choices
                                if (swaggerParameter.Extensions.GetValue<JObject>("x-ms-enum") == null) {
                                    _swaggerModeler.ContentTypeChoices.UnionWith(_effectiveConsumes);
                                }

                                var ctParameter = ((ParameterBuilder)swaggerParameter.GetBuilder(_swaggerModeler)).Build();
                                // you have to specify the content type, even if the OpenAPI definition claims it's optional
                                ctParameter.IsRequired = true;
                                method.Add(ctParameter);
                                continue;
                            }
                            break;
                    }
                }

                OnBuildMethodParameter(method, swaggerParameter, swaggerParameter.Name);
                var parameter = ((ParameterBuilder)swaggerParameter.GetBuilder(_swaggerModeler)).Build();
                method.Add(parameter);
            }
        }

        private static void OnBuildMethodParameter(Method method,
            SwaggerParameter currentSwaggerParam,
            string paramNameBuilder)
        {
            if (currentSwaggerParam == null)
            {
                throw new ArgumentNullException("currentSwaggerParam");
            }

            bool hasCollectionFormat = currentSwaggerParam.CollectionFormat != CollectionFormat.None;

            if (currentSwaggerParam.Schema?.Type == DataType.Array && !hasCollectionFormat)
            {
                // If the parameter type is array default the collectionFormat to csv
                currentSwaggerParam.Style = ParameterStyle.Form;
            }

            if (hasCollectionFormat && currentSwaggerParam.In == ParameterLocation.Path)
            {
                if (method?.Url == null)
                {
                    throw new ArgumentNullException("method"); 
                }

                method.Url = method.Url.Replace(currentSwaggerParam.Name, paramNameBuilder);
            }
        }

        private List<Stack<IModelType>> BuildResponses(Method method, CompositeType headerType)
        {
            string methodName = method.Name;
            var typesList = new List<Stack<IModelType>>();
            foreach (var response in _operation.Responses)
            {
                if (response.Key.EqualsIgnoreCase("default"))
                {
                    TryBuildDefaultResponse(methodName, response.Value, method, headerType);
                }
                else
                {
                    if (
                        !(TryBuildResponse(methodName, response.Key.ToHttpStatusCode(), response.Value, method, typesList, headerType) ||
                          TryBuildStreamResponse(response.Key.ToHttpStatusCode(), response.Value, method, typesList, headerType) ||
                          TryBuildEmptyResponse(methodName, response.Key.ToHttpStatusCode(), response.Value, method, typesList, headerType)))
                    {
                        throw new InvalidOperationException(
                            string.Format(CultureInfo.InvariantCulture,
                            Resources.UnsupportedMimeTypeForResponseBody,
                            methodName,
                            response.Key));
                    }
                    method.Responses[response.Key.ToHttpStatusCode()].Extensions = response.Value.Extensions;
                }
            }

            return typesList;
        }

        private Response BuildMethodReturnType(List<Stack<IModelType>> types, IModelType headerType)
        {
            IModelType baseType = New<PrimaryType>(KnownPrimaryType.Object);
            // Return null if no response is specified
            if (types.Count == 0)
            {
                return new Response(null, headerType);
            }
            // Return first if only one return type
            if (types.Count == 1)
            {
                return new Response(types.First().Pop(), headerType);
            }

            // BuildParameter up type inheritance tree
            types.ForEach(typeStack =>
            {
                IModelType type = typeStack.Peek();
                while (!Equals(type, baseType))
                {
                    if (type is CompositeType && _swaggerModeler.ExtendedTypes.ContainsKey(type.Name.RawValue))
                    {
                        type = _swaggerModeler.GeneratedTypes[_swaggerModeler.ExtendedTypes[type.Name.RawValue]];
                    }
                    else
                    {
                        type = baseType;
                    }
                    typeStack.Push(type);
                }
            });

            // Eliminate commonly shared base classes
            while (!types.First().IsNullOrEmpty())
            {
                IModelType currentType = types.First().Peek();
                foreach (var typeStack in types)
                {
                    IModelType t = typeStack.Pop();
                    if (!t.StructurallyEquals(currentType))
                    {
                        return new Response(baseType, headerType);
                    }
                }
                baseType = currentType;
            }

            return new Response(baseType, headerType);
        }

        private bool TryBuildStreamResponse(HttpStatusCode responseStatusCode, OperationResponse response,
            Method method, List<Stack<IModelType>> types, IModelType headerType)
        {
            bool handled = false;
            if (SwaggerOperationProducesNotEmpty())
            {
                if (response.Schema != null)
                {
                    IModelType serviceType = response.Schema.GetBuilder(_swaggerModeler)
                        .BuildServiceType(response.Schema.Reference.StripComponentsSchemaPath(), false);

                    Debug.Assert(serviceType != null);

                    BuildMethodReturnTypeStack(serviceType, types);

                    var compositeType = serviceType as CompositeType;
                    if (compositeType != null)
                    {
                        VerifyFirstPropertyIsByteArray(method, compositeType);
                    }
                    method.Responses[responseStatusCode] = new Response(serviceType, headerType);
                    handled = true;
                }
            }
            return handled;
        }

        private void VerifyFirstPropertyIsByteArray(Method method, CompositeType serviceType)
        {
            var referenceKey = serviceType.Name.RawValue;
            var responseType = _swaggerModeler.GeneratedTypes[referenceKey];
            var property = responseType.Properties.FirstOrDefault(p => (p.ModelType as PrimaryType)?.KnownPrimaryType == KnownPrimaryType.ByteArray);
            if (property == null)
            {
                throw new KeyNotFoundException($"The 'produces' of '{method.SerializedName}' requires that schema '{referenceKey}' models a stream/binary data (e.g. 'type: string, format: binary'), however '{referenceKey}' is an object schema (which would require 'produces' of 'application/json' or 'application/xml'). Please adjust either the 'produces' or the response schema to match your service's behavior.");
            }
        }

        private bool TryBuildResponse(string methodName, HttpStatusCode responseStatusCode,
            OperationResponse response, Method method, List<Stack<IModelType>> types, IModelType headerType)
        {
            IModelType serviceType;
            if (SwaggerOperationProducesSomethingDeserializable())
            {
                if (TryBuildResponseBody(methodName, response,
                    s => GenerateResponseObjectName(s, responseStatusCode), out serviceType))
                {
                    method.Responses[responseStatusCode] = new Response(serviceType, headerType);
                    if(response.Extensions.Get<bool>("x-ms-error-response")!=true)
                    {
                        BuildMethodReturnTypeStack(serviceType, types);
                    }
                    return true;
                }
            }
            return false;
        }

        private bool TryBuildEmptyResponse(string methodName, HttpStatusCode responseStatusCode,
            OperationResponse response, Method method, List<Stack<IModelType>> types, IModelType headerType)
        {
            bool handled = false;

            if (response.Schema == null)
            {
                method.Responses[responseStatusCode] = new Response(null, headerType);
                handled = true;
            }
            else
            {
                var unwrapedSchemaProperties =
                    _swaggerModeler.Resolver.Unwrap(response.Schema).Properties;
                if (unwrapedSchemaProperties != null && unwrapedSchemaProperties.Any())
                {
                    Logger.Instance.Log(Category.Warning, Resources.NoProduceOperationWithBody,
                        methodName);
                }
            }

            return handled;
        }

        private void TryBuildDefaultResponse(string methodName, OperationResponse response, Method method, IModelType headerType)
        {
            IModelType errorModel = null;
            if (SwaggerOperationProducesSomethingDeserializable())
            {
                if (TryBuildResponseBody(methodName, response, s => GenerateErrorModelName(s), out errorModel))
                {
                    method.DefaultResponse = new Response(errorModel, headerType);
                    method.DefaultResponse.Extensions = response.Extensions;
                }
            }
        }

        private bool TryBuildResponseBody(string methodName, OperationResponse response,
            Func<string, string> typeNamer, out IModelType responseType)
        {
            bool handled = false;
            responseType = null;
            if (SwaggerOperationProducesSomethingDeserializable())
            {
                if (response.Schema != null)
                {
                    string referenceKey;
                    if (response.Schema.Reference != null)
                    {
                        referenceKey = response.Schema.Reference.StripComponentsSchemaPath();
                        response.Schema.Reference = referenceKey;
                    }
                    else
                    {
                        referenceKey = typeNamer(methodName);
                    }

                    responseType = response.Schema.GetBuilder(_swaggerModeler).BuildServiceType(referenceKey, false);
                    handled = true;
                }
            }

            return handled;
        }

        private bool SwaggerOperationProducesSomethingDeserializable()
        {
            return true == _effectiveProduces?.Any(s => s.StartsWith(APP_JSON_MIME, StringComparison.OrdinalIgnoreCase) || s.StartsWith(APP_XML_MIME, StringComparison.OrdinalIgnoreCase));
        }

        private bool SwaggerOperationProducesNotEmpty() => true == _effectiveProduces?.Any();

        private void EnsureUniqueMethodName(string methodName, string methodGroup)
        {
            string serviceOperationPrefix = "";
            if (methodGroup != null)
            {
                serviceOperationPrefix = methodGroup + "_";
            }

            if (_swaggerModeler.CodeModel.Methods.Any(m => m.Group == methodGroup && m.Name == methodName))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    Resources.DuplicateOperationIdException,
                    serviceOperationPrefix + methodName));
            }
        }

        private static string GenerateResponseObjectName(string methodName, HttpStatusCode responseStatusCode)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "{0}{1}Response", methodName, responseStatusCode);
        }

        private static string GenerateErrorModelName(string methodName)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "{0}ErrorModel", methodName);
        }
    }
}
