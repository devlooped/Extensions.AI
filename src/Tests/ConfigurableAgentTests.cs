using Devlooped.Extensions.AI;
using Devlooped.Extensions.AI.Grok;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

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
    public void DedentsDescriptionAndInstructions()
    {
        var builder = new HostApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ai:clients:chat:modelid"] = "gpt-4.1-nano",
            ["ai:clients:chat:apikey"] = "sk-asdfasdf",
            ["ai:agents:bot:client"] = "chat",
            ["ai:agents:bot:name"] = "chat",
            ["ai:agents:bot:description"] =
                """


                    Line 1
                    Line 2
                    Line 3

                """,
            ["ai:agents:bot:instructions"] =
                """
                        Agent Instructions:
                            - Step 1
                            - Step 2
                            - Step 3
                """,
            ["ai:agents:bot:options:temperature"] = "0.5",
        });

        builder.AddAIAgents();

        var app = builder.Build();

        var agent = app.Services.GetRequiredKeyedService<AIAgent>("chat");

        Assert.Equal(
            """
            Line 1
            Line 2
            Line 3
            """, agent.Description);

        Assert.Equal(
            """
            Agent Instructions:
                - Step 1
                - Step 2
                - Step 3
            """, agent.GetService<ChatClientAgentOptions>()?.Instructions);
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

    [Fact]
    public void AssignsContextProviderFromKeyedService()
    {
        var builder = new HostApplicationBuilder();
        var context = Mock.Of<AIContextProvider>();

        builder.Services.AddKeyedSingleton<AIContextProviderFactory>("bot",
            Mock.Of<AIContextProviderFactory>(x
                => x.CreateProvider(It.IsAny<ChatClientAgentOptions.AIContextProviderFactoryContext>()) == context));

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ai:clients:chat:modelid"] = "gpt-4.1-nano",
            ["ai:clients:chat:apikey"] = "sk-asdfasdf",
            ["ai:agents:bot:client"] = "chat",
            ["ai:agents:bot:options:temperature"] = "0.5",
        });

        builder.AddAIAgents();

        var app = builder.Build();
        var agent = app.Services.GetRequiredKeyedService<AIAgent>("bot");
        var options = agent.GetService<ChatClientAgentOptions>();

        Assert.NotNull(options?.AIContextProviderFactory);
        Assert.Same(context, options?.AIContextProviderFactory?.Invoke(new ChatClientAgentOptions.AIContextProviderFactoryContext()));
    }

    [Fact]
    public void AssignsContextProviderFromService()
    {
        var builder = new HostApplicationBuilder();
        var context = Mock.Of<AIContextProvider>();

        builder.Services.AddSingleton<AIContextProviderFactory>(
            Mock.Of<AIContextProviderFactory>(x
                => x.CreateProvider(It.IsAny<ChatClientAgentOptions.AIContextProviderFactoryContext>()) == context));

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ai:clients:chat:modelid"] = "gpt-4.1-nano",
            ["ai:clients:chat:apikey"] = "sk-asdfasdf",
            ["ai:agents:bot:client"] = "chat",
            ["ai:agents:bot:options:temperature"] = "0.5",
        });

        builder.AddAIAgents();

        var app = builder.Build();
        var agent = app.Services.GetRequiredKeyedService<AIAgent>("bot");
        var options = agent.GetService<ChatClientAgentOptions>();

        Assert.NotNull(options?.AIContextProviderFactory);
        Assert.Same(context, options?.AIContextProviderFactory?.Invoke(new()));
    }

    [Fact]
    public void AssignsMessageStoreFactoryFromKeyedService()
    {
        var builder = new HostApplicationBuilder();
        var context = Mock.Of<ChatMessageStore>();

        builder.Services.AddKeyedSingleton<ChatMessageStoreFactory>("bot",
            Mock.Of<ChatMessageStoreFactory>(x
                => x.CreateStore(It.IsAny<ChatClientAgentOptions.ChatMessageStoreFactoryContext>()) == context));

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ai:clients:chat:modelid"] = "gpt-4.1-nano",
            ["ai:clients:chat:apikey"] = "sk-asdfasdf",
            ["ai:agents:bot:client"] = "chat",
            ["ai:agents:bot:options:temperature"] = "0.5",
        });

        builder.AddAIAgents();

        var app = builder.Build();
        var agent = app.Services.GetRequiredKeyedService<AIAgent>("bot");
        var options = agent.GetService<ChatClientAgentOptions>();

        Assert.NotNull(options?.ChatMessageStoreFactory);
        Assert.Same(context, options?.ChatMessageStoreFactory?.Invoke(new()));
    }

    [Fact]
    public void AssignsMessageStoreFactoryFromService()
    {
        var builder = new HostApplicationBuilder();
        var context = Mock.Of<ChatMessageStore>();

        builder.Services.AddSingleton<ChatMessageStoreFactory>(
            Mock.Of<ChatMessageStoreFactory>(x
                => x.CreateStore(It.IsAny<ChatClientAgentOptions.ChatMessageStoreFactoryContext>()) == context));

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ai:clients:chat:modelid"] = "gpt-4.1-nano",
            ["ai:clients:chat:apikey"] = "sk-asdfasdf",
            ["ai:agents:bot:client"] = "chat",
            ["ai:agents:bot:options:temperature"] = "0.5",
        });

        builder.AddAIAgents();

        var app = builder.Build();
        var agent = app.Services.GetRequiredKeyedService<AIAgent>("bot");
        var options = agent.GetService<ChatClientAgentOptions>();

        Assert.NotNull(options?.ChatMessageStoreFactory);
        Assert.Same(context, options?.ChatMessageStoreFactory?.Invoke(new()));
    }

    [Fact]
    public void CanSetOpenAIReasoningAndVerbosity()
    {
        var builder = new HostApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ai:clients:openai:modelid"] = "gpt-4.1",
            ["ai:clients:openai:apikey"] = "sk-asdfasdf",
            ["ai:agents:bot:client"] = "openai",
            ["ai:agents:bot:options:reasoningeffort"] = "minimal",
            ["ai:agents:bot:options:verbosity"] = "low",
        });

        var app = builder.AddAIAgents().Build();
        var agent = app.Services.GetRequiredKeyedService<AIAgent>("bot");
        var options = agent.GetService<ChatClientAgentOptions>();

        Assert.Equal(Verbosity.Low, options?.ChatOptions?.Verbosity);
        Assert.Equal(ReasoningEffort.Minimal, options?.ChatOptions?.ReasoningEffort);
    }

    [Fact]
    public void CanSetGrokOptions()
    {
        var builder = new HostApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ai:clients:grok:modelid"] = "grok-4",
            ["ai:clients:grok:apikey"] = "xai-asdfasdf",
            ["ai:clients:grok:endpoint"] = "https://api.x.ai",
            ["ai:agents:bot:client"] = "grok",
            ["ai:agents:bot:options:reasoningeffort"] = "low",
            ["ai:agents:bot:options:search"] = "auto",
        });

        var app = builder.AddAIAgents().Build();
        var agent = app.Services.GetRequiredKeyedService<AIAgent>("bot");
        var options = agent.GetService<ChatClientAgentOptions>();

        var grok = Assert.IsType<GrokChatOptions>(options?.ChatOptions);

        Assert.Equal(ReasoningEffort.Low, grok.ReasoningEffort);
        Assert.Equal(GrokSearch.Auto, grok.Search);

        var configuration = (IConfigurationRoot)app.Services.GetRequiredService<IConfiguration>();
        configuration["ai:agents:bot:options:reasoningeffort"] = "high";
        configuration["ai:agents:bot:options:search"] = "off";
        // NOTE: the in-memory provider does not support reload on change, so we must trigger it manually.
        configuration.Reload();

        options = agent.GetService<ChatClientAgentOptions>();
        grok = Assert.IsType<GrokChatOptions>(options?.ChatOptions);

        Assert.Equal(ReasoningEffort.High, grok.ReasoningEffort);
        Assert.Equal(GrokSearch.Off, grok.Search);
    }

    [Fact]
    public void Task()
    {
        var agent = new AgentRunResponse() { AgentId = "agent-123" };
        var chat = agent.AsChatResponse();

        Assert.Equal("agent-123", chat.AdditionalProperties!["AgentId"]);
    }
}

