using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoRest.Core;
using AutoRest.Core.Model;
using AutoRest.Core.Utilities;
using AutoRest.Core.Utilities.Collections;
using AutoRest.Core.Parsing;
using Microsoft.Perks.JsonRPC;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.RepresentationModel;

namespace AutoRest.Modeler
{
    public class CmContractResolver : CamelCaseContractResolver
    {
        /// <summary>
        ///     Overriden to suppress serialization of IEnumerables that are empty.
        /// </summary>
        /// <param name="member"></param>
        /// <param name="memberSerialization"></param>
        /// <returns></returns>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            // if the property is marked as a JsonObject, it should never be treated as a collection
            // and hence, doesn't need our ShouldSerialize overload.
            if (property.PropertyType.CustomAttributes().Any(each => each.AttributeType == typeof(JsonObjectAttribute)))
            {
                return property;
            }

            // if the property is an IEnumerable, put a ShouldSerialize delegate on it to check if it's empty before we bother serializing
            if ((property.PropertyType != typeof(string)) && typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
            {
                property.ShouldSerialize = instance =>
                {
                    IEnumerable enumerable = null;

                    // this value could be in a public field or public property
                    if (member is PropertyInfo )
                    {
                        enumerable = instance
                            .GetType()
                            .GetProperty(member.Name)
                            .GetValue(instance, null) as IEnumerable;
                    }
                    if( member is FieldInfo ) { 
                            enumerable = instance
                                .GetType()
                                .GetField(member.Name)
                                .GetValue(instance) as IEnumerable;
                    }

                    return (enumerable == null) || enumerable.GetEnumerator().MoveNext();
                };
            }
            return property;
        }
    }

    public class Program : NewPlugin
    {
        public static int Main(string[] args )
        {
            if(args != null && args.Length > 0 && args[0] == "--server") {
                var connection = new Connection(Console.OpenStandardOutput(), Console.OpenStandardInput());
                connection.Dispatch<IEnumerable<string>>("GetPluginNames", async () => new []{ "imodeler1" });
                connection.Dispatch<string, string, bool>("Process", (plugin, sessionId) => new Program(connection, plugin, sessionId).Process());
                connection.DispatchNotification("Shutdown", connection.Stop);

                // wait for something to do.
                connection.GetAwaiter().GetResult();

                Console.Error.WriteLine("Shutting Down");
                return 0;
            }
            Console.WriteLine("This is not an entry point.");
            Console.WriteLine("Please invoke this extension through AutoRest.");
            return 1;
        }

        public Program(Connection connection, string plugin, string sessionId) : base(connection, plugin, sessionId) { }

        protected override async Task<bool> ProcessInternal()
        {
            var settings = new Settings
            {
                Namespace = await GetValue("namespace") ?? ""
            };

            var files = await ListInputs();
            if (files.Length != 1)
            {
                return false;
            }

            var content = await ReadFile(files[0]);
            var fs = new MemoryFileSystem();
            fs.WriteAllText(files[0], content);

            var serviceDefinition = SwaggerParser.Parse(fs.ReadAllText(files[0]));
            var modeler = new SwaggerModeler(settings, true == await GetValue<bool?>("generate-empty-classes"));
            var codeModel = modeler.Build(serviceDefinition);

            var modelAsJson = JsonConvert.SerializeObject(codeModel, new JsonSerializerSettings
                {
                    Converters = { new StringEnumConverter { CamelCaseText = true } },
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new CmContractResolver()
                });
            var yaml = modelAsJson.ParseYaml();
            // wire up references/anchors
            yaml.Accept(new YamlAnchorAssignerVisitor());
            modelAsJson = yaml.Serialize();

            WriteFile("code-model-v1.yaml", modelAsJson, null);

            return true;
        }
    }

    class YamlAnchorAssignerVisitor : IYamlVisitor
    {
        public void Visit(YamlStream stream)
        {
        }

        public void Visit(YamlDocument document)
        {
        }

        public void Visit(YamlScalarNode scalar)
        {
        }

        public void Visit(YamlSequenceNode sequence)
        {
            foreach (var child in sequence.Children)
            {
                child.Accept(this);
            }
        }

        public void Visit(YamlMappingNode mapping)
        {
            foreach (var child in mapping.Children.Values)
            {
                child.Accept(this);
            }

            var keyId = new YamlScalarNode("$id");
            var keyRef = new YamlScalarNode("$ref");
            YamlNode anchor = null;
            if (mapping.Children.TryGetValue(keyId, out anchor) ||
                mapping.Children.TryGetValue(keyRef, out anchor))
            {
                if (int.TryParse(anchor.ToString(), out int anchorNumber))
                {
                    mapping.Children.Remove(keyId);
                    mapping.Children.Remove(keyRef);
                    mapping.Anchor = anchorNumber.ToString();
                }
            }
        }
    }
}
