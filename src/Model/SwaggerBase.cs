// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace AutoRest.Modeler.Model
{
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