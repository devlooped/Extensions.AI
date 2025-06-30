using OpenAI;

namespace Devlooped.Extensions.AI;

public class GrokClientOptions : OpenAIClientOptions
{
    public GrokClientOptions() : this("grok-3") { }

    public GrokClientOptions(string model)
    {
        Endpoint = new Uri("https://api.x.ai/v1");
        Model = Throw.IfNullOrEmpty(model);
    }

    public string Model { get; }
}
