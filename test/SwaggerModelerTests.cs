﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using AutoRest.Core.Model;
using AutoRest.Core.Utilities;
using Xunit;
using System.Globalization;

namespace AutoRest.Modeler.Tests
{
    [Collection("AutoRest Tests")]
    public class SwaggerModelerTests
    {
        string CodeBaseDirectory => Directory.GetParent( typeof(SwaggerModelerTests).GetAssembly().Location).ToString();

        public SwaggerModelerTests()
        {
            Directory.SetCurrentDirectory(CodeBaseDirectory);
        }

        private string CreateCSharpDeclarationString(Parameter parameter)
        {
            return $"{parameter.ModelType.Name} {parameter.Name}";
        }

        private string CreateCSharpResponseType(Response response)
        {
            if (response.Body != null && response.Headers != null)
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "HttpOperationResponse<{0},{1}>", response.Body, response.Headers);
            }
            else if (response.Body != null)
            {
                return response.Body.Name;
            }
            else if (response.Headers != null)
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "HttpOperationResponse<object,{0}>", response.Headers);
            }
            else
            {
                return "void";
            }
        }

        [Fact]
        public void TestcodeModelFromSimpleSwagger()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-simple-spec.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            var description =
                "The Products endpoint returns information about the Uber products offered at a given location. The response includes the display name and other details about each product, and lists the products in the proper display order.";
            var summary = "Product Types";

            Assert.NotNull(codeModel);
            Assert.Equal(2, codeModel.Properties.Count);
            Assert.True(
                codeModel.Properties.Any(p => p.Name.EqualsIgnoreCase("subscriptionId")));
            Assert.True(
                codeModel.Properties.Any(p => p.Name.EqualsIgnoreCase("apiVersion")));
            Assert.Equal("2014-04-01-preview", codeModel.ApiVersion);
            Assert.Equal("https://management.azure.com/", codeModel.BaseUrl);
            Assert.Equal("Some cool documentation.", codeModel.Documentation);
            //var allMethods = codeModel.Operations.SelectMany(each => each.Methods);
            Assert.Equal(2, codeModel.Methods.Count);
            Assert.Equal("List", codeModel.Methods[0].Name);
            Assert.NotEmpty(codeModel.Methods[0].Description);
            Assert.Equal(description, codeModel.Methods[0].Description);
            Assert.NotEmpty(codeModel.Methods[0].Summary);
            Assert.Equal(summary, codeModel.Methods[0].Summary);
            Assert.Equal(HttpMethod.Get, codeModel.Methods[0].HttpMethod);
            Assert.Equal(3, codeModel.Methods[0].Parameters.Count);
            Assert.Equal("subscriptionId", codeModel.Methods[0].Parameters[0].Name);
            Assert.NotNull(codeModel.Methods[0].Parameters[0].ClientProperty);
            Assert.Equal("resourceGroupName", codeModel.Methods[0].Parameters[1].Name);
            Assert.Equal("resourceGroupName", codeModel.Methods[0].Parameters[1].SerializedName);
            Assert.Equal("Resource Group ID.", codeModel.Methods[0].Parameters[1].Documentation);
            Assert.Equal(true, codeModel.Methods[0].Parameters[0].IsRequired);
            Assert.Equal(ParameterLocation.Path, codeModel.Methods[0].Parameters[0].Location);
            Assert.Equal("String", codeModel.Methods[0].Parameters[0].ModelType.Name);
            Assert.Equal("Reset", codeModel.Methods[1].Name);
            Assert.Equal("Product", codeModel.ModelTypes.First(m => m.Name == "Product").Name);
            Assert.Equal("Product", codeModel.ModelTypes.First(m => m.Name == "Product").SerializedName);
            Assert.Equal("The product title.", codeModel.ModelTypes.First(m => m.Name == "Product").Summary);
            Assert.Equal("The product documentation.",
                codeModel.ModelTypes.First(m => m.Name == "Product").Documentation);
            Assert.Equal("A product id.",
                codeModel.ModelTypes.First(m => m.Name == "Product").Properties[0].Summary);
            Assert.Equal("ProductId", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[0].Name);
            Assert.Equal("product_id",
                codeModel.ModelTypes.First(m => m.Name == "Product").Properties[0].SerializedName);
            Assert.Null(codeModel.Methods[1].ReturnType.Body);
            Assert.Null(codeModel.Methods[1].Responses[HttpStatusCode.NoContent].Body);
            Assert.Equal(3, codeModel.Methods[1].Parameters.Count);
            Assert.Equal("subscriptionId", codeModel.Methods[1].Parameters[0].Name);
            Assert.Null(codeModel.Methods[1].Parameters[0].ClientProperty);
            Assert.Equal("resourceGroupName", codeModel.Methods[1].Parameters[1].Name);
            Assert.Equal("apiVersion", codeModel.Methods[1].Parameters[2].Name);

            Assert.Equal("Capacity", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[3].Name);
            Assert.Equal("100", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[3].DefaultValue);
        }

        [Fact]
        public void TestcodeModelWithInheritance()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-allOf.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.NotNull(codeModel);
            Assert.Equal("Pet", codeModel.ModelTypes.First(m => m.Name == "Pet").Name);
            Assert.Equal("Cat", codeModel.ModelTypes.First(m => m.Name == "Cat").Name);
            Assert.Equal("Pet", codeModel.ModelTypes.First(m => m.Name == "Cat").BaseModelType.Name);
            Assert.Equal("Breed", codeModel.ModelTypes.First(m => m.Name == "Cat").Properties[0].Name);
            Assert.Equal(true, codeModel.ModelTypes.First(m => m.Name == "Cat").Properties[0].IsRequired);
            Assert.Equal("Color", codeModel.ModelTypes.First(m => m.Name == "Cat").Properties[1].Name);
            Assert.Equal("Siamese", codeModel.ModelTypes.First(m => m.Name == "Siamese").Name);
            Assert.Equal("Cat", codeModel.ModelTypes.First(m => m.Name == "Siamese").BaseModelType.Name);
        }

        [Fact]
        public void TestcodeModelWithXmsDiscriminatorValue()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-x-ms-discriminator-value.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.NotNull(codeModel);
            Assert.Equal("Pet", codeModel.ModelTypes.First(m => m.Name == "Pet").Name);
            Assert.Equal("Microsoft.Models.MSPet", codeModel.ModelTypes.First(m => m.Name == "Pet").Extensions["x-ms-discriminator-value"]);
            Assert.Equal("type", codeModel.ModelTypes.First(m => m.Name == "Pet").PolymorphicDiscriminator);
            Assert.Equal(true, codeModel.ModelTypes.First(m => m.Name == "Pet").IsPolymorphic);
            Assert.Equal(true, codeModel.ModelTypes.First(m => m.Name == "Pet").BaseIsPolymorphic);
            Assert.Equal("Cat", codeModel.ModelTypes.First(m => m.Name == "Cat").Name);
            Assert.Equal("Microsoft.Models.MSCat", codeModel.ModelTypes.First(m => m.Name == "Cat").Extensions["x-ms-discriminator-value"]);
            Assert.Equal(true, codeModel.ModelTypes.First(m => m.Name == "Cat").BaseIsPolymorphic);
            Assert.Equal("Pet", codeModel.ModelTypes.First(m => m.Name == "Cat").BaseModelType.Name);
        }

        [Fact]
        public void TestcodeModelPolymorhism()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-polymorphism.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.NotNull(codeModel);
            Assert.Equal("Pet", codeModel.ModelTypes.First(m => m.Name == "Pet").Name);
            Assert.Equal("dtype", codeModel.ModelTypes.First(m => m.Name == "Pet").PolymorphicDiscriminator);
            Assert.Equal(2, codeModel.ModelTypes.First(m => m.Name == "Pet").Properties.Count);
            Assert.Equal("Id", codeModel.ModelTypes.First(m => m.Name == "Pet").Properties[0].Name);
            Assert.Equal("Description", codeModel.ModelTypes.First(m => m.Name == "Pet").Properties[1].Name);
            Assert.Equal("Cat", codeModel.ModelTypes.First(m => m.Name == "Cat").Name);
            Assert.Equal("Pet", codeModel.ModelTypes.First(m => m.Name == "Cat").BaseModelType.Name);
            Assert.Equal(1, codeModel.ModelTypes.First(m => m.Name == "Cat").Properties.Count);
            Assert.Equal("Lizard", codeModel.ModelTypes.First(m => m.Name == "Lizard").Name);
            Assert.Equal("lzd", codeModel.ModelTypes.First(m => m.Name == "Lizard").SerializedName);
        }

        // [Fact] skipping - test runner dies.
        public void codeModelWithCircularDependencyThrowsError()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-allOf-circular.json");
            var modeler = new SwaggerModeler();
            var ex = Assert.Throws<InvalidOperationException>(() => modeler.Build(SwaggerParser.Parse(File.ReadAllText(input))));
            Assert.Contains("circular", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("siamese", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TestcodeModelWithRecursiveTypes()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-recursive-type.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.NotNull(codeModel);
            Assert.Equal("Product", codeModel.ModelTypes.First(m => m.Name == "Product").Name);
            Assert.Equal("ProductId", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[0].Name);
            Assert.Equal("String",
                codeModel.ModelTypes.First(m => m.Name == "Product").Properties[0].ModelType.Name);
        }

        [Fact]
        public void TestcodeModelWithManyAllOfRelationships()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-ref-allOf-inheritance.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            // the model has a few base type relationships which should be observed:
            // RedisResource is a Resource
            var resourceModel = codeModel.ModelTypes.Single(x => x.Name == "Resource");
            var redisResourceModel = codeModel.ModelTypes.Single(x => x.Name == "RedisResource");
            Assert.Equal(resourceModel, redisResourceModel.BaseModelType);

            // RedisResourceWithAccessKey is a RedisResource
            var redisResponseWithAccessKeyModel =
                codeModel.ModelTypes.Single(x => x.Name == "RedisResourceWithAccessKey");
            Assert.Equal(redisResourceModel, redisResponseWithAccessKeyModel.BaseModelType);

            // RedisCreateOrUpdateParameters is a Resource
            var redisCreateUpdateParametersModel =
                codeModel.ModelTypes.Single(x => x.Name == "RedisCreateOrUpdateParameters");
            Assert.Equal(resourceModel, redisCreateUpdateParametersModel.BaseModelType);

            // RedisReadableProperties is a RedisProperties
            var redisPropertiesModel = codeModel.ModelTypes.Single(x => x.Name == "RedisProperties");
            var redisReadablePropertieModel = codeModel.ModelTypes.Single(x => x.Name == "RedisReadableProperties");
            Assert.Equal(redisPropertiesModel, redisReadablePropertieModel.BaseModelType);

            // RedisReadablePropertiesWithAccessKey is a RedisReadableProperties
            var redisReadablePropertiesWithAccessKeysModel =
                codeModel.ModelTypes.Single(x => x.Name == "RedisReadablePropertiesWithAccessKey");
            Assert.Equal(redisReadablePropertieModel, redisReadablePropertiesWithAccessKeysModel.BaseModelType);
        }

        [Fact]
        public void TestcodeModelWithNoContent()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-no-content.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.Equal("DeleteBlob", codeModel.Methods[4].Name);
            Assert.True(codeModel.Methods[4].ReturnType.Body.IsPrimaryType(KnownPrimaryType.Object));
            Assert.True(
                codeModel.Methods[4].Responses[HttpStatusCode.OK].Body.IsPrimaryType(KnownPrimaryType.Object));
            Assert.Null(codeModel.Methods[4].Responses[HttpStatusCode.BadRequest].Body);
        }

        [Fact]
        public void TestcodeModelWithDifferentReturnsTypesBasedOnStatusCode()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-multiple-response-schemas.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.NotNull(codeModel);
            Assert.Equal("GetSameResponse", codeModel.Methods[0].Name);
            Assert.Equal("IList<Pet>", CreateCSharpResponseType(codeModel.Methods[0].ReturnType));
            Assert.Equal("IList<Pet>", CreateCSharpResponseType(codeModel.Methods[0].Responses[HttpStatusCode.OK]));
            Assert.Equal("IList<Pet>", CreateCSharpResponseType(codeModel.Methods[0].Responses[HttpStatusCode.Accepted]));

            Assert.Equal("PostInheretedTypes", codeModel.Methods[1].Name);
            Assert.Equal("Pet", CreateCSharpResponseType(codeModel.Methods[1].ReturnType));
            Assert.Equal("Dog", CreateCSharpResponseType(codeModel.Methods[1].Responses[HttpStatusCode.OK]));
            Assert.Equal("Cat", CreateCSharpResponseType(codeModel.Methods[1].Responses[HttpStatusCode.Accepted]));

            Assert.Equal("PatchDifferentStreamTypesNoContent", codeModel.Methods[6].Name);
            Assert.Equal("VirtualMachineGetRemoteDesktopFileResponse", CreateCSharpResponseType(codeModel.Methods[6].ReturnType));
            Assert.Equal("VirtualMachineGetRemoteDesktopFileResponse", CreateCSharpResponseType(codeModel.Methods[6].Responses[HttpStatusCode.OK]));
            Assert.Null(codeModel.Methods[6].Responses[HttpStatusCode.NoContent].Body);
        }

        [Fact]
        public void DefaultReturnsCorrectType()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-multiple-response-schemas.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            var retType = codeModel.Methods.First(m => m.Name == "PatchDefaultResponse");

            Assert.Equal("Pet", CreateCSharpResponseType(retType.ReturnType));
        }

        [Fact]
        public void GlobalResponsesReference()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-global-responses.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.Equal(1, codeModel.Methods[0].Responses.Count);
            Assert.NotNull(codeModel.Methods[0].Responses[HttpStatusCode.OK]);
        }

        [Fact]
        public void TestcodeModelWithStreamAndByteArray()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-streaming.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.NotNull(codeModel);
            Assert.Equal("GetWithStreamFormData", codeModel.Methods[0].Name);
            Assert.Equal("Stream", codeModel.Methods[0].Parameters[0].ModelType.Name);
            Assert.Equal("Stream", CreateCSharpResponseType(codeModel.Methods[0].ReturnType));
            Assert.Equal("Stream", CreateCSharpResponseType(codeModel.Methods[0].Responses[HttpStatusCode.OK]));

            Assert.Equal("PostWithByteArrayFormData", codeModel.Methods[1].Name);
            Assert.Equal("ByteArray", codeModel.Methods[1].Parameters[0].ModelType.Name);
            Assert.Equal("ByteArray", CreateCSharpResponseType(codeModel.Methods[1].ReturnType));
            Assert.Equal("ByteArray", CreateCSharpResponseType(codeModel.Methods[1].Responses[HttpStatusCode.OK]));

            Assert.Equal("GetWithStream", codeModel.Methods[2].Name);
            Assert.Equal("Stream", CreateCSharpResponseType(codeModel.Methods[2].ReturnType));
            Assert.Equal("Stream", CreateCSharpResponseType(codeModel.Methods[2].Responses[HttpStatusCode.OK]));

            Assert.Equal("PostWithByteArray", codeModel.Methods[3].Name);
            Assert.Equal("ByteArray", CreateCSharpResponseType(codeModel.Methods[3].ReturnType));
            Assert.Equal("ByteArray", CreateCSharpResponseType(codeModel.Methods[3].Responses[HttpStatusCode.OK]));
        }

        [Fact]
        public void TestcodeModelWithMethodGroups()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-optional-params.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.NotNull(codeModel);
            Assert.Equal(0, codeModel.Methods.Count(m => m.Group == null));
            Assert.Equal(2, codeModel.Methods.Count(m => m.Group == "Widgets"));
            Assert.Equal("List", codeModel.Methods[0].Name);
        }

        [Fact]
        public void TestDataTypes()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-data-types.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.NotNull(codeModel);
            Assert.Equal("Int integer", CreateCSharpDeclarationString(codeModel.Methods[0].Parameters[0]));
            Assert.Equal("Int int", CreateCSharpDeclarationString(codeModel.Methods[0].Parameters[1]));
            Assert.Equal("Long long", CreateCSharpDeclarationString(codeModel.Methods[0].Parameters[2]));
            Assert.Equal("Double number", CreateCSharpDeclarationString(codeModel.Methods[0].Parameters[3]));
            Assert.Equal("Double float", CreateCSharpDeclarationString(codeModel.Methods[0].Parameters[4]));
            Assert.Equal("Double double", CreateCSharpDeclarationString(codeModel.Methods[0].Parameters[5]));
            Assert.Equal("Decimal decimal", CreateCSharpDeclarationString(codeModel.Methods[0].Parameters[6]));
            Assert.Equal("String string", CreateCSharpDeclarationString(codeModel.Methods[0].Parameters[7]));
            Assert.Equal("enum color", CreateCSharpDeclarationString(codeModel.Methods[0].Parameters[8]));
            Assert.Equal("ByteArray byte", CreateCSharpDeclarationString(codeModel.Methods[0].Parameters[9]));
            Assert.Equal("Boolean boolean", CreateCSharpDeclarationString(codeModel.Methods[0].Parameters[10]));
            Assert.Equal("Date date", CreateCSharpDeclarationString(codeModel.Methods[0].Parameters[11]));
            Assert.Equal("DateTime dateTime", CreateCSharpDeclarationString(codeModel.Methods[0].Parameters[12]));
            Assert.Equal("Base64Url base64url", CreateCSharpDeclarationString(codeModel.Methods[0].Parameters[13]));
            Assert.Equal("IList<String> array", CreateCSharpDeclarationString(codeModel.Methods[0].Parameters[14]));

            var variableEnumInPath =
                codeModel.Methods.First(m => m.Name == "List" && m.Group.IsNullOrEmpty())
                    .Parameters.First(p => p.Name == "color" && p.Location == ParameterLocation.Path)
                    .ModelType as EnumType;
            Assert.NotNull(variableEnumInPath);
            Assert.Equal(variableEnumInPath.Values,
                new[] { new EnumValue { Name = "red" }, new EnumValue { Name = "blue" }, new EnumValue { Name = "green" } }
                    .ToList());
            Assert.True(variableEnumInPath.ModelAsString);
            Assert.Empty(variableEnumInPath.Name.RawValue);

            var variableEnumInQuery =
                codeModel.Methods.First(m => m.Name == "List" && m.Group.IsNullOrEmpty())
                    .Parameters.First(p => p.Name == "color1" && p.Location == ParameterLocation.Query)
                    .ModelType as EnumType;
            Assert.NotNull(variableEnumInQuery);
            Assert.Equal(variableEnumInQuery.Values,
                new[]
                {
                        new EnumValue {Name = "red"}, new EnumValue {Name = "blue"}, new EnumValue {Name = "green"},
                        new EnumValue {Name = "purple"}
                }.ToList());
            Assert.True(variableEnumInQuery.ModelAsString);
            Assert.Empty(variableEnumInQuery.Name.RawValue);

            var differentEnum =
                codeModel.Methods.First(m => m.Name == "List" && m.Group == "DiffEnums")
                    .Parameters.First(p => p.Name == "color" && p.Location == ParameterLocation.Query)
                    .ModelType as EnumType;
            Assert.NotNull(differentEnum);
            Assert.Equal(differentEnum.Values,
                new[] { new EnumValue { Name = "cyan" }, new EnumValue { Name = "yellow" } }.ToList());
            Assert.True(differentEnum.ModelAsString);
            Assert.Empty(differentEnum.Name.RawValue);

            var sameEnum =
                codeModel.Methods.First(m => m.Name == "Get" && m.Group == "SameEnums")
                    .Parameters.First(p => p.Name == "color2" && p.Location == ParameterLocation.Query)
                    .ModelType as EnumType;
            Assert.NotNull(sameEnum);
            Assert.Equal(sameEnum.Values,
                new[] { new EnumValue { Name = "blue" }, new EnumValue { Name = "green" }, new EnumValue { Name = "red" } }
                    .ToList());
            Assert.True(sameEnum.ModelAsString);
            Assert.Empty(sameEnum.Name.RawValue);

            var modelEnum =
                codeModel.ModelTypes.First(m => m.Name == "Product")
                    .Properties.First(p => p.Name == "Color2")
                    .ModelType as EnumType;
            Assert.NotNull(modelEnum);
            Assert.Equal(modelEnum.Values,
                new[] { new EnumValue { Name = "red" }, new EnumValue { Name = "blue" }, new EnumValue { Name = "green" } }
                    .ToList());
            Assert.True(modelEnum.ModelAsString);
            Assert.Empty(modelEnum.Name.RawValue);

            var fixedEnum =
                codeModel.ModelTypes.First(m => m.Name == "Product")
                    .Properties.First(p => p.Name == "Color")
                    .ModelType as EnumType;
            Assert.NotNull(fixedEnum);
            Assert.Equal(fixedEnum.Values,
                new[] { new EnumValue { Name = "red" }, new EnumValue { Name = "blue" }, new EnumValue { Name = "green" } }
                    .ToList());
            Assert.False(fixedEnum.ModelAsString);
            Assert.Equal("Colors", fixedEnum.Name);

            var fixedEnum2 =
                codeModel.ModelTypes.First(m => m.Name == "Product")
                    .Properties.First(p => p.Name == "Color3")
                    .ModelType as EnumType;
            Assert.Equal(fixedEnum2, fixedEnum);

            var refEnum =
                codeModel.ModelTypes.First(m => m.Name == "Product")
                    .Properties.First(p => p.Name == "RefColor")
                    .ModelType as EnumType;
            Assert.NotNull(refEnum);
            Assert.Equal(refEnum.Values,
                new[] { new EnumValue { Name = "red" }, new EnumValue { Name = "green" }, new EnumValue { Name = "blue" } }
                    .ToList());
            Assert.True(refEnum.ModelAsString);
            Assert.Equal("RefColors", refEnum.Name);


            Assert.Equal(2, codeModel.EnumTypes.Count);
            Assert.Equal("Colors", codeModel.EnumTypes.First().Name);
        }

        [Fact]
        public void TestClientWithValidation()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-validation.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.Equal("resourceGroupName", codeModel.Methods[0].Parameters[1].Name);
            Assert.Equal(true, codeModel.Methods[0].Parameters[1].IsRequired);
            Assert.Equal(3, codeModel.Methods[0].Parameters[1].Constraints.Count);
            Assert.Equal("10", codeModel.Methods[0].Parameters[1].Constraints[Constraint.MaxLength]);
            Assert.Equal("3", codeModel.Methods[0].Parameters[1].Constraints[Constraint.MinLength]);
            Assert.Equal("[a-zA-Z0-9]+", codeModel.Methods[0].Parameters[1].Constraints[Constraint.Pattern]);
            Assert.False(codeModel.Methods[0].Parameters[1].Constraints.ContainsKey(Constraint.MultipleOf));
            Assert.False(codeModel.Methods[0].Parameters[1].Constraints.ContainsKey(Constraint.ExclusiveMaximum));
            Assert.False(codeModel.Methods[0].Parameters[1].Constraints.ContainsKey(Constraint.ExclusiveMinimum));
            Assert.False(codeModel.Methods[0].Parameters[1].Constraints.ContainsKey(Constraint.InclusiveMinimum));
            Assert.False(codeModel.Methods[0].Parameters[1].Constraints.ContainsKey(Constraint.InclusiveMaximum));
            Assert.False(codeModel.Methods[0].Parameters[1].Constraints.ContainsKey(Constraint.MinItems));
            Assert.False(codeModel.Methods[0].Parameters[1].Constraints.ContainsKey(Constraint.MaxItems));
            Assert.False(codeModel.Methods[0].Parameters[1].Constraints.ContainsKey(Constraint.UniqueItems));


            Assert.Equal("id", codeModel.Methods[0].Parameters[2].Name);
            Assert.Equal(3, codeModel.Methods[0].Parameters[2].Constraints.Count);
            Assert.Equal("10", codeModel.Methods[0].Parameters[2].Constraints[Constraint.MultipleOf]);
            Assert.Equal("100", codeModel.Methods[0].Parameters[2].Constraints[Constraint.InclusiveMinimum]);
            Assert.Equal("1000", codeModel.Methods[0].Parameters[2].Constraints[Constraint.InclusiveMaximum]);
            Assert.False(codeModel.Methods[0].Parameters[2].Constraints.ContainsKey(Constraint.ExclusiveMaximum));
            Assert.False(codeModel.Methods[0].Parameters[2].Constraints.ContainsKey(Constraint.ExclusiveMinimum));
            Assert.False(codeModel.Methods[0].Parameters[2].Constraints.ContainsKey(Constraint.MaxLength));
            Assert.False(codeModel.Methods[0].Parameters[2].Constraints.ContainsKey(Constraint.MinLength));
            Assert.False(codeModel.Methods[0].Parameters[2].Constraints.ContainsKey(Constraint.Pattern));
            Assert.False(codeModel.Methods[0].Parameters[2].Constraints.ContainsKey(Constraint.MinItems));
            Assert.False(codeModel.Methods[0].Parameters[2].Constraints.ContainsKey(Constraint.MaxItems));
            Assert.False(codeModel.Methods[0].Parameters[2].Constraints.ContainsKey(Constraint.UniqueItems));

            Assert.Equal("apiVersion", codeModel.Methods[0].Parameters[3].Name);
            Assert.NotNull(codeModel.Methods[0].Parameters[3].ClientProperty);
            Assert.Equal(1, codeModel.Methods[0].Parameters[3].Constraints.Count);
            Assert.Equal("\\d{2}-\\d{2}-\\d{4}", codeModel.Methods[0].Parameters[3].Constraints[Constraint.Pattern]);

            Assert.Equal("Product", codeModel.ModelTypes.First(m => m.Name == "Product").Name);
            Assert.Equal("DisplayNames", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[2].Name);
            Assert.Equal(3, codeModel.ModelTypes.First(m => m.Name == "Product").Properties[2].Constraints.Count);
            Assert.Equal("6", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[2].Constraints[Constraint.MaxItems]);
            Assert.Equal("0", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[2].Constraints[Constraint.MinItems]);
            Assert.Equal("true", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[2].Constraints[Constraint.UniqueItems]);
            Assert.False(codeModel.ModelTypes.First(m => m.Name == "Product").Properties[2].Constraints.ContainsKey(Constraint.ExclusiveMaximum));
            Assert.False(codeModel.ModelTypes.First(m => m.Name == "Product").Properties[2].Constraints.ContainsKey(Constraint.ExclusiveMinimum));
            Assert.False(codeModel.ModelTypes.First(m => m.Name == "Product").Properties[2].Constraints.ContainsKey(Constraint.InclusiveMaximum));
            Assert.False(codeModel.ModelTypes.First(m => m.Name == "Product").Properties[2].Constraints.ContainsKey(Constraint.InclusiveMinimum));
            Assert.False(codeModel.ModelTypes.First(m => m.Name == "Product").Properties[2].Constraints.ContainsKey(Constraint.MultipleOf));
            Assert.False(codeModel.ModelTypes.First(m => m.Name == "Product").Properties[2].Constraints.ContainsKey(Constraint.MinLength));
            Assert.False(codeModel.ModelTypes.First(m => m.Name == "Product").Properties[2].Constraints.ContainsKey(Constraint.MaxLength));
            Assert.False(codeModel.ModelTypes.First(m => m.Name == "Product").Properties[2].Constraints.ContainsKey(Constraint.Pattern));

            Assert.Equal("Capacity", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[3].Name);
            Assert.Equal(2, codeModel.ModelTypes.First(m => m.Name == "Product").Properties[3].Constraints.Count);
            Assert.Equal("100", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[3].Constraints[Constraint.ExclusiveMaximum]);
            Assert.Equal("0", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[3].Constraints[Constraint.ExclusiveMinimum]);
        }

        [Fact]
        public void TestConstants()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-validation.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.Equal("myintconst", codeModel.Methods[0].Parameters[4].Name);
            Assert.Equal(true, codeModel.Methods[0].Parameters[4].ModelType.IsPrimaryType(KnownPrimaryType.Int));
            Assert.Equal(true, codeModel.Methods[0].Parameters[4].IsConstant);
            Assert.Equal("0", codeModel.Methods[0].Parameters[4].DefaultValue);

            Assert.Equal("mystrconst", codeModel.Methods[0].Parameters[5].Name);
            Assert.Equal(true, codeModel.Methods[0].Parameters[5].ModelType.IsPrimaryType(KnownPrimaryType.String));
            Assert.Equal(true, codeModel.Methods[0].Parameters[5].IsConstant);
            Assert.Equal("constant", codeModel.Methods[0].Parameters[5].DefaultValue);

            Assert.Equal("Myintconst", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[5].Name);
            Assert.Equal(true, codeModel.ModelTypes.First(m => m.Name == "Product").Properties[5].ModelType.IsPrimaryType(KnownPrimaryType.Int));
            Assert.Equal(true, codeModel.ModelTypes.First(m => m.Name == "Product").Properties[5].IsConstant);
            Assert.Equal("0", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[5].DefaultValue);

            Assert.Equal("Mystrconst", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[6].Name);
            Assert.Equal(true, codeModel.ModelTypes.First(m => m.Name == "Product").Properties[6].ModelType.IsPrimaryType(KnownPrimaryType.String));
            Assert.Equal(true, codeModel.ModelTypes.First(m => m.Name == "Product").Properties[6].IsConstant);
            Assert.Equal("constant", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[6].DefaultValue);

            Assert.Equal("RefStrEnumRequiredConstant", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[7].Name);
            Assert.Equal(true, codeModel.ModelTypes.First(m => m.Name == "Product").Properties[7].ModelType.IsPrimaryType(KnownPrimaryType.String));
            Assert.Equal(true, codeModel.ModelTypes.First(m => m.Name == "Product").Properties[7].IsConstant);
            Assert.Equal("ReferenceEnum1", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[7].DefaultValue);

            Assert.Equal("RefIntEnumRequiredConstant", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[8].Name);
            Assert.Equal(true, codeModel.ModelTypes.First(m => m.Name == "Product").Properties[8].ModelType.IsPrimaryType(KnownPrimaryType.Int));
            Assert.Equal(true, codeModel.ModelTypes.First(m => m.Name == "Product").Properties[8].IsConstant);
            Assert.Equal("0", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[8].DefaultValue);

            Assert.Equal("RefStrEnum", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[9].Name);
            Assert.Equal("enum", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[9].ModelType.Name);
            Assert.Equal(false, codeModel.ModelTypes.First(m => m.Name == "Product").Properties[9].IsConstant);
            Assert.True(codeModel.ModelTypes.First(m => m.Name == "Product").Properties[9].DefaultValue.IsNullOrEmpty());

            Assert.Equal("RefIntEnum", codeModel.ModelTypes.First(m => m.Name == "Product").Properties[10].Name);
            Assert.Equal(true, codeModel.ModelTypes.First(m => m.Name == "Product").Properties[10].ModelType.IsPrimaryType(KnownPrimaryType.Int));
            Assert.Equal(false, codeModel.ModelTypes.First(m => m.Name == "Product").Properties[10].IsConstant);
            Assert.True(codeModel.ModelTypes.First(m => m.Name == "Product").Properties[10].DefaultValue.IsNullOrEmpty());

            Assert.Equal(true, codeModel.ModelTypes.First(m => m.Name == "Product").ContainsConstantProperties);
            Assert.Equal(false, codeModel.ModelTypes.First(m => m.Name == "Error").ContainsConstantProperties);
        }

        [Fact]
        public void TestCompositeConstants()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-composite-constants.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.Equal(false, codeModel.ModelTypes.First(m => m.Name == "NetworkInterfaceIPConfigurationPropertiesFormat").ContainsConstantProperties);
            Assert.Equal(false, codeModel.ModelTypes.First(m => m.Name == "IPConfigurationPropertiesFormat").ContainsConstantProperties);
        }

        [Fact]
        public void TestcodeModelWithResponseHeaders()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-response-headers.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.NotNull(codeModel);
            Assert.Equal(2, codeModel.Methods.Count);
            Assert.Equal(2, codeModel.Methods[0].Responses.Count);
            Assert.Equal("ListHeaders", codeModel.Methods[0].Responses[HttpStatusCode.OK].Headers.Name);
            Assert.Equal(3, ((CompositeType)codeModel.Methods[0].Responses[HttpStatusCode.OK].Headers).Properties.Count);
            Assert.Equal("ListHeaders", codeModel.Methods[0].Responses[HttpStatusCode.Created].Headers.Name);
            Assert.Equal(3, ((CompositeType)codeModel.Methods[0].Responses[HttpStatusCode.Created].Headers).Properties.Count);
            Assert.Equal("ListHeaders", codeModel.Methods[0].ReturnType.Headers.Name);
            Assert.Equal(3, ((CompositeType)codeModel.Methods[0].ReturnType.Headers).Properties.Count);

            Assert.Equal(1, codeModel.Methods[1].Responses.Count);
            Assert.Equal("CreateHeaders", codeModel.Methods[1].Responses[HttpStatusCode.OK].Headers.Name);
            Assert.Equal(3, ((CompositeType)codeModel.Methods[1].Responses[HttpStatusCode.OK].Headers).Properties.Count);
            Assert.Equal("CreateHeaders", codeModel.Methods[1].ReturnType.Headers.Name);
            Assert.Equal(3, ((CompositeType)codeModel.Methods[1].ReturnType.Headers).Properties.Count);
            Assert.True(codeModel.HeaderTypes.Any(c => c.Name == "ListHeaders"));
            Assert.True(codeModel.HeaderTypes.Any(c => c.Name == "CreateHeaders"));
        }

        [Fact]
        public void TestCustomPaths()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-x-ms-paths.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.NotNull(codeModel);
            Assert.Equal(3, codeModel.Methods.Count);
            Assert.True(codeModel.Methods.All(m => m.Url == "/values/foo"));
        }

        [Fact]
        public void TestYamlParsing()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-simple-spec.json");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.NotNull(codeModel);
        }

        [Fact]
        public void TestAdditionalProperties()
        {
            var input = Path.Combine(CodeBaseDirectory, "Resource", "Swagger", "swagger-additional-properties.yaml");
            var modeler = new SwaggerModeler();
            var codeModel = modeler.Build(SwaggerParser.Parse(File.ReadAllText(input)));

            Assert.NotNull(codeModel);
            Assert.Equal(5, codeModel.ModelTypes.Count);

            // did we find the type?
            var wtd = codeModel.ModelTypes.FirstOrDefault(each => each.Name == "WithTypedDictionary");
            Assert.NotNull(wtd);

            // did we find the member called 'additionalProperties'
            var prop = wtd.Properties.FirstOrDefault(each => each.Name == "AdditionalProperties");
            Assert.NotNull(prop);

            // is it a DictionaryType?
            var dictionaryProperty = prop.ModelType as DictionaryType;
            Assert.NotNull(dictionaryProperty);

            // is a string,string dictionary?
            Assert.Equal("Dictionary<string,Feature>", dictionaryProperty.Name);
            Assert.Equal("Feature", dictionaryProperty.ValueType.Name);

            // is it marked as an 'additionalProperties' bucket?
            Assert.True(dictionaryProperty.SupportsAdditionalProperties);

            // did we find the type?
            var wud = codeModel.ModelTypes.FirstOrDefault(each => each.Name == "WithUntypedDictionary");
            Assert.NotNull(wud);

            // did we find the member called 'additionalProperties'
            prop = wud.Properties.FirstOrDefault(each => each.Name == "AdditionalProperties");
            Assert.NotNull(prop);

            // is it a DictionaryType?
            dictionaryProperty = prop.ModelType as DictionaryType;
            Assert.NotNull(dictionaryProperty);

            // is a string,string dictionary?
            Assert.Equal("Dictionary<string,Object>", dictionaryProperty.Name);
            Assert.Equal("Object", dictionaryProperty.ValueType.Name);

            // is it marked as an 'additionalProperties' bucket?
            Assert.True(dictionaryProperty.SupportsAdditionalProperties);

            var wsd = codeModel.ModelTypes.FirstOrDefault(each => each.Name == "WithStringDictionary");
            Assert.NotNull(wsd);

            // did we find the member called 'additionalProperties'
            prop = wsd.Properties.FirstOrDefault(each => each.Name == "AdditionalProperties");
            Assert.NotNull(prop);

            // is it a DictionaryType?
            dictionaryProperty = prop.ModelType as DictionaryType;
            Assert.NotNull(dictionaryProperty);

            // is a string,string dictionary?
            Assert.Equal("Dictionary<string,String>", dictionaryProperty.Name);
            Assert.Equal("String", dictionaryProperty.ValueType.Name);

            // is it marked as an 'additionalProperties' bucket?
            Assert.True(dictionaryProperty.SupportsAdditionalProperties);
        }
    }
}
