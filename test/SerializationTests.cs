using System.IO;
using AutoRest.Modeler;
using AutoRest.Modeler.Model;
using AutoRest.Swagger;
using Xunit;

namespace AutoRest.CSharp.Unit.Tests
{
    public class SerializationTests
    {
        [Fact]
        public void ParseSecurityDefinitionType()
        {
            var filePath = Path.Combine("Resource", "SerializationTests", "SerializationTests.json");
            var swaggerContent = File.ReadAllText(filePath);
            var definition = SwaggerParser.Parse(swaggerContent);
            Assert.Equal(SecuritySchemeType.OAuth2, definition.Components.SecuritySchemes["petstore_auth"].SecuritySchemeType);
            Assert.Equal(SecuritySchemeType.ApiKey, definition.Components.SecuritySchemes["api_key"].SecuritySchemeType);
        }
    }
}