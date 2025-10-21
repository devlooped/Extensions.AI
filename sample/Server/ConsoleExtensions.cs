using Devlooped.Agents.AI;
using Devlooped.Extensions.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;

public static class ConsoleExtensions
{
    public static async ValueTask RenderAgentsAsync(this IServiceProvider services, IServiceCollection collection)
    {
        var catalog = services.GetRequiredService<AgentCatalog>();
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include,
            DefaultValueHandling = DefaultValueHandling.Ignore
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
        await foreach (var agent in catalog.GetAgentsAsync())
        {
            var metadata = agent.GetService<ConfigurableAIAgentMetadata>();

            AnsiConsole.Write(new Panel(new JsonText(JsonConvert.SerializeObject(new { Agent = agent, Metadata = metadata }, settings)))
            {
                Header = new PanelHeader($"| 🤖 {agent.DisplayName} from {metadata?.ConfigurationSection} |"),
            });
        }
    }
}
