using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Devlooped.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.OpenAI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using Spectre.Console;

var builder = WebApplication.CreateBuilder(args);

#if DEBUG
builder.Environment.EnvironmentName = Environments.Development;
// Fixes console rendering when running from Visual Studio
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    Console.InputEncoding = Console.OutputEncoding = Encoding.UTF8;
#endif

builder.AddServiceDefaults();
builder.ConfigureReload();

// 👇 showcases using dynamic AI context from configuration
builder.Services.AddKeyedSingleton("get_date", AIFunctionFactory.Create(() => DateTimeOffset.UtcNow, "get_date"));
// dummy ones for illustration
builder.Services.AddKeyedSingleton("create_order", AIFunctionFactory.Create(() => "OK", "create_order"));
builder.Services.AddKeyedSingleton("cancel_order", AIFunctionFactory.Create(() => "OK", "cancel_order"));

builder.Services.AddKeyedSingleton<AIContextProvider, NotesContextProvider>("notes");

// 👇 seamless integration of MCP tools
//builder.Services.AddMcpServer().WithTools<NotesTools>();

// 👇 implicitly calls AddChatClients
builder.AddAIAgents()
    .WithTools<NotesTools>();

var app = builder.Build();

// From ServiceDefaults.cs
app.MapDefaultEndpoints();

#if DEBUG
// 👇 render all configured agents
await app.Services.RenderAgentsAsync(builder.Services);
#endif

// Map each agent's endpoints via response API
var catalog = app.Services.GetRequiredService<AgentCatalog>();
// List configured agents
await foreach (var agent in catalog.GetAgentsAsync())
{
    if (agent.Name != null)
        app.MapOpenAIResponses(agent.Name);
}

// Map the agents HTTP endpoints
app.MapAgentDiscovery("/agents");

if (!app.Environment.IsProduction())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var baseUrl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        AnsiConsole.MarkupLine("[orange1]Registered Routes:[/]");

        var endpoints = ((IEndpointRouteBuilder)app).DataSources
                    .SelectMany(es => es.Endpoints)
                    .OfType<RouteEndpoint>()
                    .Where(e => e.RoutePattern.RawText != null)
                    .OrderBy(e => e.RoutePattern.RawText);

        foreach (var endpoint in endpoints)
        {
            var httpMethods = endpoint.Metadata
                .OfType<HttpMethodMetadata>()
                .SelectMany(m => m.HttpMethods) ?? [];

            var methods = httpMethods.Any() ? $"{string.Join(", ", httpMethods)}" : "ANY";

            AnsiConsole.MarkupLineInterpolated($"[blue][[{methods}]][/] [lime][link={baseUrl}{endpoint.RoutePattern.RawText}]{endpoint.RoutePattern.RawText}[/][/]");
        }
    });
}

app.Run();