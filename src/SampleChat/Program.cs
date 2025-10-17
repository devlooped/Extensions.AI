using Devlooped.Agents.AI;
using Devlooped.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;
using Tomlyn.Extensions.Configuration;

var host = new HostApplicationBuilder(args);

#if DEBUG
host.Environment.EnvironmentName = Environments.Development;
#endif

// Setup config files from output directory vs project directory
// depending on environment, so we can reload by editing the source
if (host.Environment.IsProduction())
{
    foreach (var json in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.json", SearchOption.AllDirectories))
        host.Configuration.AddJsonFile(json, optional: false, reloadOnChange: true);

    foreach (var toml in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.toml", SearchOption.AllDirectories))
        host.Configuration.AddTomlFile(toml, optional: false, reloadOnChange: true);
}
else
{
    var baseDir = ThisAssembly.Project.MSBuildProjectDirectory;
    var outDir = Path.Combine(baseDir, ThisAssembly.Project.BaseOutputPath);
    var objDir = Path.Combine(baseDir, ThisAssembly.Project.BaseIntermediateOutputPath);

    bool IsSource(string path) => !path.StartsWith(outDir) && !path.StartsWith(objDir);

    foreach (var json in Directory.EnumerateFiles(baseDir, "*.json", SearchOption.AllDirectories).Where(IsSource))
        host.Configuration.AddJsonFile(json, optional: false, reloadOnChange: true);

    foreach (var toml in Directory.EnumerateFiles(baseDir, "*.toml", SearchOption.AllDirectories).Where(IsSource))
        host.Configuration.AddTomlFile(toml, optional: false, reloadOnChange: true);
}

// .env/secrets override other config, which may contain dummy API keys, for example
host.Configuration
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>();

// 👇 implicitly calls AddChatClients
host.AddAIAgents();

var app = host.Build();
var catalog = app.Services.GetRequiredService<AgentCatalog>();
var settings = new JsonSerializerSettings
{
    NullValueHandling = NullValueHandling.Include,
    DefaultValueHandling = DefaultValueHandling.Ignore
};

// List configured clients
foreach (var description in host.Services.AsEnumerable().Where(x => x.ServiceType == typeof(IChatClient) && x.IsKeyedService))
{
    var client = app.Services.GetKeyedService<IChatClient>(description.ServiceKey);
    if (client is null)
        continue;

    var metadata = client.GetService<ChatClientMetadata>();
    var chatopt = (client as ConfigurableChatClient)?.Options;

    AnsiConsole.Write(new Panel(new JsonText(JsonConvert.SerializeObject(new { Metadata = metadata, Options = chatopt }, settings)))
    {
        Header = new PanelHeader($"| 💬 {description.ServiceKey} |"),
    });
}

// List configured agents
await foreach (var agent in catalog.GetAgentsAsync())
{
    var metadata = agent.GetService<AIAgentMetadata>();

    AnsiConsole.Write(new Panel(new JsonText(JsonConvert.SerializeObject(new { Agent = agent, Metadata = metadata }, settings)))
    {
        Header = new PanelHeader($"| 🤖 {agent.DisplayName} |"),
    });
}

Console.ReadLine();
