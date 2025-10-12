using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Devlooped.Extensions.AI;

public class ConfigurableTests
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

        var calls = new List<string>();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .UseChatClients(configuration)
            .BuildServiceProvider();

        var openai = services.GetRequiredKeyedService<IChatClient>("openai");
        var grok = services.GetRequiredKeyedService<IChatClient>("grok");

        Assert.Equal("openai", openai.GetRequiredService<ChatClientMetadata>().ProviderName);
        Assert.Equal("xai", grok.GetRequiredService<ChatClientMetadata>().ProviderName);
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

        var calls = new List<string>();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .UseChatClients(configuration)
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

        var calls = new List<string>();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .UseChatClients(configuration)
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

        var calls = new List<string>();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .UseChatClients(configuration)
            .BuildServiceProvider();

        var grok = services.GetRequiredKeyedService<IChatClient>("grok");

        Assert.Equal("xai", grok.GetRequiredService<ChatClientMetadata>().ProviderName);
    }
}
