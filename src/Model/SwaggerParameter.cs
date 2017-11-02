// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Newtonsoft.Json;

namespace AutoRest.Modeler.Model
{
    /// <summary>
    /// Describes a single operation parameter.
    /// https://github.com/wordnik/swagger-spec/blob/master/versions/2.0.md#parameterObject 
    /// </summary>
    public class SwaggerParameter : Header
    {
        public string Name { get; set; }

        public ParameterLocation In { get; set; }

        [JsonProperty(PropertyName = "required")]
        public override bool IsRequired
        {
            get => base.IsRequired || In == ParameterLocation.Path;
            set => base.IsRequired = value;
        }

        [JsonIgnore]
        public bool IsConstant => IsRequired && Schema?.Enum != null && Schema?.Enum.Count == 1;
    }
}