// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using AutoRest.Core.Model;
using AutoRest.Modeler.Model;
using ParameterLocation = AutoRest.Modeler.Model.ParameterLocation;
using static AutoRest.Core.Utilities.DependencyInjection;
using AutoRest.Swagger;

namespace AutoRest.Modeler
{
    /// <summary>
    /// The builder for building swagger parameters into client model parameters, 
    /// service types or Json serialization types.
    /// </summary>
    public class ParameterBuilder : ObjectBuilder
    {
        private readonly SwaggerParameter _swaggerParameter;

        public ParameterBuilder(SwaggerParameter swaggerParameter, SwaggerModeler modeler)
            : base(swaggerParameter.Schema, modeler)
        {
            _swaggerParameter = swaggerParameter;
        }

        public Parameter Build()
        {
            string parameterName = _swaggerParameter.Name;
            SwaggerParameter unwrappedParameter = _swaggerParameter;

            if (_swaggerParameter.Reference != null)
            {
                unwrappedParameter = Modeler.Unwrap(_swaggerParameter);
            }

            if (unwrappedParameter.Schema != null && unwrappedParameter.Schema.Reference != null)
            {
                parameterName = unwrappedParameter.Schema.Reference.StripComponentsSchemaPath();
            }

            if (parameterName == null)
            {
                parameterName = unwrappedParameter.Name;
            }

            IModelType parameterType = BuildServiceType(parameterName);
            var parameter = New<Parameter>(new
            {
                Name = unwrappedParameter.Name,
                SerializedName = unwrappedParameter.Name,
                ModelType = parameterType,
                Location = (Core.Model.ParameterLocation)Enum.Parse(typeof(Core.Model.ParameterLocation), unwrappedParameter.In.ToString())
            });
            parameter.IsRequired = parameter.IsRequired || parameter.Location == Core.Model.ParameterLocation.Path;
            PopulateParameter(parameter, unwrappedParameter);

            if (_swaggerParameter.Reference != null)
            {
                var clientProperty = Modeler.CodeModel.Properties.FirstOrDefault(p => p.SerializedName == unwrappedParameter.Name);
                parameter.ClientProperty = clientProperty;
            }

            return parameter;
        }

        public override IModelType BuildServiceType(string serviceTypeName)
        {
            var swaggerParameter = Modeler.Unwrap(_swaggerParameter);

            // create service type
            if (swaggerParameter.In == ParameterLocation.Body)
            {
                if (swaggerParameter.Schema == null)
                {
                    throw new Exception($"Invalid Swagger: Body parameter{(serviceTypeName == null ? "" : $" '{serviceTypeName}'")} missing 'schema'.");
                }
                return swaggerParameter.Schema.GetBuilder(Modeler).BuildServiceType(serviceTypeName);
            }

            return swaggerParameter.GetBuilder(Modeler).ParentBuildServiceType(serviceTypeName);
        }

        public override IModelType ParentBuildServiceType(string serviceTypeName) => base.BuildServiceType(serviceTypeName);
    }
}