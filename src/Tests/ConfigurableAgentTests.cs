using Devlooped.Extensions.AI;
using Devlooped.Extensions.AI.Grok;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using OpenAI.Assistants;
using Tomlyn.Extensions.Configuration;

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
    public void CanGetFromAlternativeKey()
    {
        var builder = new HostApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ai:clients:Chat:modelid"] = "gpt-4.1-nano",
            ["ai:clients:Chat:apikey"] = "sk-asdfasdf",
            // NOTE: mismatched case in client id
            ["ai:agents:bot:client"] = "chat",
        });

        builder.AddAIAgents();

        var app = builder.Build();

        var agent = app.Services.GetRequiredKeyedService<AIAgent>(new ServiceKey("Bot"));

        Assert.Equal("bot", agent.Name);
        Assert.Same(agent, app.Services.GetIAAgent("Bot"));
    }

    [Fact]
    public void CanGetSectionAndIdFromMetadata()
    {
        var builder = new HostApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ai:clients:chat:modelid"] = "gpt-4.1-nano",
            ["ai:clients:chat:apikey"] = "sk-asdfasdf",
            ["ai:agents:bot:client"] = "chat",
        });

        builder.AddAIAgents();

        var app = builder.Build();

        var agent = app.Services.GetRequiredKeyedService<AIAgent>("bot");
        var metadata = agent.GetService<ConfigurableAIAgentMetadata>();

        Assert.NotNull(metadata);
        Assert.Equal("bot", metadata.Name);
        Assert.Equal("ai:agents:bot", metadata.ConfigurationSection);
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
    public void UseContextProviderFactoryFromKeyedService()
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
    public async Task UseContextProviderFromKeyedServiceAsync()
    {
        var builder = new HostApplicationBuilder();
        var context = new AIContext();

        var provider = new Mock<AIContextProvider>();
        provider
            .Setup(x => x.InvokingAsync(It.IsAny<AIContextProvider.InvokingContext>(), default(CancellationToken)))
            .ReturnsAsync(context);

        builder.Services.AddKeyedSingleton("chat", provider.Object);

        builder.Configuration.AddToml(
            """"
            [ai.clients.openai]
            modelid = "gpt-4.1"
            apikey = "sk-asdf"
            
            [ai.agents.chat]
            description = "Chat agent."
            client = "openai"
            """");

        builder.AddAIAgents();

        var app = builder.Build();
        var agent = app.Services.GetRequiredKeyedService<AIAgent>("chat");
        var options = agent.GetService<ChatClientAgentOptions>();

        Assert.NotNull(options?.AIContextProviderFactory);

        var actualProvider = options?.AIContextProviderFactory?.Invoke(new());

        Assert.NotNull(actualProvider);

        Assert.Same(context, await actualProvider.InvokingAsync(new([]), default));
    }

    [Fact]
    public void UseAndContextProviderFactoryIncompatible()
    {
        var builder = new HostApplicationBuilder();

        builder.Configuration.AddToml(
            """"
            [ai.clients.openai]
            modelid = "gpt-4.1"
            apikey = "sk-asdf"

            [ai.agents.chat]
            description = "Chat agent."
            client = "openai"
            use = ["voseo"]

            [ai.context.voseo]
            instructions = 'Default to using spanish language, using argentinean "voseo" in your responses'
            """");

        builder.AddAIAgents(configureOptions: (name, options)
            => options.AIContextProviderFactory = context => Mock.Of<AIContextProvider>());

        var app = builder.Build();

        var exception = Assert.ThrowsAny<Exception>(() => app.Services.GetRequiredKeyedService<AIAgent>("chat"));

        Assert.Contains("ai:agents:chat:use", exception.Message);
    }

    [Fact]
    public void UseAndContextProviderIncompatible()
    {
        var builder = new HostApplicationBuilder();

        builder.Configuration.AddToml(
            """"
            [ai.clients.openai]
            modelid = "gpt-4.1"
            apikey = "sk-asdf"

            [ai.agents.chat]
            description = "Chat agent."
            client = "openai"
            use = ["voseo"]

            [ai.context.voseo]
            instructions = """\
                Default to using spanish language, using argentinean "voseo" in your responses \
                (unless the user explicitly talks in a different language). \
                This means using "vos" instead of "tú" and conjugating verbs accordingly. \
                Don't use the expression "pa'" instead of "para". Don't mention the word "voseo".
                """            
            """");

        builder.Services.AddKeyedSingleton("chat", Mock.Of<AIContextProvider>());
        builder.AddAIAgents();

        var app = builder.Build();
        var exception = Assert.ThrowsAny<Exception>(() => app.Services.GetRequiredKeyedService<AIAgent>("chat"));

        Assert.Contains("ai:agents:chat:use", exception.Message);
    }

    [Fact]
    public async Task UseAIContextFromKeyedServiceAsync()
    {
        var builder = new HostApplicationBuilder();
        var voseo = new AIContext { Instructions = "voseo" };

        builder.Configuration.AddToml(
            """"
            [ai.clients.openai]
            modelid = "gpt-4.1"
            apikey = "sk-asdf"
            
            [ai.agents.chat]
            description = "Chat agent."
            client = "openai"
            use = ["voseo"]
            """");

        builder.Services.AddKeyedSingleton("voseo", voseo);

        builder.AddAIAgents();

        var app = builder.Build();
        var agent = app.Services.GetRequiredKeyedService<AIAgent>("chat");
        var options = agent.GetService<ChatClientAgentOptions>();

        Assert.NotNull(options?.AIContextProviderFactory);
        var actualProvider = options?.AIContextProviderFactory?.Invoke(new());
        Assert.NotNull(actualProvider);

        var actualContext = await actualProvider.InvokingAsync(new([]), default);

        Assert.Same(voseo, await actualProvider.InvokingAsync(new([]), default));
    }

    [Fact]
    public async Task UseAggregatedAIContextsFromKeyedServiceAsync()
    {
        var builder = new HostApplicationBuilder();
        var voseo = new AIContext { Instructions = "voseo" };
        var formatting = new AIContext { Instructions = "formatting" };

        builder.Configuration.AddToml(
            """"
            [ai.clients.openai]
            modelid = "gpt-4.1"
            apikey = "sk-asdf"
            
            [ai.agents.chat]
            description = "Chat agent."
            client = "openai"
            use = ["voseo", "formatting"]
            """");

        builder.Services.AddKeyedSingleton("voseo", voseo);
        builder.Services.AddKeyedSingleton("formatting", formatting);

        builder.AddAIAgents();

        var app = builder.Build();
        var agent = app.Services.GetRequiredKeyedService<AIAgent>("chat");
        var options = agent.GetService<ChatClientAgentOptions>();

        Assert.NotNull(options?.AIContextProviderFactory);
        var actualProvider = options?.AIContextProviderFactory?.Invoke(new());
        Assert.NotNull(actualProvider);

        var actualContext = await actualProvider.InvokingAsync(new([]), default);

        Assert.StartsWith(voseo.Instructions, actualContext.Instructions);
        Assert.EndsWith(formatting.Instructions, actualContext.Instructions);
    }

    [Fact]
    public async Task UseAIToolFromKeyedServiceAsync()
    {
        var builder = new HostApplicationBuilder();

        builder.Configuration.AddToml(
            """"
            [ai.clients.openai]
            modelid = "gpt-4.1"
            apikey = "sk-asdf"
            
            [ai.agents.chat]
            description = "Chat agent."
            client = "openai"
            use = ["get_date"]
            """");

        AITool tool = AIFunctionFactory.Create(() => DateTimeOffset.Now, "get_date");
        builder.Services.AddKeyedSingleton("get_date", tool);
        builder.AddAIAgents();

        var app = builder.Build();
        var agent = app.Services.GetRequiredKeyedService<AIAgent>("chat");
        var options = agent.GetService<ChatClientAgentOptions>();

        Assert.NotNull(options?.AIContextProviderFactory);
        var provider = options?.AIContextProviderFactory?.Invoke(new());
        Assert.NotNull(provider);

        var context = await provider.InvokingAsync(new([]), default);

        Assert.NotNull(context.Tools);
        Assert.Single(context.Tools);
        Assert.Same(tool, context.Tools[0]);
    }

    [Fact]
    public async Task UseAIContextFromSection()
    {
        var builder = new HostApplicationBuilder();
        var voseo =
            """
            Default to using spanish language, using argentinean "voseo" in your responses.
            """;

        builder.Configuration.AddToml(
            $$"""
            [ai.clients.openai]
            modelid = "gpt-4.1"
            apikey = "sk-asdf"

            [ai.agents.chat]
            description = "Chat agent."
            client = "openai"
            use = ["default"]

            [ai.context.default]
            instructions = '{{voseo}}'
            messages = [
                { system = "You are strictly professional." },
                { user = "Hey you!"},
                { assistant = "Hello there. How can I assist you today?" }
            ]
            tools = ["get_date"]
            """);

        var tool = AIFunctionFactory.Create(() => DateTimeOffset.Now, "get_date");
        builder.Services.AddKeyedSingleton("get_date", tool);
        builder.AddAIAgents();
        var app = builder.Build();

        var agent = app.Services.GetRequiredKeyedService<AIAgent>("chat");
        var options = agent.GetService<ChatClientAgentOptions>();

        Assert.NotNull(options?.AIContextProviderFactory);
        var provider = options?.AIContextProviderFactory?.Invoke(new());
        Assert.NotNull(provider);

        var context = await provider.InvokingAsync(new([]), default);

        Assert.NotNull(context.Instructions);
        Assert.Equal(voseo, context.Instructions);
        Assert.Equal(3, context.Messages?.Count);
        Assert.Single(context.Messages!, x => x.Role == ChatRole.System && x.Text == "You are strictly professional.");
        Assert.Single(context.Messages!, x => x.Role == ChatRole.User && x.Text == "Hey you!");
        Assert.Single(context.Messages!, x => x.Role == ChatRole.Assistant && x.Text == "Hello there. How can I assist you today?");
        Assert.Same(tool, context.Tools?.First());
    }

    [Fact]
    public async Task MissingToolAIContextFromSectionThrows()
    {
        var builder = new HostApplicationBuilder();

        builder.Configuration.AddToml(
            $$"""
            [ai.clients.openai]
            modelid = "gpt-4.1"
            apikey = "sk-asdf"

            [ai.agents.chat]
            description = "Chat agent."
            client = "openai"
            use = ["default"]

            [ai.context.default]
            tools = ["get_date"]
            """);

        builder.AddAIAgents();
        var app = builder.Build();

        var exception = Assert.ThrowsAny<Exception>(() => app.Services.GetRequiredKeyedService<AIAgent>("chat"));

        Assert.Contains("get_date", exception.Message);
        Assert.Contains("ai:context:default:tools", exception.Message);
        Assert.Contains("ai:agents:chat", exception.Message);
    }

    [Fact]
    public async Task UnknownUseThrows()
    {
        var builder = new HostApplicationBuilder();

        builder.Configuration.AddToml(
            $$"""
            [ai.clients.openai]
            modelid = "gpt-4.1"
            apikey = "sk-asdf"

            [ai.agents.chat]
            description = "Chat agent."
            client = "openai"
            use = ["foo"]
            """);

        builder.AddAIAgents();
        var app = builder.Build();

        var exception = Assert.ThrowsAny<Exception>(() => app.Services.GetRequiredKeyedService<AIAgent>("chat"));

        Assert.Contains("foo", exception.Message);
    }
}

