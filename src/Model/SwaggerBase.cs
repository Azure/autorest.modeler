// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AutoRest.Modeler.Model
{
    public enum EntityType { Type, Parameter, Property, Operation }

    public class SwaggerBase
    {
        public SwaggerBase()
        {
            Extensions = new Dictionary<string, object>();
        }

        /// <summary>
        /// Vendor extensions.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> Extensions { get; set; }

        public bool Deprecated { get; set; }

        /// <summary>
        /// Indicates whether this entity is deprecated (if "!= null") and if so, returns a corresponding message.
        /// </summary>
        public string GetDeprecationMessage(EntityType entityType)
        {
            var genericMessage = $"This {entityType.ToString().ToLowerInvariant()} is deprecated.";
            var extension = Extensions.GetValueOrDefault("x-deprecated");
            var extensionObj = extension as JObject;
            var extDescription = extensionObj?["description"]?.ToString();
            var extReplacedBy = extensionObj?["replaced-by"]?.ToString();
            if (extDescription != null)
            {
                return extDescription;
            }
            if (extReplacedBy != null)
            {
                return $"{genericMessage} Please use {extReplacedBy} instead.";
            }
            if (Deprecated || extension != null)
            {
                return $"{genericMessage} Please do not use it any longer.";
            }
            return null;
        }

        public ObjectBuilder GetBuilder(SwaggerModeler swaggerSpecBuilder)
        {
            if (this is SwaggerParameter)
            {
                return new ParameterBuilder(this as SwaggerParameter, swaggerSpecBuilder);
            }
            if (this is Schema)
            {
                return new SchemaBuilder(this as Schema, swaggerSpecBuilder);
            }
            return new ObjectBuilder(this as SwaggerObject, swaggerSpecBuilder);
        }
    }
}