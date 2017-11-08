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
    public class OperationResponse : SwaggerBase
    {
        private string _description;

        public string Description
        {
            get { return _description; }
            set { _description = value.StripControlCharacters(); }
        }

        // TODO: get rid of this
        public Schema Schema => Content?.Values.FirstOrDefault()?.Schema;

        public Dictionary<string, MediaTypeObject> Content { get; set; }

        public Dictionary<string, Header> Headers { get; set; }

        public Dictionary<string, object> Examples { get; set; }
    }
}