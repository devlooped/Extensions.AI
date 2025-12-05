using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Devlooped.Extensions.AI;

public class ConfigurableClientTests(ITestOutputHelper output)
{
    [Fact]
    public void CanConfigureClients()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:openai:modelid"] = "gpt-4.1.nano",
                ["ai:clients:openai:ApiKey"] = "sk-asdfasdf",
                ["ai:clients:grok:modelid"] = "grok-4-fast",
                ["ai:clients:grok:ApiKey"] = "xai-asdfasdf",
                ["ai:clients:grok:endpoint"] = "https://api.x.ai",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddChatClients(configuration)
            .BuildServiceProvider();

        var openai = services.GetRequiredKeyedService<IChatClient>("openai");
        var grok = services.GetRequiredKeyedService<IChatClient>("grok");

        Assert.Equal("openai", openai.GetRequiredService<ChatClientMetadata>().ProviderName);
        Assert.Equal("xai", grok.GetRequiredService<ChatClientMetadata>().ProviderName);
    }

    [Fact]
    public void CanGetFromAlternativeKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:Grok:modelid"] = "grok-4-fast",
                ["ai:clients:Grok:ApiKey"] = "xai-asdfasdf",
                ["ai:clients:Grok:endpoint"] = "https://api.x.ai",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddChatClients(configuration)
            .BuildServiceProvider();

        var grok = services.GetRequiredKeyedService<IChatClient>(new ServiceKey("grok"));

        Assert.Equal("xai", grok.GetRequiredService<ChatClientMetadata>().ProviderName);
        Assert.Same(grok, services.GetChatClient("grok"));
    }

    [Fact]
    public void CanGetSectionAndIdFromMetadata()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:Grok:id"] = "groked",
                ["ai:clients:Grok:modelid"] = "grok-4-fast",
                ["ai:clients:Grok:ApiKey"] = "xai-asdfasdf",
                ["ai:clients:Grok:endpoint"] = "https://api.x.ai",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddChatClients(configuration)
            .BuildServiceProvider();

        var grok = services.GetRequiredKeyedService<IChatClient>("groked");
        var metadata = grok.GetRequiredService<ConfigurableChatClientMetadata>();

        Assert.Equal("groked", metadata.Id);
        Assert.Equal("ai:clients:Grok", metadata.ConfigurationSection);
    }

    [Fact]
    public void CanOverrideClientId()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:grok:id"] = "xai",
                ["ai:clients:grok:modelid"] = "grok-4-fast",
                ["ai:clients:grok:apikey"] = "xai-asdfasdf",
                ["ai:clients:grok:endpoint"] = "https://api.x.ai",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddChatClients(configuration)
            .BuildServiceProvider();

        var grok = services.GetRequiredKeyedService<IChatClient>("xai");

        Assert.Equal("xai", grok.GetRequiredService<ChatClientMetadata>().ProviderName);
    }

    [Fact]
    public void CanSetApiKeyToConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["keys:grok"] = "xai-asdfasdf",
                ["ai:clients:grok:modelid"] = "grok-4-fast",
                ["ai:clients:grok:apikey"] = "keys:grok",
                ["ai:clients:grok:endpoint"] = "https://api.x.ai",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddChatClients(configuration)
            .BuildServiceProvider();

        var grok = services.GetRequiredKeyedService<IChatClient>("grok");

        Assert.Equal("xai", grok.GetRequiredService<ChatClientMetadata>().ProviderName);
    }

    [Fact]
    public void CanSetApiKeyToSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["keys:grok:apikey"] = "xai-asdfasdf",
                ["ai:clients:grok:modelid"] = "grok-4-fast",
                ["ai:clients:grok:apikey"] = "keys:grok",
                ["ai:clients:grok:endpoint"] = "https://api.x.ai",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddChatClients(configuration)
            .BuildServiceProvider();

        var grok = services.GetRequiredKeyedService<IChatClient>("grok");

        Assert.Equal("xai", grok.GetRequiredService<ChatClientMetadata>().ProviderName);
    }

    [Fact]
    public void CanChangeAndReloadModelId()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:openai:modelid"] = "gpt-4.1",
                ["ai:clients:openai:apikey"] = "sk-asdfasdf",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging(builder => builder.AddTestOutput(output))
            .AddChatClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("openai");

        Assert.Equal("openai", client.GetRequiredService<ChatClientMetadata>().ProviderName);
        Assert.Equal("gpt-4.1", client.GetRequiredService<ChatClientMetadata>().DefaultModelId);

        configuration["ai:clients:openai:modelid"] = "gpt-5";
        // NOTE: the in-memory provider does not support reload on change, so we must trigger it manually.
        configuration.Reload();

        Assert.Equal("gpt-5", client.GetRequiredService<ChatClientMetadata>().DefaultModelId);
    }

    [Fact]
    public void CanChangeAndSwapProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:chat:modelid"] = "gpt-4.1",
                ["ai:clients:chat:apikey"] = "sk-asdfasdf",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging(builder => builder.AddTestOutput(output))
            .AddChatClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("chat");

        Assert.Equal("openai", client.GetRequiredService<ChatClientMetadata>().ProviderName);
        Assert.Equal("gpt-4.1", client.GetRequiredService<ChatClientMetadata>().DefaultModelId);

        configuration["ai:clients:chat:modelid"] = "grok-4";
        configuration["ai:clients:chat:apikey"] = "xai-asdfasdf";
        configuration["ai:clients:chat:endpoint"] = "https://api.x.ai";

        // NOTE: the in-memory provider does not support reload on change, so we must trigger it manually.
        configuration.Reload();

        Assert.Equal("xai", client.GetRequiredService<ChatClientMetadata>().ProviderName);
        Assert.Equal("grok-4", client.GetRequiredService<ChatClientMetadata>().DefaultModelId);
    }

    [Fact]
    public void CanConfigureAzureInference()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:chat:modelid"] = "gpt-5",
                ["ai:clients:chat:apikey"] = "asdfasdf",
                ["ai:clients:chat:endpoint"] = "https://ai.azure.com/.default"
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging(builder => builder.AddTestOutput(output))
            .AddChatClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("chat");

        Assert.Equal("azure.ai.inference", client.GetRequiredService<ChatClientMetadata>().ProviderName);
        Assert.Equal("gpt-5", client.GetRequiredService<ChatClientMetadata>().DefaultModelId);
    }

    [Fact]
    public void CanConfigureAzureOpenAI()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:chat:modelid"] = "gpt-5",
                ["ai:clients:chat:apikey"] = "asdfasdf",
                ["ai:clients:chat:endpoint"] = "https://chat.openai.azure.com/",
                ["ai:clients:chat:UserAgentApplicationId"] = "myapp/1.0"
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging(builder => builder.AddTestOutput(output))
            .AddChatClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("chat");

        Assert.Equal("azure.ai.openai", client.GetRequiredService<ChatClientMetadata>().ProviderName);
        Assert.Equal("gpt-5", client.GetRequiredService<ChatClientMetadata>().DefaultModelId);
    }
}
