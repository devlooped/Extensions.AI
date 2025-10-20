namespace Devlooped.Extensions.AI;

record OpenAIOptions(string Key, string[] Vectors)
{
    public static OpenAIOptions Empty { get; } = new();

    public OpenAIOptions() : this("", []) { }
}