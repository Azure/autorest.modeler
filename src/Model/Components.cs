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
            RequestBodies = new Dictionary<string, RequestBody>();
            Responses = new Dictionary<string, OperationResponse>();
        }

        public Dictionary<string, Schema> Schemas { get; set; }

        public Dictionary<string, SwaggerParameter> Parameters { get; set; }

        public Dictionary<string, RequestBody> RequestBodies { get; set; }

        public Dictionary<string, OperationResponse> Responses { get; set; }

    }
}