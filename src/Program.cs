using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoRest.Core;
using AutoRest.Core.Model;
using AutoRest.Core.Utilities;
using Microsoft.Perks.JsonRPC;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AutoRest.Modeler
{
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
            if (true == await this.GetValue<bool?>($"modeler.debugger"))
            {
                AutoRest.Core.Utilities.Debugger.Await();
            }

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
                    ContractResolver = CodeModelContractResolver.Instance
                });

            WriteFile("code-model-v1.yaml", modelAsJson, null);

            return true;
        }
    }
}
