// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AutoRest.Modeler.Model
{
    /// <summary>
    /// Class that represents Swagger 2.0 schema
    /// http://json.schemastore.org/swagger-2.0
    /// Swagger Object - https://github.com/wordnik/swagger-spec/blob/master/versions/2.0.md#swagger-object- 
    /// </summary>
    public class ServiceDefinition : SwaggerBase
    {
        public ServiceDefinition()
        {
            Components = new Components();
            Paths = new Dictionary<string, Dictionary<string, Operation>>();
            CustomPaths = new Dictionary<string, Dictionary<string, Operation>>();
            Tags = new List<Tag>();
        }

        /// <summary>
        /// Specifies the OpenApi Specification version being used. 
        /// </summary>
        public string OpenApi { get; set; }

        /// <summary>
        /// Provides metadata about the API. The metadata can be used by the clients if needed.
        /// </summary>
        public Info Info { get; set; }

        public IList<Server> Servers { get; set; }

        /// <summary>
        /// Key is actual path and the value is serializationProperty of http operations and operation objects.
        /// </summary>
        public Dictionary<string, Dictionary<string, Operation>> Paths { get; set; }

        /// <summary>
        /// Key is actual path and the value is serializationProperty of http operations and operation objects.
        /// </summary>
        [JsonProperty("x-ms-paths")]
        public Dictionary<string, Dictionary<string, Operation>> CustomPaths { get; set; }

        public Components Components { get; set; }

        /// <summary>
        /// A list of tags used by the specification with additional metadata. The order 
        /// of the tags can be used to reflect on their order by the parsing tools. Not all 
        /// tags that are used by the Operation Object must be declared. The tags that are 
        /// not declared may be organized randomly or based on the tools' logic. Each 
        /// tag name in the list MUST be unique.
        /// </summary>
        public IList<Tag> Tags { get; set; }

        /// <summary>
        /// Additional external documentation
        /// </summary>
        public ExternalDoc ExternalDocs { get; set; }
    }
}