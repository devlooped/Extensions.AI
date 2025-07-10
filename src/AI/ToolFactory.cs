using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Creates tools for function calling that can leverage the <see cref="ToolExtensions"/> 
/// extension methods for locating invocations and their results.
/// </summary>
public static partial class ToolFactory
{
    /// <summary>
    /// Invokes <see cref="AIFunctionFactory.Create(Delegate, string?, string?, System.Text.Json.JsonSerializerOptions?)"/> 
    /// using the method name following the naming convention and serialization options from <see cref="ToolJsonOptions.Default"/> 
    /// so that <c>FindCalls</c> extension methods on <see cref="ChatResponse"/> can be used.
    /// </summary>
    public static AIFunction Create(Delegate method, string? name = default)
        => AIFunctionFactory.Create(method,
            name ?? ToolJsonOptions.Default.PropertyNamingPolicy!.ConvertName(SanitizeName(method.Method.Name)),
            serializerOptions: ToolJsonOptions.Default);

    static string SanitizeName(string name)
    {
        if (!name.Contains('<'))
            return name;

        // i.e.: <GetResponsesAsync>g__SetCandidates|0 > SetCandidates
        var match = AnonymousMethodExpr().Match(name);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return name
            .Replace("<", string.Empty)
            .Replace(">", string.Empty)
            .Replace("g__", "_")
            .Replace("|", string.Empty);
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"__(.+?)\|")]
    private static partial System.Text.RegularExpressions.Regex AnonymousMethodExpr();
}
