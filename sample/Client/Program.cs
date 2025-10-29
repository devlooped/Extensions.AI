using System.Net.Http.Json;
using Devlooped.Extensions.AI.OpenAI;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = App.CreateBuilder(args);
#if DEBUG
builder.Environment.EnvironmentName = Environments.Development;
#endif

builder.AddServiceDefaults();
builder.Services.AddHttpClient()
    .ConfigureHttpClientDefaults(b => b.AddStandardResilienceHandler());

var app = builder.Build(async (IServiceProvider services, CancellationToken cancellation) =>
{
    var baseUrl = Environment.GetEnvironmentVariable("applicationUrl") ?? "http://localhost:5117";
    var http = services.GetRequiredService<IHttpClientFactory>().CreateClient();
    var agents = await http.GetFromJsonAsync<AgentCard[]>($"{baseUrl}/agents", cancellation) ?? [];

    if (agents.Length == 0)
    {
        AnsiConsole.MarkupLine(":warning: No agents available");
        return;
    }

    var selectedAgent = AnsiConsole.Prompt(new SelectionPrompt<AgentCard>()
        .Title("Select agent:")
        .UseConverter(a => $"{a.Name}: {a.Description ?? ""}")
        .AddChoices(agents));

    var chat = new OpenAIChatClient("none", "default", new OpenAI.OpenAIClientOptions
    {
        Endpoint = new Uri($"{baseUrl}/{selectedAgent.Name}/v1")
    }).AsBuilder().UseOpenTelemetry().UseJsonConsoleLogging().Build(services);

    var history = new List<ChatMessage>();

    AnsiConsole.MarkupLine($":robot: Ready");
    AnsiConsole.Markup($":person_beard: ");
    while (!cancellation.IsCancellationRequested)
    {
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input))
            continue;

        history.Add(new ChatMessage(ChatRole.User, input));
        try
        {
            var response = await AnsiConsole.Status().StartAsync(":robot: Thinking...", ctx => chat.GetResponseAsync(input));
            history.AddRange(response.Messages);
            try
            {
                // Try rendering as formatted markup
                if (response.Text is { Length: > 0 })
                    AnsiConsole.MarkupLine($":robot: {response.Text}");
            }
            catch (Exception)
            {
                // Fallback to escaped markup text if rendering fails
                AnsiConsole.MarkupLineInterpolated($":robot: {response.Text}");
            }
            AnsiConsole.Markup($":person_beard: ");
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
        }
    }

    AnsiConsole.MarkupLine($":robot: Shutting down...");
});

Console.WriteLine("Powered by Smith");

await app.RunAsync();

record AgentCard(string Name, string? Description);