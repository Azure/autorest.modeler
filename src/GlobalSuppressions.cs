// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Code Analysis results, point to "Suppress Message", and click 
// "In Suppression File".
// You do not need to add suppressions to this file manually.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", 
    Scope = "member", Target = "AutoRest.Modeler.Model.Operation.#Security", 
    Justification = "This type is strictly a serialization model.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", 
    Scope = "member", Target = "AutoRest.Modeler.Model.ServiceDefinition.#Paths", 
    Justification = "This type is strictly a serialization model.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", 
    Scope = "member", Target = "AutoRest.Modeler.Model.ServiceDefinition.#Security", 
    Justification = "This type is strictly a serialization model.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", 
    MessageId = "1#", Scope = "member", 
    Target = "AutoRest.Modeler.OperationBuilder.#BuildMethod(AutoRest.Core.Model.HttpMethod,System.String,System.String,System.String)", Justification = "May not parse as valid Uri")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", 
    MessageId = "1#", Scope = "member", 
    Target = "AutoRest.Modeler.SwaggerModeler.#BuildMethod(AutoRest.Core.Model.HttpMethod,System.String,System.String,AutoRest.Modeler.Model.Operation)", Justification = "May not parse as valid Uri")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings", 
    Scope = "member", Target = "AutoRest.Modeler.SwaggerModeler.#BuildMethodBaseUrl(AutoRest.Core.Model.CodeModel,System.String)", Justification = "May not parse as valid Uri")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", 
    Scope = "member", Target = "AutoRest.Modeler.Model.ExternalDoc.#Url", Justification = "May not parse as valid Uri")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", 
    Scope = "member", Target = "AutoRest.Modeler.Model.License.#Url", Justification = "May not parse as valid Uri")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "AutoRest.Core.Logging.ErrorManager.CreateError(System.String,System.Object[])", Scope = "member", Target = "AutoRest.Modeler.SwaggerParser.#Parse(System.String)", Justification = "Generated Code")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", 
    Scope = "member", Target = "AutoRest.Modeler.Extensions.#ToHttpMethod(System.String)", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", 
    Scope = "member", Target = "AutoRest.Modeler.SchemaResolver.#Dereference(System.String)", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", 
    Scope = "member", Target = "AutoRest.Modeler.SwaggerModeler.#InitializeClientModel()", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", 
    Scope = "member", Target = "AutoRest.Modeler.OperationBuilder.#BuildMethod(AutoRest.Core.Model.HttpMethod,System.String,System.String,System.String)", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", 
    MessageId = "param", Scope = "member", 
    Target = "AutoRest.Modeler.CollectionFormatBuilder.#OnBuildMethodParameter(AutoRest.Core.Model.Method,AutoRest.Modeler.Model.SwaggerParameter,System.Text.StringBuilder)", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", 
    MessageId = "Param", Scope = "member", 
    Target = "AutoRest.Modeler.CollectionFormatBuilder.#OnBuildMethodParameter(AutoRest.Core.Model.Method,AutoRest.Modeler.Model.SwaggerParameter,System.Text.StringBuilder)", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", 
    MessageId = "OAuth", Scope = "member", Target = "AutoRest.Modeler.Model.SecuritySchemeType.#OAuth2", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", 
    MessageId = "Default", Scope = "member", Target = "AutoRest.Modeler.Model.SwaggerObject.#Default", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", 
    MessageId = "Enum", Scope = "member", Target = "AutoRest.Modeler.Model.SwaggerObject.#Enum", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", 
    Scope = "member", Target = "AutoRest.Modeler.Model.SwaggerObject.#Type", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces", 
    Scope = "type", Target = "AutoRest.Modeler.Model.Schema", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", 
    Scope = "member", Target = "AutoRest.Modeler.Model.SwaggerObject.#GetBuilder(AutoRest.Modeler.SwaggerModeler)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", 
    MessageId = "operation", Scope = "member", 
    Target = "AutoRest.Modeler.OperationBuilder.#SwaggerOperationProducesJson(AutoRest.Modeler.Model.Operation)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", 
    MessageId = "operation", Scope = "member", 
    Target = "AutoRest.Modeler.OperationBuilder.#SwaggerOperationConsumesJson(AutoRest.Modeler.Model.Operation)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", 
    MessageId = "operation", Scope = "member", 
    Target = "AutoRest.Modeler.OperationBuilder.#SwaggerOperationProducesOctetStream(AutoRest.Modeler.Model.Operation)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", 
    MessageId = "operation", Scope = "member", 
    Target = "AutoRest.Modeler.OperationBuilder.#SwaggerOperationConsumesMultipartFormData(AutoRest.Modeler.Model.Operation)", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", 
    "CA1703:ResourceStringsShouldBeSpelledCorrectly", MessageId = "multi", Scope = "resource", 
    Target = "AutoRest.Modeler.Properties.Resources.resources", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", 
    MessageId = "Auth", Scope = "member", Target = "AutoRest.Modeler.Model.SecuritySchemeType.#OAuth2")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.Operation.#Tags", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.Operation.#Consumes", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.Operation.#Produces", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.Operation.#Parameters", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.Operation.#Responses", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.Operation.#Schemes", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.Operation.#Security", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.OperationResponse.#Headers", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.OperationResponse.#Examples", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.Schema.#Properties", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.Schema.#Required", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.Schema.#AllOf", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.ServiceDefinition.#Paths", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.ServiceDefinition.#SecurityDefinitions", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.ServiceDefinition.#Security", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.ServiceDefinition.#Tags", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.ServiceDefinition.#ExternalReferences", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.SwaggerBase.#Extensions", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.SwaggerObject.#Enum", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.JsonConverters.SwaggerJsonConverter.#Document", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", 
    Scope = "member", Target = "AutoRest.Modeler.Model.ServiceDefinition.#CustomPaths", Justification = "Serialization Type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "AutoRest.Modeler.SchemaBuilder.#BuildServiceType(System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "AutoRest.Core.Logging.ErrorManager.CreateError(System.String,System.Object[])", Scope = "member", Target = "AutoRest.Modeler.SwaggerModeler.#Build()")]
