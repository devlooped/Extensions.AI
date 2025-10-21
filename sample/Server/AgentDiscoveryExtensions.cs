using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Agents.AI.Hosting;

static class AgentDiscoveryExtensions
{
    public static void MapAgentDiscovery(this IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string path)
    {
        var routeGroup = endpoints.MapGroup(path);
        routeGroup.MapGet("/", async (AgentCatalog catalog, CancellationToken cancellation)
            => Results.Ok(await catalog
                .GetAgentsAsync(cancellation)
                .Select(agent => new AgentDiscoveryCard(agent.Name!, agent.Description))
                .ToArrayAsync()))
            .WithName("GetAgents");
    }

    record AgentDiscoveryCard(string Name,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description);
}
