// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AutoRest.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

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

        // TODO: get rid of this
        public SwaggerParameter AsParameter() => new SwaggerParameter
        {
            Description = Description,
            In = ParameterLocation.Body,
            Name = "body", // TODO
            IsRequired = Required,
            Schema = Content?.Values.FirstOrDefault()?.Schema
        };
        public int Index => 0; // TODO

        public Dictionary<string, MediaTypeObject> Content { get; set; }

        public bool Required { get; set; }
    }
}