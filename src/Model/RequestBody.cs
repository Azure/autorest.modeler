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
        public SwaggerParameter AsParameter() =>
            new SwaggerParameter
            {
                Description = Description,
                In = ParameterLocation.Body,
                Name = Extensions.GetValue<string>("x-ms-client-name") ?? "body",
                IsRequired = Required,
                Schema = Content?.Values.FirstOrDefault()?.Schema,
                Reference = Reference
            };

        public Dictionary<string, MediaTypeObject> Content { get; set; }

        public bool Required { get; set; }
    }
}