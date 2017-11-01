// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace AutoRest.Modeler.Model
{
    public class Server : SwaggerBase
    {
        public string Url { get; set; }
        public string Description { get; set; }
        public Dictionary<string, Schema> Variables { get; set; }
    }
}