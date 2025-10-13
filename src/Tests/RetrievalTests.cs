using Microsoft.Extensions.AI;
using OpenAI.Responses;
using static ConfigurationExtensions;

namespace Devlooped.Extensions.AI;

public class RetrievalTests(ITestOutputHelper output)
{
    [SecretsTheory("OPENAI_API_KEY")]
    [InlineData("gpt-4.1-nano", "Qué es la rebeldía en el Código Procesal Civil y Comercial Nacional?")]
    [InlineData("gpt-4.1-nano", "What's the battery life in an iPhone 15?", true)]
    public async Task CanRetrieveContent(string model, string question, bool empty = false)
    {
        var client = new global::OpenAI.OpenAIClient(Configuration["OPENAI_API_KEY"]);
        var store = client.GetVectorStoreClient().CreateVectorStore();
        try
        {
            var file = client.GetOpenAIFileClient().UploadFile("Content/LNS0004592.md", global::OpenAI.Files.FileUploadPurpose.Assistants);
            try
            {
                client.GetVectorStoreClient().AddFileToVectorStore(store.Value.Id, file.Value.Id);

                var responses = new OpenAIResponseClient(model, Configuration["OPENAI_API_KEY"]);

                var options = new ChatOptions();
                options.Tools ??= [];
                options.Tools.Add(new HostedFileSearchTool
                {
                    Inputs =
                    [
                        new HostedVectorStoreContent(store.Value.Id),
                    ],
                    MaximumResultCount = 10,
                });

                var chat = responses.AsIChatClient()
                    .AsBuilder()
                    .UseLogging(output.AsLoggerFactory())
                    //.Use((messages, options, next, cancellationToken) =>
                    //{
                    //    return next.Invoke(messages, options, cancellationToken);
                    //})
                    .Build();

                var response = await chat.GetResponseAsync(
                    [
                        new ChatMessage(ChatRole.System, "Use file search tool to respond, exclusively. If no content was found, respond just with the string 'N/A' and nothing else."),
                        new ChatMessage(ChatRole.User, question),
                    ], options);

                output.WriteLine(response.Text);
                if (empty)
                    Assert.Equal("N/A", response.Text);
                else
                    Assert.True(response.Text.Length > 50);
            }
            finally
            {
                client.GetOpenAIFileClient().DeleteFile(file.Value.Id);
            }
        }
        finally
        {
            client.GetVectorStoreClient().DeleteVectorStore(store.Value.Id);
        }
    }
}
