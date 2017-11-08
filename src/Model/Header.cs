// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;

namespace AutoRest.Modeler.Model
{
    /// <summary>
    /// Swagger header object.
    /// </summary>
    public class Header : SwaggerBase
    {
        public string Description { get; set; }

        [JsonProperty(PropertyName = "$ref")]
        public string Reference { get; set; }

        [JsonProperty(PropertyName = "required")]
        public virtual bool IsRequired { get; set; }

        /// <summary>
        /// The schema defining the type used for the body parameter.
        /// </summary>
        public Schema Schema { get; set; }
    }
}