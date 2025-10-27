using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Devlooped.Agents.AI;

/// <summary>
/// Concatenates multiple <see cref="AIContext"/> instances into a single one.
/// </summary>
class CompositeAIContextProvider : AIContextProvider
{
    readonly AIContext context;

    public CompositeAIContextProvider(IList<AIContext> contexts)
    {
        if (contexts.Count == 1)
        {
            context = contexts[0];
            return;
        }

        // Concatenate instructions from all contexts
        context = new();
        var instructions = new List<string>();
        var messages = new List<ChatMessage>();
        var tools = new List<AITool>();

        foreach (var ctx in contexts)
        {
            if (!string.IsNullOrEmpty(ctx.Instructions))
                instructions.Add(ctx.Instructions);

            if (ctx.Messages != null)
                messages.AddRange(ctx.Messages);

            if (ctx.Tools != null)
                tools.AddRange(ctx.Tools);
        }

        // Same separator used by M.A.AI for instructions appending from AIContext
        if (instructions.Count > 0)
            context.Instructions = string.Join('\n', instructions);

        if (messages.Count > 0)
            context.Messages = messages;

        if (tools.Count > 0)
            context.Tools = tools;
    }

    public override ValueTask<AIContext> InvokingAsync(InvokingContext context, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(this.context);
}