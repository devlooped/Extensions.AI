using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Agents.AI;

static class AgentDiscoveryExtensions
{
    public static void MapAgentDiscovery(this IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string path)
    {
        var routeGroup = endpoints.MapGroup(path);
        routeGroup.MapGet("/", () => Results.Ok(endpoints.ServiceProvider
            .GetKeyedServices<AIAgent>(KeyedService.AnyKey)
            .Select(agent => new AgentDiscoveryCard(agent.Name!, agent.Description)).ToArray()))
            .WithName("GetAgents");
    }

    record AgentDiscoveryCard(string Name,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description);
}
