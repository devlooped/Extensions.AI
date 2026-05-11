using Json5;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Devlooped;

public class EndToEnd
{
    static readonly IConfigurationRoot configuration = new ConfigurationBuilder()
        .AddUserSecrets<EndToEnd>()
        .AddJson5File("EndToEnd.json5")
        .Build();

    [SecretsFact("AI:Clients:XAI:ApiKey")]
    public async Task GetText()
    {
        var services = new ServiceCollection()
            .ConfigureChatClientDefaults(builder => builder.UseLogging())
            .AddChatClients(configuration)
            .BuildServiceProvider();

        var chat = services.GetChatClient("XAI");

        Assert.NotNull(chat);

        var hello = await chat.GetResponseAsync("Hi there!");

        //var tts = services.GetChatClient.GetTextToSpeechClient("XAI");

        //Assert.NotNull(tts);
    }
}
