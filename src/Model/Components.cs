// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace AutoRest.Modeler.Model
{
    public class Components : SwaggerBase
    {
        public Components()
        {
            Schemas = new Dictionary<string, Schema>();
            Parameters = new Dictionary<string, SwaggerParameter>();
            Responses = new Dictionary<string, OperationResponse>();
            SecuritySchemes = new Dictionary<string, SecurityDefinition>();
        }

        /// <summary>
        /// Key is the object serviceTypeName and the value is swagger definition.
        /// </summary>
        public Dictionary<string, Schema> Schemas { get; set; }

        /// <summary>
        /// Dictionary of parameters that can be used across operations.
        /// This property does not define global parameters for all operations.
        /// </summary>
        public Dictionary<string, SwaggerParameter> Parameters { get; set; }

        public Dictionary<string, RequestBody> RequestBodies { get; set; }

        /// <summary>
        /// Dictionary of responses that can be used across operations. The key indicates status code.
        /// </summary>
        public Dictionary<string, OperationResponse> Responses { get; set; }

        /// <summary>
        /// Key is the object serviceTypeName and the value is swagger security definition.
        /// </summary>
        public Dictionary<string, SecurityDefinition> SecuritySchemes { get; set; }

    }
}