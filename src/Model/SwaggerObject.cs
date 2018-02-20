// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using AutoRest.Core.Model;
using AutoRest.Core.Utilities;
using AutoRest.Modeler.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static AutoRest.Core.Utilities.DependencyInjection;

namespace AutoRest.Modeler.Model
{
    /// <summary>
    /// Describes a single operation determining with this object is mandatory.
    /// https://github.com/wordnik/swagger-spec/blob/master/versions/2.0.md#parameterObject
    /// </summary>
    public abstract class SwaggerObject : SwaggerBase
    {
        private string _description;

        /// <summary>
        /// The type of the parameter.
        /// </summary>
        public DataType? Type { get; set; }

        /// <summary>
        /// The extending format for the previously mentioned type.
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Returns the KnownFormat of the Format string (provided it matches a KnownFormat)
        /// Otherwise, returns KnownFormat.none
        /// </summary>
        public KnownFormat KnownFormat => KnownFormatExtensions.Parse(Format);

        /// <summary>
        /// Describes the type of items in the array.
        /// </summary>
        public Schema Items { get; set; }

        [JsonProperty(PropertyName = "$ref")]
        public string Reference { get; set; }

        /// <summary>
        /// Describes the type of additional properties in the data type.
        /// </summary>
        public Schema AdditionalProperties { get; set; }

        public string Description
        {
            get { return _description; }
            set { _description = value.StripControlCharacters(); }
        }

        /// <summary>
        /// Sets a default value to the parameter.
        /// </summary>
        public string Default { get; set; }

        public string MultipleOf { get; set; }

        public string Maximum { get; set; }

        public bool ExclusiveMaximum { get; set; }

        public string Minimum { get; set; }

        public bool ExclusiveMinimum { get; set; }

        public string MaxLength { get; set; }

        public string MinLength { get; set; }

        public string Pattern { get; set; }

        public string MaxItems { get; set; }

        public string MinItems { get; set; }

        public bool UniqueItems { get; set; }

        public IList<JToken> Enum { get; set; }

        /// <summary>
        /// Returns the PrimaryType that the SwaggerObject maps to, given the Type and the KnownFormat.
        /// 
        /// Note: Since a given language still may interpret the value of the Format after this, 
        /// it is possible the final implemented type may not be the type given here. 
        /// 
        /// This allows languages to not have a specific PrimaryType decided by the Modeler.
        /// 
        /// For example, if the Type is DataType.String, and the KnownFormat is 'char' the C# generator 
        /// will end up creating a char type in the generated code, but other languages will still 
        /// use string.
        /// </summary>
        /// <returns>
        /// The PrimaryType that best represents this object.
        /// </returns>
        public PrimaryType ToType()
        {
            switch (Type)
            {
                case DataType.String:
                    switch (KnownFormat)
                    {
                        case KnownFormat.date:
                        return New<PrimaryType>(KnownPrimaryType.Date);
                        case KnownFormat.date_time:
                        return New<PrimaryType>(KnownPrimaryType.DateTime);
                        case KnownFormat.date_time_rfc1123:
                        return New<PrimaryType>(KnownPrimaryType.DateTimeRfc1123);
                        case KnownFormat.@byte:
                        return New<PrimaryType>(KnownPrimaryType.ByteArray);
                        case KnownFormat.duration:
                        return New<PrimaryType>(KnownPrimaryType.TimeSpan);
                        case KnownFormat.uuid:
                        return New<PrimaryType>(KnownPrimaryType.Uuid);
                        case KnownFormat.base64url:
                        return New<PrimaryType>(KnownPrimaryType.Base64Url);
                        default:
                            return New<PrimaryType>(KnownPrimaryType.String);
                    }
                   
                case DataType.Number:
                    switch (KnownFormat)
                    {
                        case KnownFormat.@decimal:
                        return New<PrimaryType>(KnownPrimaryType.Decimal);
                        default:
                            return New<PrimaryType>(KnownPrimaryType.Double);
                    }

                case DataType.Integer:
                    switch (KnownFormat)
                    {
                        case KnownFormat.int64:
                        return New<PrimaryType>(KnownPrimaryType.Long);
                        case KnownFormat.unixtime:
                        return New<PrimaryType>(KnownPrimaryType.UnixTime);
                        default:
                            return New<PrimaryType>(KnownPrimaryType.Int);
                    }

                case DataType.Boolean:
                    return New<PrimaryType>(KnownPrimaryType.Boolean);
                case DataType.Object:
                case DataType.Array:
                case null:
                    return New<PrimaryType>(KnownPrimaryType.Object);
                case DataType.File:
                    return New<PrimaryType>(KnownPrimaryType.Stream);
                default:
                    throw new NotImplementedException(
                        string.Format(CultureInfo.InvariantCulture,
                           Resources.InvalidTypeInSwaggerSchema,
                            Type));
            }
        }
    }
}