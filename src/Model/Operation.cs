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
        public IList<string> Consumes
        {
            get
            {
                var result = RequestBody?.Content?.Keys.ToList();
                if (result == null || result.Count == 0) return new List<string> { "application/json" };
                return result;
            }
        }

        // TODO: fix/remove
        public IList<string> Produces
        {
            get
            {
                var result = Responses?.Values.SelectMany(r => r.Content?.Keys ?? Enumerable.Empty<string>()).Distinct().ToList();
                if (result == null || result.Count == 0 || result.Count == 1 && result[0] == "*/*") return new List<string> { "application/json" };
                return result;
            }
        }

        [JsonIgnore]
        IList<SwaggerParameter> _parameters;
        /// <summary>
        /// A list of parameters that are applicable for this operation. 
        /// If a parameter is already defined at the Path Item, the 
        /// new definition will override it, but can never remove it.
        /// </summary>
        [JsonProperty(PropertyName = "parameters")]
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
            set => _parameters = value.Where(v => v.In != ParameterLocation.Body).ToList();
        } // TODO: not like this...

        public RequestBody RequestBody { get; set; }

        /// <summary>
        /// The list of possible responses as they are returned from executing this operation.
        /// </summary>
        public Dictionary<string, OperationResponse> Responses { get; set; }

        /// <summary>
        /// The transfer protocol for the operation. 
        /// </summary>
        public IList<TransferProtocolScheme> Schemes { get; set; }

        public bool Deprecated { get; set; }

        /// <summary>
        /// A declaration of which security schemes are applied for this operation. 
        /// The list of values describes alternative security schemes that can be used 
        /// (that is, there is a logical OR between the security requirements). 
        /// This definition overrides any declared top-level security. To remove a 
        /// top-level security declaration, an empty array can be used.
        /// </summary>
        public IList<Dictionary<string, List<string>>> Security { get; set; }

        private SwaggerParameter FindParameter(string name, IEnumerable<SwaggerParameter> operationParameters, IDictionary<string, SwaggerParameter> clientParameters)
        {
            if (Parameters != null)
            {
                foreach (var param in operationParameters)
                {
                    if (name.Equals(param.Name))
                        return param;

                    var pRef = FindReferencedParameter(param.Reference, clientParameters);

                    if (pRef != null && name.Equals(pRef.Name))
                    {
                        return pRef;
                    }
                }
            }
            return null;
        }

        private OperationResponse FindResponse(string name, IDictionary<string, OperationResponse> responses)
        {
            OperationResponse response = null;
            this.Responses.TryGetValue(name, out response);
            return response;
        }


        private static SwaggerParameter FindReferencedParameter(string reference, IDictionary<string, SwaggerParameter> parameters)
        {
            if (reference != null && reference.StartsWith("#", StringComparison.Ordinal))
            {
                var parts = reference.Split('/');
                if (parts.Length == 3 && parts[1].Equals("parameters"))
                {
                    SwaggerParameter p = null;
                    if (parameters.TryGetValue(parts[2], out p))
                    {
                        return p;
                    }
                }
            }

            return null;
        }
    }
}