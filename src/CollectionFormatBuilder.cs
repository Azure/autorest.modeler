// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using AutoRest.Core.Model;
using AutoRest.Modeler.Model;
using AutoRest.Modeler.Properties;
using ParameterLocation = AutoRest.Modeler.Model.ParameterLocation;

namespace AutoRest.Modeler
{
    public static class CollectionFormatBuilder
    {
        public static void OnBuildMethodParameter(Method method,
            SwaggerParameter currentSwaggerParam,
            string paramNameBuilder)
        {
            if (currentSwaggerParam == null)
            {
                throw new ArgumentNullException("currentSwaggerParam");
            }

            bool hasCollectionFormat = currentSwaggerParam.CollectionFormat != CollectionFormat.None;

            if (currentSwaggerParam.Schema?.Type == DataType.Array && !hasCollectionFormat)
            {
                // If the parameter type is array default the collectionFormat to csv
                currentSwaggerParam.Style = ParameterStyle.Form;
            }

            if (hasCollectionFormat && currentSwaggerParam.In == ParameterLocation.Path)
            {
                if (method?.Url == null)
                {
                    throw new ArgumentNullException("method"); 
                }

                method.Url = method.Url.Replace(currentSwaggerParam.Name, paramNameBuilder);
            }
        }
    }
}