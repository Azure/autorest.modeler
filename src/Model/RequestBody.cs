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
                Func<string, bool> isFormDataMimeType = type => type == "multipart/form-data" || type == "application/x-www-form-urlencoded";
                if (isFormDataMimeType(Content?.Keys?.FirstOrDefault()) && Content.Values.First().Schema != null) // => in: formData
                {
                    var schema = Content.Values.First().Schema;
                    asParamCache = schema.Properties.Select(prop =>
                        new SwaggerParameter
                        {
                            Description = prop.Value.Description,
                            In = ParameterLocation.FormData,
                            Name = prop.Key,
                            IsRequired = schema.Required?.Contains(prop.Key) ?? false,
                            Schema = prop.Value,
                            Extensions = schema.Extensions,
                            Style = prop.Value?.Style
                        });
                }
                else // => in: body
                {
                    var schema = Content?.Values.FirstOrDefault()?.Schema;
                    var p = new SwaggerParameter
                    {
                        Description = Description,
                        In = ParameterLocation.Body,
                        Name = Extensions.GetValue<string>("x-ms-requestBody-name") ?? "body",
                        IsRequired = Required,
                        Schema = schema,
                        Reference = Reference,
                        Extensions = Extensions,
                        Style = schema?.Style
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
