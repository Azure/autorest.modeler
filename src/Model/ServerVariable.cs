// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace AutoRest.Modeler.Model
{
    public class ServerVariable : SwaggerBase
    {
        public IList<string> Enum { get; set; }
        public string Default { get; set; }
        public string Description { get; set; }
    }
}