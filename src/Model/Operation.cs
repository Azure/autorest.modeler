// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
using System;
using System.Linq;
using System.Collections.Generic;
using AutoRest.Core.Utilities;
using Newtonsoft.Json;

namespace AutoRest.Modeler.Model
{
    /// <summary>
    /// Describes a single API operation on a path.
    /// </summary>
    public class Operation : SwaggerBase
    {
        private string _description;
        private string _summary;

        public Operation()
        {
        }

        /// <summary>
        /// A list of tags for API documentation control.
        /// </summary>
        public IList<string> Tags { get; set; }

        /// <summary>
        /// A friendly serviceTypeName for the operation. The id MUST be unique among all 
        /// operations described in the API. Tools and libraries MAY use the 
        /// operation id to uniquely identify an operation.
        /// </summary>
        public string OperationId { get; set; }

        public string Summary
        {
            get { return _summary; }
            set { _summary = value.StripControlCharacters(); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value.StripControlCharacters(); }
        }

        /// <summary>
        /// Additional external documentation for this operation.
        /// </summary>
        public ExternalDoc ExternalDocs { get; set; }

        // TODO: fix/remove
        public IEnumerable<string> GetConsumes(Dictionary<string, RequestBody> requestBodies)
        {
            var body = RequestBody;
            if (body?.Reference != null)
            {
                body = requestBodies[body.Reference.StripComponentsRequestBodyPath()];
            }
            var result = body?.Content?.Keys.ToList();
            if (result == null || result.Count == 0) return new List<string> { "application/json" };
            return result;
        }

        // TODO: fix/remove
        public IEnumerable<string> GetProduces()
        {
            var result = Responses?.Values.SelectMany(r => r.Content?.Keys ?? Enumerable.Empty<string>()).Distinct().ToList();
            if (result == null || result.Count == 0 || result.Count == 1 && result[0] == "*/*") return new List<string> { "application/json" };
            return result;
        }

        [JsonProperty(PropertyName = "parameters")]
        private IList<SwaggerParameter> _parameters;


        /// <summary>
        /// A list of parameters that are applicable for this operation. 
        /// If a parameter is already defined at the Path Item, the 
        /// new definition will override it, but can never remove it.
        /// </summary>
        [JsonIgnore]
        public SwaggerParameter[] Parameters
        {
            get
            {
                var result = _parameters?.ToList() ?? new List<SwaggerParameter>();
                if (RequestBody != null)
                {
                    result.InsertRange(Math.Min(Extensions.Get<int>("x-ms-requestBody-index") ?? 0, result.Count), RequestBody.AsParameters());
                }
                return result.ToArray();
            }
        }

        public RequestBody RequestBody { get; set; }

        /// <summary>
        /// The list of possible responses as they are returned from executing this operation.
        /// </summary>
        public Dictionary<string, OperationResponse> Responses { get; set; }
    }
}