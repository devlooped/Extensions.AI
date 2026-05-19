using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using xAI;

namespace Devlooped.Extensions.AI;

public class ConfigurableClientTests(ITestOutputHelper output)
{
    [Fact]
    public void Migrate()
    {
        var configuration = new ConfigurationBuilder().Build();
        var collection = new ServiceCollection();

        collection.ConfigureChatClientDefaults(b => b.UseLogging())
            .AddAIClients(configuration, useDefaultProviders: false);

        var services = collection.BuildServiceProvider();
    }


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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var openai = services.GetRequiredKeyedService<IChatClient>("ai:clients:openai");
        var grok = services.GetRequiredKeyedService<IChatClient>("ai:clients:grok");

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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var openai = services.GetRequiredKeyedService<IChatClient>("ai:clients:openai");
        var grok = services.GetRequiredKeyedService<IChatClient>("ai:clients:grok");

        // Untyped by name+object
        Assert.NotNull(openai.GetService<object>("options"));
        // Typed to concrete options, no need for key
        Assert.NotNull(openai.GetService<OpenAIClientOptions>());

        Assert.NotNull(grok.GetService<object>("Options"));
        Assert.NotNull(grok.GetService<GrokClientOptions>());
    }

    [Fact]
    public void CanResolveClientByImplicitSectionId()
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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var grok = services.GetRequiredKeyedService<IChatClient>("ai:clients:Grok");
        var alias = services.GetRequiredKeyedService<IChatClient>("Grok");

        Assert.Same(grok, alias);
        Assert.Same(grok, services.GetRequiredKeyedService<IChatClient>("grok"));
        Assert.Equal("xai", grok.GetRequiredService<ChatClientMetadata>().ProviderName);
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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var grok = services.GetRequiredKeyedService<IChatClient>("ai:clients:Grok");
        var alias = services.GetRequiredKeyedService<IChatClient>("groked");
        var defaultAlias = services.GetRequiredKeyedService<IChatClient>("Grok");
        var metadata = grok.GetRequiredService<ConfigurableChatClientMetadata>();

        Assert.Same(grok, alias);
        Assert.Same(grok, defaultAlias);
        Assert.Equal("groked", metadata.Id);
        Assert.Equal("ai:clients:Grok", metadata.ConfigurationSection);
    }

    [Fact]
    public void CanResolveClientByConfiguredId()
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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var grok = services.GetRequiredKeyedService<IChatClient>("ai:clients:grok");
        var alias = services.GetRequiredKeyedService<IChatClient>("xai");
        var defaultAlias = services.GetRequiredKeyedService<IChatClient>("grok");

        Assert.Same(grok, alias);
        Assert.Same(grok, defaultAlias);
        Assert.Equal("xai", grok.GetRequiredService<ChatClientMetadata>().ProviderName);
    }

    [Fact]
    public void CanResolveClientByLowercasePath()
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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var grok = services.GetRequiredKeyedService<IChatClient>("ai:clients:Grok");

        // lowercase path
        Assert.Same(grok, services.GetRequiredKeyedService<IChatClient>("ai:clients:grok"));
        // dotted path (original casing)
        Assert.Same(grok, services.GetRequiredKeyedService<IChatClient>("ai.clients.Grok"));
        // dotted path (lowercase)
        Assert.Same(grok, services.GetRequiredKeyedService<IChatClient>("ai.clients.grok"));
        // last segment (already tested elsewhere, but confirms it too)
        Assert.Same(grok, services.GetRequiredKeyedService<IChatClient>("Grok"));
        // last segment lowercase
        Assert.Same(grok, services.GetRequiredKeyedService<IChatClient>("grok"));
    }

    [Fact]
    public void ConfiguredIdWinsOverGeneratedAliasConflict()
    {
        // Section B's path segment is "fast", and section A has id="fast".
        // Configured id should win (registered before generated aliases).
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:grok:id"] = "fast",
                ["ai:clients:grok:modelid"] = "grok-4-fast",
                ["ai:clients:grok:apikey"] = "xai-asdfasdf",
                ["ai:clients:grok:endpoint"] = "https://api.x.ai",
                ["ai:clients:fast:modelid"] = "gpt-4.1.nano",
                ["ai:clients:fast:apikey"] = "sk-asdfasdf",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var grok = services.GetRequiredKeyedService<IChatClient>("ai:clients:grok");
        var fast = services.GetRequiredKeyedService<IChatClient>("ai:clients:fast");

        // "fast" as an alias key was claimed first by grok's configured id
        Assert.Same(grok, services.GetRequiredKeyedService<IChatClient>("fast"));
        Assert.NotSame(fast, services.GetRequiredKeyedService<IChatClient>("fast"));
    }

    [Fact]
    public void CanResolveFactoryByAlternateKeys()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:Grok:apikey"] = "xai-asdfasdf",
                ["ai:clients:Grok:endpoint"] = "https://api.x.ai",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var factory = services.GetRequiredKeyedService<IClientFactory>("ai:clients:Grok");

        Assert.Same(factory, services.GetRequiredKeyedService<IClientFactory>("ai:clients:grok"));
        Assert.Same(factory, services.GetRequiredKeyedService<IClientFactory>("ai.clients.Grok"));
        Assert.Same(factory, services.GetRequiredKeyedService<IClientFactory>("ai.clients.grok"));
        Assert.Same(factory, services.GetRequiredKeyedService<IClientFactory>("Grok"));
        Assert.Same(factory, services.GetRequiredKeyedService<IClientFactory>("grok"));
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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var grok = services.GetRequiredKeyedService<IChatClient>("ai:clients:grok");

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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var grok = services.GetRequiredKeyedService<IChatClient>("ai:clients:grok");

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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("ai:clients:openai");

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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("ai:clients:chat");

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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("ai:clients:chat");

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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("ai:clients:chat");

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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("ai:clients:openai");
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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("ai:clients:chat");
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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("ai:clients:chat");
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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("ai:clients:grok");
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
            .AddAIClients(configuration)
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
                ["ai:clients:openai:id"] = "default-openai",
                ["ai:clients:openai:modelid"] = "gpt-4.1.nano",
                ["ai:clients:openai:apikey"] = "sk-asdfasdf",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var factory = services.GetRequiredKeyedService<IClientFactory>("ai:clients:openai");
        var alias = services.GetRequiredKeyedService<IClientFactory>("default-openai");
        var defaultAlias = services.GetRequiredKeyedService<IClientFactory>("openai");
        var client = factory.CreateChatClient();

        Assert.Same(factory, alias);
        Assert.Same(factory, defaultAlias);
        Assert.Same(factory, services.GetRequiredKeyedService<IClientFactory>("ai.clients.openai"));
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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        Assert.NotNull(services.GetKeyedService<IClientFactory>("ai:clients:grok"));
        Assert.NotNull(services.GetKeyedService<IClientFactory>("grok"));
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
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var factory = services.GetRequiredKeyedService<IClientFactory>("ai:clients:openai");
        var original = factory.CreateChatClient();

        configuration["ai:clients:openai:modelid"] = "gpt-5";

        var updated = factory.CreateChatClient();

        Assert.Equal("gpt-4.1", original.GetRequiredService<ChatClientMetadata>().DefaultModelId);
        Assert.Equal("gpt-5", updated.GetRequiredService<ChatClientMetadata>().DefaultModelId);
    }

    [Fact]
    public void GlobalChatDefaultsApplyToAddClients()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:openai:modelid"] = "gpt-4.1",
                ["ai:clients:openai:apikey"] = "sk-asdfasdf",
            })
            .Build();

        var services = new ServiceCollection()
            .ConfigureChatClientDefaults(b => b.Use(inner => new MarkerChatClient(inner)))
            .AddSingleton<IConfiguration>(configuration)
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("ai:clients:openai");
        Assert.NotNull(client.GetService<MarkerChatClient>());
    }

    [Fact]
    public void SectionSpecificChatDefaultsApplyOnlyToMatchingSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:openai:modelid"] = "gpt-4.1",
                ["ai:clients:openai:apikey"] = "sk-asdfasdf",
                ["ai:clients:grok:modelid"] = "grok-4-fast",
                ["ai:clients:grok:apikey"] = "xai-asdfasdf",
                ["ai:clients:grok:endpoint"] = "https://api.x.ai",
            })
            .Build();

        var services = new ServiceCollection()
            .ConfigureChatClientDefaults("ai:clients:openai", b => b.Use(inner => new MarkerChatClient(inner)))
            .AddSingleton<IConfiguration>(configuration)
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var openai = services.GetRequiredKeyedService<IChatClient>("ai:clients:openai");
        var grok = services.GetRequiredKeyedService<IChatClient>("ai:clients:grok");

        Assert.NotNull(openai.GetService<MarkerChatClient>());
        Assert.Null(grok.GetService<MarkerChatClient>());
    }

    [Fact]
    public void MixedGlobalAndSectionDefaultsPreserveRegistrationOrder()
    {
        var order = new List<string>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:openai:modelid"] = "gpt-4.1",
                ["ai:clients:openai:apikey"] = "sk-asdfasdf",
            })
            .Build();

        var services = new ServiceCollection()
            .ConfigureChatClientDefaults(b =>
            {
                order.Add("global-1");
                b.Use(inner => new MarkerChatClient(inner));
            })
            .ConfigureChatClientDefaults("ai:clients:openai", b =>
            {
                order.Add("section");
                b.Use(inner => new SecondMarkerChatClient(inner));
            })
            .ConfigureChatClientDefaults(b => order.Add("global-2"))
            .AddSingleton<IConfiguration>(configuration)
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("ai:clients:openai");

        Assert.NotNull(client.GetService<MarkerChatClient>());
        Assert.NotNull(client.GetService<SecondMarkerChatClient>());
        Assert.Equal(["global-1", "section", "global-2"], order);
    }

    [Fact]
    public void SectionSpecificDefaultsDoNotMatchSimilarlyPrefixedSections()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:foo:modelid"] = "gpt-4.1",
                ["ai:clients:foo:apikey"] = "sk-asdfasdf",
                ["ai:clients:foobar:modelid"] = "gpt-4.1",
                ["ai:clients:foobar:apikey"] = "sk-asdfasdf",
            })
            .Build();

        var services = new ServiceCollection()
            .ConfigureChatClientDefaults("ai:clients:foo", b => b.Use(inner => new MarkerChatClient(inner)))
            .AddSingleton<IConfiguration>(configuration)
            .AddAIClients(configuration)
            .BuildServiceProvider();

        Assert.NotNull(services.GetRequiredKeyedService<IChatClient>("ai:clients:foo").GetService<MarkerChatClient>());
        Assert.Null(services.GetRequiredKeyedService<IChatClient>("ai:clients:foobar").GetService<MarkerChatClient>());
    }

    [Fact]
    public void ChatDefaultsWrapStableClientAcrossReload()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:openai:modelid"] = "gpt-4.1",
                ["ai:clients:openai:apikey"] = "sk-asdfasdf",
            })
            .Build();

        var services = new ServiceCollection()
            .ConfigureChatClientDefaults(b => b.Use(inner => new MarkerChatClient(inner)))
            .AddSingleton<IConfiguration>(configuration)
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var original = services.GetRequiredKeyedService<IChatClient>("ai:clients:openai");
        Assert.NotNull(original.GetService<MarkerChatClient>());
        Assert.Equal("gpt-4.1", original.GetRequiredService<ChatClientMetadata>().DefaultModelId);

        configuration["ai:clients:openai:modelid"] = "gpt-5";
        configuration.Reload();

        var updated = services.GetRequiredKeyedService<IChatClient>("ai:clients:openai");
        Assert.Same(original, updated);
        Assert.NotNull(updated.GetService<MarkerChatClient>());
        Assert.Equal("gpt-5", updated.GetRequiredService<ChatClientMetadata>().DefaultModelId);
    }

    [Fact]
    public void AddClientsAppliesDefaultsToChatFactory()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:openai:modelid"] = "gpt-4.1",
                ["ai:clients:openai:apikey"] = "sk-asdfasdf",
            })
            .Build();

        var services = new ServiceCollection()
            .ConfigureChatClientDefaults(b => b.Use(inner => new MarkerChatClient(inner)))
            .AddSingleton<IConfiguration>(configuration)
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var factory = services.GetRequiredKeyedService<IClientFactory>("ai:clients:openai");
        var alias = services.GetRequiredKeyedService<IClientFactory>("openai");
        var client = factory.CreateChatClient();

        Assert.Same(factory, alias);
        Assert.Same(factory, services.GetRequiredKeyedService<IClientFactory>("ai.clients.openai"));
        Assert.NotNull(client.GetService<MarkerChatClient>());
    }

    [Fact]
    public void AddClientsAppliesDefaultsToSpeechFactory()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:openai:modelid"] = "gpt-4o-transcribe",
                ["ai:clients:openai:apikey"] = "sk-asdfasdf",
            })
            .Build();

        var services = new ServiceCollection()
            .ConfigureTextToSpeechClientDefaults(b => b.Use(inner => new MarkerTextToSpeechClient(inner)))
            .ConfigureSpeechToTextClientDefaults(b => b.Use(inner => new MarkerSpeechToTextClient(inner)))
            .AddSingleton<IConfiguration>(configuration)
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var factory = services.GetRequiredKeyedService<IClientFactory>("ai:clients:openai");
        var textToSpeech = factory.CreateTextToSpeechClient();
        var speechToText = factory.CreateSpeechToTextClient();

        Assert.NotNull(textToSpeech.GetService<MarkerTextToSpeechClient>());
        Assert.NotNull(speechToText.GetService<MarkerSpeechToTextClient>());
    }

    [Fact]
    public void UnsupportedSpeechProviderFailsBeforeDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:azure:modelid"] = "gpt-4o",
                ["ai:clients:azure:apikey"] = "azure-asdfasdf",
                ["ai:clients:azure:endpoint"] = "https://ai.azure.com/deployment",
            })
            .Build();

        bool sttCalled = false;
        bool ttsCalled = false;

        var services = new ServiceCollection()
            .ConfigureSpeechToTextClientDefaults(b => sttCalled = true)
            .ConfigureTextToSpeechClientDefaults(b => ttsCalled = true)
            .AddSingleton<IConfiguration>(configuration)
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var factory = services.GetRequiredKeyedService<IClientFactory>("ai:clients:azure");

        Assert.Throws<NotSupportedException>(() => factory.CreateSpeechToTextClient());
        Assert.Throws<NotSupportedException>(() => factory.CreateTextToSpeechClient());
        Assert.False(sttCalled);
        Assert.False(ttsCalled);
    }

    [Fact]
    public void FactoryCreatedClientsReflectConfigChangesWithDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:openai:modelid"] = "gpt-4.1",
                ["ai:clients:openai:apikey"] = "sk-asdfasdf",
            })
            .Build();

        var services = new ServiceCollection()
            .ConfigureChatClientDefaults(b => b.Use(inner => new MarkerChatClient(inner)))
            .AddSingleton<IConfiguration>(configuration)
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var factory = services.GetRequiredKeyedService<IClientFactory>("ai:clients:openai");
        var client1 = factory.CreateChatClient();

        Assert.NotNull(client1.GetService<MarkerChatClient>());
        Assert.Equal("gpt-4.1", client1.GetRequiredService<ChatClientMetadata>().DefaultModelId);

        configuration["ai:clients:openai:modelid"] = "gpt-5";

        var client2 = factory.CreateChatClient();

        Assert.NotNull(client2.GetService<MarkerChatClient>());
        Assert.Equal("gpt-5", client2.GetRequiredService<ChatClientMetadata>().DefaultModelId);
        Assert.Equal("gpt-4.1", client1.GetRequiredService<ChatClientMetadata>().DefaultModelId);
    }

    [Fact]
    public void BoundFactoryReflectsProviderSwitch()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:chat:modelid"] = "gpt-4.1",
                ["ai:clients:chat:apikey"] = "sk-asdfasdf",
            })
            .Build();

        var services = new ServiceCollection()
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var factory = services.GetRequiredKeyedService<IClientFactory>("ai:clients:chat");
        var original = factory.CreateChatClient();
        Assert.Equal("openai", original.GetRequiredService<ChatClientMetadata>().ProviderName);

        // Switch to Grok by updating config — no reload required for IClientFactory
        configuration["ai:clients:chat:modelid"] = "grok-4";
        configuration["ai:clients:chat:apikey"] = "xai-asdfasdf";
        configuration["ai:clients:chat:endpoint"] = "https://api.x.ai";

        var updated = factory.CreateChatClient();
        Assert.Equal("xai", updated.GetRequiredService<ChatClientMetadata>().ProviderName);
        // Previous client still reflects original provider
        Assert.Equal("openai", original.GetRequiredService<ChatClientMetadata>().ProviderName);
    }

    [Fact]
    public void SubSectionChatClientInheritsParentApiKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:grok:apikey"] = "xai-asdfasdf",
                ["ai:clients:grok:chat:modelid"] = "grok-4-fast",
                ["ai:clients:grok:chat:endpoint"] = "https://api.x.ai",
            })
            .Build();

        var services = new ServiceCollection()
            .AddAIClients(configuration)
            .BuildServiceProvider();

        // Sub-section without direct apikey has no registered IClientFactory
        Assert.Null(services.GetKeyedService<IClientFactory>("ai:clients:grok:chat"));

        // But IChatClient resolves via apikey inheritance from the parent section
        var client = services.GetRequiredKeyedService<IChatClient>("ai:clients:grok:chat");
        var alias = services.GetRequiredKeyedService<IChatClient>("chat");
        Assert.Same(client, alias);
        Assert.Equal("xai", client.GetRequiredService<ChatClientMetadata>().ProviderName);
    }

    [Fact]
    public void ChatDefaultsAppliedExactlyOnceToRegisteredClient()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ai:clients:openai:modelid"] = "gpt-4.1",
                ["ai:clients:openai:apikey"] = "sk-asdfasdf",
            })
            .Build();

        int instanceCount = 0;
        var services = new ServiceCollection()
            .ConfigureChatClientDefaults(b => b.Use(inner =>
            {
                instanceCount++;
                return new MarkerChatClient(inner);
            }))
            .AddAIClients(configuration)
            .BuildServiceProvider();

        var client = services.GetRequiredKeyedService<IChatClient>("ai:clients:openai");
        Assert.Equal(1, instanceCount);
        Assert.NotNull(client.GetService<MarkerChatClient>());
    }

    sealed class MarkerChatClient(IChatClient inner) : DelegatingChatClient(inner)
    {
        public override object? GetService(Type serviceType, object? serviceKey = null)
            => serviceType == typeof(MarkerChatClient) ? this : base.GetService(serviceType, serviceKey);
    }

    sealed class SecondMarkerChatClient(IChatClient inner) : DelegatingChatClient(inner)
    {
        public override object? GetService(Type serviceType, object? serviceKey = null)
            => serviceType == typeof(SecondMarkerChatClient) ? this : base.GetService(serviceType, serviceKey);
    }

    sealed class MarkerTextToSpeechClient(ITextToSpeechClient inner) : DelegatingTextToSpeechClient(inner)
    {
        public override object? GetService(Type serviceType, object? serviceKey = null)
            => serviceType == typeof(MarkerTextToSpeechClient) ? this : base.GetService(serviceType, serviceKey);
    }

    sealed class MarkerSpeechToTextClient(ISpeechToTextClient inner) : DelegatingSpeechToTextClient(inner)
    {
        public override object? GetService(Type serviceType, object? serviceKey = null)
            => serviceType == typeof(MarkerSpeechToTextClient) ? this : base.GetService(serviceType, serviceKey);
    }
}
