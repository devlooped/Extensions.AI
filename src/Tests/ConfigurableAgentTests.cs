using Devlooped.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Devlooped.Agents.AI;

public class ConfigurableAgentTests(ITestOutputHelper output)
{
    [Fact]
    public void CanConfigureAgent()
    {
        var builder = new HostApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ai:clients:chat:modelid"] = "gpt-4.1-nano",
            ["ai:clients:chat:apikey"] = "sk-asdfasdf",
            ["ai:agents:bot:client"] = "chat",
            ["ai:agents:bot:name"] = "chat",
            ["ai:agents:bot:description"] = "Helpful chat agent",
            ["ai:agents:bot:instructions"] = "You are a helpful chat agent.",
            ["ai:agents:bot:options:temperature"] = "0.5",
        });

        builder.AddAIAgents();

        var app = builder.Build();

        var agent = app.Services.GetRequiredKeyedService<AIAgent>("chat");

        Assert.Equal("chat", agent.Name);
        Assert.Equal("chat", agent.DisplayName);
        Assert.Equal("Helpful chat agent", agent.Description);
    }

    [Fact]
    public void CanReloadConfiguration()
    {
        var builder = new HostApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ai:clients:openai:modelid"] = "gpt-4.1-nano",
            ["ai:clients:openai:apikey"] = "sk-asdfasdf",
            ["ai:clients:grok:modelid"] = "grok-4",
            ["ai:clients:grok:apikey"] = "xai-asdfasdf",
            ["ai:clients:grok:endpoint"] = "https://api.x.ai",
            ["ai:agents:bot:client"] = "openai",
            ["ai:agents:bot:description"] = "Helpful chat agent",
            ["ai:agents:bot:instructions"] = "You are a helpful agent.",
        });

        builder.AddAIAgents();

        var app = builder.Build();

        var agent = app.Services.GetRequiredKeyedService<AIAgent>("bot");

        Assert.Equal("Helpful chat agent", agent.Description);
        Assert.Equal("You are a helpful agent.", agent.GetService<ChatClientAgentOptions>()?.Instructions);
        Assert.Equal("openai", agent.GetService<AIAgentMetadata>()?.ProviderName);

        // Change the configuration to point to a different client
        var configuration = (IConfigurationRoot)app.Services.GetRequiredService<IConfiguration>();
        configuration["ai:agents:bot:client"] = "grok";
        configuration["ai:agents:bot:description"] = "Very helpful chat agent";
        configuration["ai:agents:bot:instructions"] = "You are a very helpful chat agent.";

        // NOTE: the in-memory provider does not support reload on change, so we must trigger it manually.
        configuration.Reload();

        Assert.Equal("Very helpful chat agent", agent.Description);
        Assert.Equal("You are a very helpful chat agent.", agent.GetService<ChatClientAgentOptions>()?.Instructions);
        Assert.Equal("xai", agent.GetService<AIAgentMetadata>()?.ProviderName);
    }
}

