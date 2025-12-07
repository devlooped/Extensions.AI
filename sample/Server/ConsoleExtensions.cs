using Devlooped.Agents.AI;
using Devlooped.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Spectre.Console;
using Spectre.Console.Json;

public static class ConsoleExtensions
{
    extension(IServiceProvider services)
    {
        public async ValueTask RenderAgentsAsync(IServiceCollection collection)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                ContractResolver = new IgnoreDelegatePropertiesResolver(),
            };

            // List configured clients
            foreach (var description in collection.AsEnumerable().Where(x => x.ServiceType == typeof(IChatClient) && x.IsKeyedService && x.ServiceKey is string))
            {
                var client = services.GetKeyedService<IChatClient>(description.ServiceKey);
                if (client is null)
                    continue;

                var metadata = client.GetService<ConfigurableChatClientMetadata>();
                var chatopt = (client as ConfigurableChatClient)?.Options;

                AnsiConsole.Write(new Panel(new JsonText(JsonConvert.SerializeObject(new { Metadata = metadata, Options = chatopt }, settings)))
                {
                    Header = new PanelHeader($"| 💬 {metadata?.Id} from {metadata?.ConfigurationSection} |"),
                });
            }

            // List configured agents
            var rendered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var descriptor in collection.AsEnumerable().Where(x => x.ServiceType == typeof(AIAgent) && x.IsKeyedService && x.ServiceKey is string))
            {
                if (rendered.Contains((string)descriptor.ServiceKey!))
                    continue;

                var agent = services.GetKeyedService<AIAgent>(descriptor.ServiceKey);
                if (agent is null)
                    continue;

                var metadata = agent.GetService<ConfigurableAIAgentMetadata>();

                AnsiConsole.Write(new Panel(new JsonText(JsonConvert.SerializeObject(new { Agent = agent, Metadata = metadata }, settings)))
                {
                    Header = new PanelHeader($"| 🤖 {agent.DisplayName} from {metadata?.ConfigurationSection} |"),
                });

                rendered.Add((string)descriptor.ServiceKey!);
            }
        }
    }

    class IgnoreDelegatePropertiesResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (property.PropertyType != null && typeof(Delegate).IsAssignableFrom(property.PropertyType))
            {
                property.ShouldSerialize = _ => false;
            }

            return property;
        }
    }
}
