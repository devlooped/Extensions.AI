using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI.Grok;

public static class GrokClientExtensions
{
    public static IChatClient AsIChatClient(this GrokClient client, string defaultModelId)
        => new GrokChatClient(client.Channel, client.Options, defaultModelId);
}
