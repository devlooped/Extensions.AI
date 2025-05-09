using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using static ConfigurationExtensions;

namespace Devlooped.Extensions.AI;

public class RetrievalTests(ITestOutputHelper output)
{
    [SecretsTheory("OpenAI:Key")]
    [InlineData("gpt-4.1-mini", "Qué es la rebeldía en el Código Procesal Civil y Comercial Nacional?")]
    [InlineData("gpt-4.1-nano", "What's the battery life in an iPhone 15?", true)]
    public async Task CanRetrieveContent(string model, string question, bool empty = false)
    {
        var client = new OpenAI.OpenAIClient(Configuration["OpenAI:Key"]);
        var store = client.GetVectorStoreClient().CreateVectorStore(true);
        try
        {
            var file = client.GetOpenAIFileClient().UploadFile("Content/LNS0004592.md", OpenAI.Files.FileUploadPurpose.Assistants);
            try
            {
                client.GetVectorStoreClient().AddFileToVectorStore(store.VectorStoreId, file.Value.Id, true);

                var responses = new OpenAIResponseClient(model, Configuration["OpenAI:Key"]);

                var chat = responses.AsIChatClient(
                        ResponseTool.CreateFileSearchTool([store.VectorStoreId]))
                    .AsBuilder()
                    .UseLogging(output.AsLoggerFactory())
                    .Build();

                var response = await chat.GetResponseAsync(
                    [
                        new ChatMessage(ChatRole.System, "Use file search tool to respond, exclusively. If no content was found, respond just with the string 'N/A' and nothing else."),
                        new ChatMessage(ChatRole.User, question),
                    ]);

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
            client.GetVectorStoreClient().DeleteVectorStore(store.VectorStoreId);
        }
    }
}
