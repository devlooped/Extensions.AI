using System.Text;
using Devlooped.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace Devlooped;

public class Misc
{
    [Fact]
    public void AddMarkdown()
    {
        var markdown =
            """
            ---
            id: ai.agents.tests
            name: TestAgent
            description: Test agent
            options: 
              temperature: 0.7
            use: ["foo", "bar"]
            ---
            Hello world
            """;

        var configuration = new ConfigurationBuilder()
            .AddAgentMarkdown(new MemoryStream(Encoding.UTF8.GetBytes(markdown)))
            .Build();

        Assert.Equal("TestAgent", configuration["ai:agents:tests:name"]);
        Assert.Equal("Hello world", configuration["ai:agents:tests:instructions"]);

        var agent = configuration.GetSection("ai:agents:tests").Get<AgentConfig>();

        Assert.NotNull(agent);
        Assert.Equal("TestAgent", agent.Name);
        Assert.Equal("Test agent", agent.Description);
        Assert.Equal(0.7f, agent.Options?.Temperature);
        Assert.Equal(["foo", "bar"], agent.Use);
        Assert.Equal("Hello world", agent.Instructions);
    }

    record AgentConfig(string Name, string Description, string Instructions, ChatOptions? Options, List<string> Use);
}
