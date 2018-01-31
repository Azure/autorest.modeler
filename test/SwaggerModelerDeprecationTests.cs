// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using AutoRest.Core.Model;
using AutoRest.Core.Utilities;
using Xunit;
using System.Globalization;
using Newtonsoft.Json;

namespace AutoRest.Modeler.Tests
{
    [Collection("marking stuff as deprecated")]
    public class SwaggerModelerDeprecationTests
    {
        string CodeBaseDirectory => Directory.GetParent( typeof(SwaggerModelerTests).GetAssembly().Location).ToString();

        [Fact]
        public void GenerateCodeModel()
        {
            var input = Path.Combine(CodeBaseDirectory, "..", "..", "..", "Resource", "Swagger", "deprecated.yaml");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            var output = Path.Combine(CodeBaseDirectory, "..", "..", "..", "Expected", "deprecated", "deprecated.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonConvert.SerializeObject(codeModel, Formatting.Indented));
        }
    }
}
