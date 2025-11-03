using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Devlooped.Agents.AI;

/// <summary>
/// Concatenates multiple <see cref="AIContext"/> instances into a single one.
/// </summary>
class CompositeAIContextProvider : AIContextProvider
{
    readonly IList<AIContextProvider> providers;
    readonly AIContext? staticContext;

    public CompositeAIContextProvider(IList<AIContextProvider> providers)
    {
        this.providers = providers;

        // Special case for single provider of static contexts
        if (providers.Count == 1 && providers[0] is StaticAIContextProvider staticProvider)
        {
            staticContext = staticProvider.Context;
            return;
        }

        // Special case where all providers are static
        if (providers.All(x => x is StaticAIContextProvider))
        {
            // Concatenate instructions from all contexts
            staticContext = new();
            var instructions = new List<string>();
            var messages = new List<ChatMessage>();
            var tools = new List<AITool>();

            foreach (var provider in providers.Cast<StaticAIContextProvider>())
            {
                var ctx = provider.Context;

                if (!string.IsNullOrEmpty(ctx.Instructions))
                    instructions.Add(ctx.Instructions);

                if (ctx.Messages != null)
                    messages.AddRange(ctx.Messages);

                if (ctx.Tools != null)
                    tools.AddRange(ctx.Tools);
            }

            // Same separator used by M.A.AI for instructions appending from AIContext
            if (instructions.Count > 0)
                staticContext.Instructions = string.Join('\n', instructions);

            if (messages.Count > 0)
                staticContext.Messages = messages;

            if (tools.Count > 0)
                staticContext.Tools = tools;
        }
    }

    public override async ValueTask<AIContext> InvokingAsync(InvokingContext invoking, CancellationToken cancellationToken = default)
    {
        if (staticContext is not null)
            return staticContext;

        var context = new AIContext();
        var instructions = new List<string>();
        var messages = new List<ChatMessage>();
        var tools = new List<AITool>();

        foreach (var provider in providers)
        {
            var ctx = await provider.InvokingAsync(invoking, cancellationToken);

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

        return context;
    }
}