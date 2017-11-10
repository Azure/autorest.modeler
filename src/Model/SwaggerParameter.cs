// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AutoRest.Core.Model;
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

        public ParameterStyle? Style { get; set; }

        public bool? Explode { get; set; }

        public CollectionFormat CollectionFormat
        {
            get
            {
                bool quirksMode = true; // disable/make settable as appropriate
                if (!Style.HasValue && !Explode.HasValue && quirksMode)
                {
                    return CollectionFormat.None; // WAT
                }
                var style = Style ?? (In == ParameterLocation.Query || In == ParameterLocation.Cookie ? ParameterStyle.Form : ParameterStyle.Simple);
                var explode = Explode ?? (quirksMode ? false : style == ParameterStyle.Form);
                if (explode)
                {
                    return CollectionFormat.Multi;
                }
                switch (style)
                {
                    case ParameterStyle.Form:
                        return CollectionFormat.Csv;
                    case ParameterStyle.SpaceDelimited:
                        return CollectionFormat.Ssv;
                    case ParameterStyle.PipeDelimited:
                        return CollectionFormat.Pipes;
                    case ParameterStyle.TabDelimited: //FAKE
                        return CollectionFormat.Tsv;
                }
                if (quirksMode)
                {
                    return CollectionFormat.Csv;
                }
                throw new System.NotImplementedException($"Style '{style}' is not yet supported.");
            }
        }

        [JsonProperty(PropertyName = "required")]
        public override bool IsRequired
        {
            get => base.IsRequired || In == ParameterLocation.Path;
            set => base.IsRequired = value;
        }

        [JsonIgnore]
        public bool IsConstant => IsRequired && Schema?.Enum != null && Schema?.Enum.Count == 1;

        public bool? AllowReserved { get; set; }
    }
}