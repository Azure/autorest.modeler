// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoRest.Core.Logging;
using AutoRest.Core.Utilities;
using AutoRest.Modeler.JsonConverters;
using AutoRest.Modeler.Model;
using AutoRest.Modeler.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AutoRest.Core.Model;

namespace AutoRest.Modeler
{
    public static class BuilderExtensions
    {
        public static IEnumerable<JToken> StringsToTokens(this IEnumerable<string> xs) => xs.Select<string, JToken>(x => x);
        public static IEnumerable<string> TokensToStrings(this IEnumerable<JToken> xs) => xs.Select<JToken, string>(x => JsonConvert.DeserializeObject<string>(JsonConvert.SerializeObject(x)));

        public static bool IsTaggedAsNoWire(this SwaggerBase item) => item.Extensions.Get<bool>("x-ms-no-wire") == true;
        public static string ForwardTo(this SwaggerBase item) => item.Extensions.GetValue<string>("x-ms-forward-to");
        public static Dictionary<string, string> Implementation(this SwaggerBase item)
        {
            var implementation = item.Extensions.GetValue<object>("x-ms-implementation");
            if (implementation != null)
            {
                if (implementation is string implAgnostic)
                {
                    var res = new Dictionary<string, string>();
                    res[""] = implAgnostic;
                    return res;
                }
                else if (implementation is JObject impl)
                {
                    return impl.ToObject<Dictionary<string, string>>();
                }
            }
            return null;
        }

        /// <summary>
        /// Removes #/components/{component}/ or url#/components/{component} from the reference path.
        /// </summary>
        private static string StripSomeComponentPath(string component, string reference)
        {
            var prefix = $"#/components/{component}/";
            if (reference != null && reference.Contains(prefix))
            {
                reference = reference.Substring(reference.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) + prefix.Length);
            }
            return reference;
        }

        public static string StripComponentsParameterPath(this string reference) => StripSomeComponentPath("parameters", reference);
        public static string StripComponentsRequestBodyPath(this string reference) => StripSomeComponentPath("requestBodies", reference);
        public static string StripComponentsSchemaPath(this string reference) => StripSomeComponentPath("schemas", reference);

        /// <summary>
        /// A schema represents a primitive type if it's not an object or it represents a dictionary
        /// Notes: 
        ///      'additionalProperties' on a type AND no defined 'properties', indicates that
        ///      this type is a Dictionary. (and is handled by ObjectBuilder)
        /// </summary>
        public static bool IsPrimitiveType(this Schema _schema)
            => (_schema.Type != null && _schema.Type != DataType.Object || (_schema.AdditionalProperties != null && _schema.Properties.IsNullOrEmpty()));

        /// <summary>
        /// A schema represents a simple primary type if it's a stream, or an object with no properties
        /// </summary>
        public static KnownPrimaryType GetSimplePrimaryType(this Schema _schema, bool generateEmptyClasses)
        {
            // If object with file format treat as stream
            if (_schema.Type != null
                && _schema.Type == DataType.Object
                && "file".EqualsIgnoreCase(_schema.Format))
            {
                return KnownPrimaryType.Stream;
            }

            // If the object does not have any properties, treat it as raw json (i.e. object)
            if ((_schema.Properties == null || !generateEmptyClasses && _schema.Properties.Count == 0) &&
                string.IsNullOrEmpty(_schema.Extends) && _schema.AdditionalProperties == null)
            {
                return KnownPrimaryType.Object;
            }

            // The schema doesn't match any KnownPrimaryType
            return KnownPrimaryType.None;
        }

        /// <summary>
        /// Determines if a constraint is supported for the SwaggerObject Type
        /// </summary>
        public static bool IsConstraintSupported(this SwaggerObject swaggerObject, string constraintName)
        {
            switch (swaggerObject.Type)
            {
                case DataType.Array:
                    return (constraintName.EqualsIgnoreCase(Constraint.MinItems.ToString()) ||
                            constraintName.EqualsIgnoreCase(Constraint.MaxItems.ToString()) ||
                            constraintName.EqualsIgnoreCase(Constraint.UniqueItems.ToString()));
                case DataType.Integer:
                case DataType.Number:
                    return constraintName.EqualsIgnoreCase(Constraint.ExclusiveMaximum.ToString()) ||
                           constraintName.EqualsIgnoreCase(Constraint.ExclusiveMinimum.ToString()) ||
                           constraintName.EqualsIgnoreCase(Constraint.MultipleOf.ToString()) ||
                           constraintName.EqualsIgnoreCase("minimum") || constraintName.EqualsIgnoreCase("maximum");
                case DataType.String:
                    return (constraintName.EqualsIgnoreCase(Constraint.MinLength.ToString()) ||
                            constraintName.EqualsIgnoreCase(Constraint.MaxLength.ToString()) ||
                            constraintName.EqualsIgnoreCase(Constraint.Pattern.ToString()));
                 default:
                    return false;
            }
        }
    }
}