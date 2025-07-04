using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Creates tools for function calling that can leverage the <see cref="ToolExtensions"/> 
/// extension methods for locating invocations and their results.
/// </summary>
public static class ToolFactory
{
    /// <summary>
    /// Invokes <see cref="AIFunctionFactory.Create(Delegate, string?, string?, System.Text.Json.JsonSerializerOptions?)"/> 
    /// using the method name following the naming convention and serialization options from <see cref="ToolJsonOptions.Default"/>.
    /// </summary>
    public static AIFunction Create(Delegate method)
        => AIFunctionFactory.Create(method,
            ToolJsonOptions.Default.PropertyNamingPolicy!.ConvertName(method.Method.Name),
            serializerOptions: ToolJsonOptions.Default);
}
