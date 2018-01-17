// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using AutoRest.Core;
using AutoRest.Core.Logging;
using AutoRest.Core.Parsing;
using AutoRest.Core.Utilities;
using AutoRest.Modeler.JsonConverters;
using AutoRest.Modeler.Model;
using AutoRest.Modeler.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

namespace AutoRest.Modeler
{
    public static class SwaggerParser
    {
        public static ServiceDefinition Parse(string swaggerDocument)
        {
            try
            {
                swaggerDocument = swaggerDocument.EnsureYamlIsJson();
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None,
                    MetadataPropertyHandling = MetadataPropertyHandling.Ignore
                };
                settings.Converters.Add(new ResponseRefConverter(swaggerDocument));
                settings.Converters.Add(new PathItemRefConverter(swaggerDocument));
                settings.Converters.Add(new PathLevelParameterConverter(swaggerDocument));
                var swaggerService = JsonConvert.DeserializeObject<ServiceDefinition>(swaggerDocument, settings);

                // for parameterized host, will be made available via JsonRpc accessible state in the future
                if (swaggerService.Servers == null || swaggerService.Servers.Count == 0)
                {
                    swaggerService.Servers = new List<Server>
                    {
                        new Server
                        {
                            Url = "/"
                        }
                    };
                }
                return swaggerService;
            }
            catch (JsonException ex)
            {
                throw ErrorManager.CreateError("{0}. {1}", Resources.ErrorParsingSpec, ex.Message);
            }
        }
    }
}
