using Microsoft.Agents.AI;

namespace Devlooped.Agents.AI;

class StaticAIContextProvider(AIContext context) : AIContextProvider
{
    public AIContext Context => context;

    public override ValueTask<AIContext> InvokingAsync(InvokingContext context, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Context);
}
