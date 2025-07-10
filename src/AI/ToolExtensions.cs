using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Represents a tool call made by the AI, including the function call content and the result of the function execution.
/// </summary>
public record ToolCall(FunctionCallContent Call, FunctionResultContent Outcome);

/// <summary>
/// Represents a tool call made by the AI, including the function call content, the result of the function execution,
/// and the deserialized result of type <typeparamref name="TResult"/>.
/// </summary>
public record ToolCall<TResult>(FunctionCallContent Call, FunctionResultContent Outcome, TResult Result);

/// <summary>
/// Extensions for inspecting chat messages and responses for tool 
/// usage and processing responses.
/// </summary>
public static class ToolExtensions
{
    /// <summary>
    /// Looks for calls to a tool and their outcome.
    /// </summary>
    public static IEnumerable<ToolCall> FindCalls(this ChatResponse response, AIFunction tool)
        => FindCalls(response.Messages, tool.Name);

    /// <summary>
    /// Looks for calls to a tool and their outcome.
    /// </summary>
    public static IEnumerable<ToolCall> FindCalls(this ChatResponse response, string tool)
        => FindCalls(response.Messages, tool);

    /// <summary>
    /// Looks for calls to a tool and their outcome.
    /// </summary>
    public static IEnumerable<ToolCall> FindCalls(this IEnumerable<ChatMessage> messages, AIFunction tool)
        => FindCalls(messages, tool.Name);

    /// <summary>
    /// Looks for calls to a tool and their outcome.
    /// </summary>
    public static IEnumerable<ToolCall> FindCalls(this IEnumerable<ChatMessage> messages, string tool)
    {
        var calls = messages
            .Where(x => x.Role == ChatRole.Assistant)
            .SelectMany(x => x.Contents)
            .OfType<FunctionCallContent>()
            .Where(x => x.Name == tool)
            .ToDictionary(x => x.CallId);

        var results = messages
            .Where(x => x.Role == ChatRole.Tool)
            .SelectMany(x => x.Contents)
            .OfType<FunctionResultContent>()
            .Where(x => calls.TryGetValue(x.CallId, out var call) && call.Name == tool)
            .Select(x => new ToolCall(calls[x.CallId], x));

        return results;
    }

    /// <summary>
    /// Looks for a user prompt in the chat response messages. 
    /// </summary>
    /// <remarks>
    /// In order for this to work, the <see cref="AIFunctionFactory"/> must have been invoked using 
    /// the <see cref="ToolJsonOptions.Default"/> or with a <see cref="JsonSerializerOptions"/> configured 
    /// with <see cref="TypeInjectingResolverExtensions.WithTypeInjection(JsonSerializerOptions)"/> so 
    /// that the tool result type can be properly inspected.
    /// </remarks>
    public static IEnumerable<ToolCall<TResult>> FindCalls<TResult>(this ChatResponse response, AIFunction tool)
        => FindCalls<TResult>(response.Messages, tool.Name);

    /// <summary>
    /// Looks for a user prompt in the chat response messages. 
    /// </summary>
    /// <remarks>
    /// In order for this to work, the <see cref="AIFunctionFactory"/> must have been invoked using 
    /// the <see cref="ToolJsonOptions.Default"/> or with a <see cref="JsonSerializerOptions"/> configured 
    /// with <see cref="TypeInjectingResolverExtensions.WithTypeInjection(JsonSerializerOptions)"/> so 
    /// that the tool result type can be properly inspected.
    /// </remarks>
    public static IEnumerable<ToolCall<TResult>> FindCalls<TResult>(this IEnumerable<ChatMessage> messages, AIFunction tool)
        => FindCalls<TResult>(messages, tool.Name);

    /// <summary>
    /// Looks for a user prompt in the chat response messages. 
    /// </summary>
    /// <remarks>
    /// In order for this to work, the <see cref="AIFunctionFactory"/> must have been invoked using 
    /// the <see cref="ToolJsonOptions.Default"/> or with a <see cref="JsonSerializerOptions"/> configured 
    /// with <see cref="TypeInjectingResolverExtensions.WithTypeInjection(JsonSerializerOptions)"/> so 
    /// that the tool result type can be properly inspected.
    /// </remarks>
    public static IEnumerable<ToolCall<TResult>> FindCalls<TResult>(this IEnumerable<ChatMessage> messages, string tool)
    {
        var calls = FindCalls(messages, tool)
            .Where(x => x.Outcome.Result is JsonElement element &&
                        element.ValueKind == JsonValueKind.Object &&
                        element.TryGetProperty("$type", out var type) &&
                        type.GetString() == typeof(TResult).FullName)
            .Select(x => new ToolCall<TResult>(
                Call: x.Call,
                Outcome: x.Outcome,
                Result: JsonSerializer.Deserialize<TResult>((JsonElement)x.Outcome.Result!, ToolJsonOptions.Default) ??
                    throw new InvalidOperationException($"Failed to deserialize result for tool '{tool}' to {typeof(TResult).FullName}.")));

        return calls;
    }
}
