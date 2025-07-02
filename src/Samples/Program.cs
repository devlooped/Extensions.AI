// Sample X.AI client usage with .NET
var messages = new Chat()
{
    { "system", "You are an AI assistant that knows how to search the web." },
    { "user", "What's Tesla stock worth today? Search X and the news for latest info." },
};

// Env supports .env as well as all standard .NET configuration sources
var grok = new GrokClient(Throw.IfNullOrEmpty(Env.Get("XAI_API_KEY")), new OpenAI.OpenAIClientOptions()
    .UseJsonConsoleLogging(new() { WrapLength = 80 }));

var options = new ChatOptions
{
    // Enables Live Search
    Tools = [new HostedWebSearchTool()]
};

var chat = grok.GetChatClient("grok-3").AsIChatClient();
var response = await chat.GetResponseAsync(messages, options);

AnsiConsole.MarkupLine($":robot: {response.Text.EscapeMarkup()}");