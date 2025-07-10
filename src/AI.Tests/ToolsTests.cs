using System.ComponentModel;
using Devlooped.Extensions.AI.OpenAI;
using Microsoft.Extensions.AI;
using static ConfigurationExtensions;

namespace Devlooped.Extensions.AI;

public class ToolsTests(ITestOutputHelper output)
{
    public record ToolResult(string Name, string Description, string Content);

    [Fact]
    public void SanitizesToolName()
    {
        static void DoSomething() { }

        var tool = ToolFactory.Create(DoSomething);

        Assert.Equal("do_something", tool.Name);
    }

    [SecretsFact("OPENAI_API_KEY")]
    public async Task RunToolResult()
    {
        var chat = new Chat()
        {
            { "system", "You make up a tool run by making up a name, description and content based on whatever the user says." },
            { "user", "I want to create an order for a dozen eggs" },
        };

        var client = new OpenAIChatClient(Configuration["OPENAI_API_KEY"]!, "gpt-4.1",
            global::OpenAI.OpenAIClientOptions.WriteTo(output))
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();

        var tool = ToolFactory.Create(RunTool);
        var options = new ChatOptions
        {
            ToolMode = ChatToolMode.RequireSpecific(tool.Name),
            Tools = [tool]
        };

        var response = await client.GetResponseAsync(chat, options);
        var result = response.FindCalls<ToolResult>(tool).FirstOrDefault();

        Assert.NotNull(result);
        Assert.NotNull(result.Call);
        Assert.Equal(tool.Name, result.Call.Name);
        Assert.NotNull(result.Outcome);
        Assert.Null(result.Outcome.Exception);
    }

    [SecretsFact("OPENAI_API_KEY")]
    public async Task RunToolTerminateResult()
    {
        var chat = new Chat()
        {
            { "system", "You make up a tool run by making up a name, description and content based on whatever the user says." },
            { "user", "I want to create an order for a dozen eggs" },
        };

        var client = new OpenAIChatClient(Configuration["OPENAI_API_KEY"]!, "gpt-4.1",
            global::OpenAI.OpenAIClientOptions.WriteTo(output))
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();

        var tool = ToolFactory.Create(RunToolTerminate);
        var options = new ChatOptions
        {
            ToolMode = ChatToolMode.RequireSpecific(tool.Name),
            Tools = [tool]
        };

        var response = await client.GetResponseAsync(chat, options);
        var result = response.FindCalls<ToolResult>(tool).FirstOrDefault();

        Assert.NotNull(result);
        Assert.NotNull(result.Call);
        Assert.Equal(tool.Name, result.Call.Name);
        Assert.NotNull(result.Outcome);
        Assert.Null(result.Outcome.Exception);
    }

    [SecretsFact("OPENAI_API_KEY")]
    public async Task RunToolExceptionOutcome()
    {
        var chat = new Chat()
        {
            { "system", "You make up a tool run by making up a name, description and content based on whatever the user says." },
            { "user", "I want to create an order for a dozen eggs" },
        };

        var client = new OpenAIChatClient(Configuration["OPENAI_API_KEY"]!, "gpt-4.1",
            global::OpenAI.OpenAIClientOptions.WriteTo(output))
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();

        var tool = ToolFactory.Create(RunToolThrows);
        var options = new ChatOptions
        {
            ToolMode = ChatToolMode.RequireSpecific(tool.Name),
            Tools = [tool]
        };

        var response = await client.GetResponseAsync(chat, options);
        var result = response.FindCalls(tool).FirstOrDefault();

        Assert.NotNull(result);
        Assert.NotNull(result.Call);
        Assert.Equal(tool.Name, result.Call.Name);
        Assert.NotNull(result.Outcome);
        Assert.NotNull(result.Outcome.Exception);
    }

    [Description("Runs a tool to provide a result based on user input.")]
    ToolResult RunTool(
        [Description("The name")] string name,
        [Description("The description")] string description,
        [Description("The content")] string content)
    {
        // Simulate running a tool and returning a result
        return new ToolResult(name, description, content);
    }

    [Description("Runs a tool to provide a result based on user input.")]
    ToolResult RunToolTerminate(
        [Description("The name")] string name,
        [Description("The description")] string description,
        [Description("The content")] string content)
    {
        FunctionInvokingChatClient.CurrentContext?.Terminate = true;
        // Simulate running a tool and returning a result
        return new ToolResult(name, description, content);
    }

    [Description("Runs a tool to provide a result based on user input.")]
    ToolResult RunToolThrows(
        [Description("The name")] string name,
        [Description("The description")] string description,
        [Description("The content")] string content)
    {
        FunctionInvokingChatClient.CurrentContext?.Terminate = true;
        throw new ArgumentException("BOOM");
    }
}
