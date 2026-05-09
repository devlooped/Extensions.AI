using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using xAI;

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
    public void CanGetClientOptions()
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

        // Untyped by name+object
        Assert.NotNull(openai.GetService<object>("options"));
        // Typed to concrete options, no need for key
        Assert.NotNull(openai.GetService<OpenAIClientOptions>());

        Assert.NotNull(grok.GetService<object>("Options"));
        Assert.NotNull(grok.GetService<GrokClientOptions>());
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

    [Fact]
    public void CanInspectOpenAIProviderOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:openai:modelid"] = "gpt-4.1.nano",
                ["ai:clients:openai:apikey"] = "sk-asdfasdf",
                ["ai:clients:openai:UserAgentApplicationId"] = "myapp/1.0",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddChatClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("openai");
        var options = Assert.IsType<OpenAIClientProvider.OpenAIProviderOptions>(
            client.GetService(typeof(object), "OpTiOnS"));

        Assert.Same(options, client.GetService(typeof(OpenAIClientProvider.OpenAIProviderOptions), "options"));
        Assert.Equal("gpt-4.1.nano", options.ModelId);
        Assert.Equal("myapp/1.0", options.UserAgentApplicationId);
    }

    [Fact]
    public void CanInspectAzureOpenAIProviderOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:chat:modelid"] = "gpt-5",
                ["ai:clients:chat:apikey"] = "asdfasdf",
                ["ai:clients:chat:endpoint"] = "https://chat.openai.azure.com/",
                ["ai:clients:chat:UserAgentApplicationId"] = "myapp/1.0",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddChatClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("chat");
        var options = Assert.IsType<AzureOpenAIClientProvider.AzureOpenAIProviderOptions>(
            client.GetService(typeof(object), "options"));

        Assert.Same(options, client.GetService(typeof(AzureOpenAIClientProvider.AzureOpenAIProviderOptions), "OPTIONS"));
        Assert.Equal(new Uri("https://chat.openai.azure.com/"), options.Endpoint);
        Assert.Equal("myapp/1.0", options.UserAgentApplicationId);
    }

    [Fact]
    public void CanInspectAzureInferenceProviderOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:chat:modelid"] = "gpt-5",
                ["ai:clients:chat:apikey"] = "asdfasdf",
                ["ai:clients:chat:endpoint"] = "https://ai.azure.com/.default",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddChatClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("chat");
        var options = Assert.IsType<AzureAIInferenceClientProvider.AzureInferenceProviderOptions>(
            client.GetService(typeof(object), "options"));

        Assert.Same(options, client.GetService(typeof(AzureAIInferenceClientProvider.AzureInferenceProviderOptions), "OPTIONS"));
        Assert.Equal(new Uri("https://ai.azure.com/.default"), options.Endpoint);
        Assert.Equal("gpt-5", options.ModelId);
    }

    [Fact]
    public void CanInspectGrokProviderOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:grok:modelid"] = "grok-4-fast",
                ["ai:clients:grok:apikey"] = "xai-asdfasdf",
                ["ai:clients:grok:endpoint"] = "https://api.x.ai",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddChatClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("grok");
        var options = Assert.IsType<GrokClientProvider.GrokProviderOptions>(
            client.GetService(typeof(object), "options"));

        Assert.Same(options, client.GetService(typeof(GrokClientProvider.GrokProviderOptions), "OPTIONS"));
        Assert.Equal("grok-4-fast", options.ModelId);
    }

    [Fact]
    public void CanInspectOpenAISpeechProviderOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:openai:modelid"] = "gpt-4o-transcribe",
                ["ai:clients:openai:apikey"] = "sk-asdfasdf",
                ["ai:clients:openai:UserAgentApplicationId"] = "myapp/1.0",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddClients(configuration)
            .BuildServiceProvider();

        var factory = services.GetRequiredKeyedService<IClientFactory>("ai:clients:openai");
        var speechToText = factory.CreateSpeechToTextClient();
        var textToSpeech = factory.CreateTextToSpeechClient();

        var speechOptions = Assert.IsType<OpenAIClientProvider.OpenAIProviderOptions>(
            speechToText.GetService(typeof(object), "options"));
        var textOptions = Assert.IsType<OpenAIClientProvider.OpenAIProviderOptions>(
            textToSpeech.GetService(typeof(object), "OPTIONS"));

        Assert.Same(speechOptions, speechToText.GetService(typeof(OpenAIClientProvider.OpenAIProviderOptions), "options"));
        Assert.Same(textOptions, textToSpeech.GetService(typeof(OpenAIClientProvider.OpenAIProviderOptions), "options"));
        Assert.Equal("gpt-4o-transcribe", speechOptions.ModelId);
        Assert.Equal("myapp/1.0", textOptions.UserAgentApplicationId);
    }

    [Fact]
    public void CanInspectAzureOpenAISpeechProviderOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:audio:modelid"] = "audio-deployment",
                ["ai:clients:audio:apikey"] = "asdfasdf",
                ["ai:clients:audio:endpoint"] = "https://chat.openai.azure.com/",
                ["ai:clients:audio:UserAgentApplicationId"] = "myapp/1.0",
            })
            .Build();

        var section = configuration.GetRequiredSection("ai:clients:audio");
        var factory = ClientFactoryResolver.CreateDefault().Resolve(section);
        var speechToText = factory.CreateSpeechToTextClient();
        var textToSpeech = factory.CreateTextToSpeechClient();

        var speechOptions = Assert.IsType<AzureOpenAIClientProvider.AzureOpenAIProviderOptions>(
            speechToText.GetService(typeof(object), "options"));
        var textOptions = Assert.IsType<AzureOpenAIClientProvider.AzureOpenAIProviderOptions>(
            textToSpeech.GetService(typeof(object), "OPTIONS"));

        Assert.Same(speechOptions, speechToText.GetService(typeof(AzureOpenAIClientProvider.AzureOpenAIProviderOptions), "options"));
        Assert.Same(textOptions, textToSpeech.GetService(typeof(AzureOpenAIClientProvider.AzureOpenAIProviderOptions), "options"));
        Assert.Equal(new Uri("https://chat.openai.azure.com/"), speechOptions.Endpoint);
        Assert.Equal("myapp/1.0", textOptions.UserAgentApplicationId);
    }

    [Fact]
    public void CanInspectXAIProviderOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:grok:modelid"] = "grok-4-fast",
                ["ai:clients:grok:apikey"] = "xai-asdfasdf",
                ["ai:clients:grok:endpoint"] = "https://api.x.ai",
            })
            .Build();

        var section = configuration.GetRequiredSection("ai:clients:grok");
        var factory = ClientFactoryResolver.CreateDefault().Resolve(section);

        var chat = factory.CreateChatClient();
        var speechToText = factory.CreateSpeechToTextClient();
        var textToSpeech = factory.CreateTextToSpeechClient();

        var speechOptions = speechToText.GetService<GrokClientOptions>();
        var textOptions = textToSpeech.GetService<GrokClientOptions>();

        Assert.Same(speechOptions, speechToText.GetService(typeof(object), "options"));
        Assert.Same(textOptions, textToSpeech.GetService(typeof(object), "options"));

        Assert.Equal("grok-4-fast", chat.GetRequiredService<GrokClientProvider.GrokProviderOptions>().ModelId);
    }

    [Fact]
    public void ThrowsForUnsupportedSpeechProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:azure:modelid"] = "gpt-4o",
                ["ai:clients:azure:apikey"] = "azure-asdfasdf",
                ["ai:clients:azure:endpoint"] = "https://ai.azure.com/deployment",
            })
            .Build();

        var section = configuration.GetRequiredSection("ai:clients:azure");
        var factory = ClientFactoryResolver.CreateDefault().Resolve(section);

        var speechToText = Assert.Throws<NotSupportedException>(() => factory.CreateSpeechToTextClient());
        var textToSpeech = Assert.Throws<NotSupportedException>(() => factory.CreateTextToSpeechClient());

        Assert.Contains(nameof(ISpeechToTextClient), speechToText.Message);
        Assert.Contains(nameof(ITextToSpeechClient), textToSpeech.Message);
    }

    [Fact]
    public void CanResolveKeyedClientFactoryBySectionPath()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:openai:modelid"] = "gpt-4.1.nano",
                ["ai:clients:openai:apikey"] = "sk-asdfasdf",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddClients(configuration)
            .BuildServiceProvider();

        var factory = services.GetRequiredKeyedService<IClientFactory>("ai:clients:openai");
        var alternative = services.GetRequiredKeyedService<IClientFactory>(new ServiceKey("AI:CLIENTS:OPENAI"));
        var client = factory.CreateChatClient();

        Assert.Same(factory, alternative);
        Assert.Equal("gpt-4.1.nano", client.GetRequiredService<ChatClientMetadata>().DefaultModelId);
    }

    [Fact]
    public void AddClientsOnlyRegistersSectionsWithDirectApiKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:grok:apikey"] = "xai-asdfasdf",
                ["ai:clients:grok:router:modelid"] = "grok-4-fast",
                ["ai:clients:grok:router:endpoint"] = "https://api.x.ai",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddClients(configuration)
            .BuildServiceProvider();

        Assert.NotNull(services.GetKeyedService<IClientFactory>("ai:clients:grok"));
        Assert.Null(services.GetKeyedService<IClientFactory>("ai:clients:grok:router"));
    }

    [Fact]
    public void BoundFactoryReflectsConfigurationChanges()
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
            .AddClients(configuration)
            .BuildServiceProvider();

        var factory = services.GetRequiredKeyedService<IClientFactory>("ai:clients:openai");
        var original = factory.CreateChatClient();

        configuration["ai:clients:openai:modelid"] = "gpt-5";

        var updated = factory.CreateChatClient();

        Assert.Equal("gpt-4.1", original.GetRequiredService<ChatClientMetadata>().DefaultModelId);
        Assert.Equal("gpt-5", updated.GetRequiredService<ChatClientMetadata>().DefaultModelId);
    }
}
