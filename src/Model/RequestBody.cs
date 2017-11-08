// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AutoRest.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace AutoRest.Modeler.Model
{
    /// <summary>
    /// Describes a single response from an API Operation.
    /// </summary>
    public class RequestBody : SwaggerBase
    {
        private string _description;

        public string Description
        {
            get { return _description; }
            set { _description = value.StripControlCharacters(); }
        }

        [JsonProperty(PropertyName = "$ref")]
        public string Reference { get; set; }

        // TODO: get rid of this
        private IEnumerable<SwaggerParameter> asParamCache = null;
        public IEnumerable<SwaggerParameter> AsParameters()
        {
            if (asParamCache == null)
            {
                var isFormData = Content?.Keys?.FirstOrDefault() == "multipart/form-data" && Content.Values.First().Schema != null;
                if (isFormData) // => in: form-data
                {
                    var schema = Content.Values.First().Schema;
                    asParamCache = schema.Properties.Select(prop =>
                        new SwaggerParameter
                        {
                            Description = prop.Value.Description,
                            In = ParameterLocation.FormData,
                            Name = prop.Key,
                            IsRequired = schema.Required.Contains(prop.Key),
                            Schema = prop.Value,
                            Extensions = schema.Extensions
                        });
                }
                else // => in: body
                {
                    var p = new SwaggerParameter
                    {
                        Description = Description,
                        In = ParameterLocation.Body,
                        Name = Extensions.GetValue<string>("x-ms-requestBody-name") ?? "body",
                        IsRequired = Required,
                        Schema = Content?.Values.FirstOrDefault()?.Schema,
                        Reference = Reference,
                        Extensions = Extensions
                    };
                    asParamCache = new [] { p };
                }
            }
            return asParamCache;
        }

        public Dictionary<string, MediaTypeObject> Content { get; set; }

        public bool Required { get; set; }
    }
}