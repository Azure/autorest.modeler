// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace AutoRest.Modeler.Model
{
    public class Discriminator
    {
        public string PropertyName { get; set; }

        // TODO: translate x-ms-discriminator-value to this! Completely ignored so far.
        public Dictionary<string, string> Mapping { get; set; }
    }
}