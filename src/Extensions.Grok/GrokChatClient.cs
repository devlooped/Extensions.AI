using System.Text.Json;
using System.ClientModel.Primitives;

using Microsoft.Extensions.AI;

using XaiApi;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Xai = XaiApi.Chat;

namespace Devlooped.Extensions.AI.Grok;

class GrokChatClient : IChatClient
{
    readonly Xai.ChatClient client;
    readonly string defaultModelId;
    readonly GrokClientOptions clientOptions;

    internal GrokChatClient(GrpcChannel channel, GrokClientOptions clientOptions, string defaultModelId)
    {
        client = new Xai.ChatClient(channel);
        this.clientOptions = clientOptions;
        this.defaultModelId = defaultModelId;
    }

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var requestDto = MapToRequest(messages, options);
        
        var protoResponse = await client.GetCompletionAsync(requestDto, cancellationToken: cancellationToken);

        // Assuming single choice (n=1)
        var output = protoResponse.Outputs[0];
        
        return new ChatResponse(new[] { MapToChatMessage(output.Message) })
        {
            ResponseId = protoResponse.Id,
            ModelId = protoResponse.Model,
            CreatedAt = protoResponse.Created.ToDateTimeOffset(),
            FinishReason = MapFinishReason(output.FinishReason),
            Usage = MapToUsage(protoResponse.Usage),
        };
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        return CompleteChatStreamingCore(messages, options, cancellationToken);

        async IAsyncEnumerable<ChatResponseUpdate> CompleteChatStreamingCore(IEnumerable<ChatMessage> messages, ChatOptions? options, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var requestDto = MapToRequest(messages, options);
            
            using var call = client.GetCompletionChunk(requestDto, cancellationToken: cancellationToken);
            
            await foreach (var chunk in call.ResponseStream.ReadAllAsync(cancellationToken))
            {
                var outputChunk = chunk.Outputs[0];
                
                // Use positional arguments for ChatResponseUpdate
                yield return new ChatResponseUpdate(
                    outputChunk.Delta.Role != MessageRole.InvalidRole ? MapRole(outputChunk.Delta.Role) : null,
                    outputChunk.Delta.Content
                )
                {
                    ResponseId = chunk.Id,
                    ModelId = chunk.Model,
                    CreatedAt = chunk.Created.ToDateTimeOffset(),
                    FinishReason = outputChunk.FinishReason != FinishReason.ReasonInvalid ? MapFinishReason(outputChunk.FinishReason) : null,
                };
            }
        }
    }

    GetCompletionsRequest MapToRequest(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        var request = new GetCompletionsRequest
        {
            Model = options?.ModelId ?? defaultModelId,
        };

        if ((options?.EndUserId ?? clientOptions.EndUserId) is { } user) request.User = user;
        if (options?.MaxOutputTokens is { } maxTokens) request.MaxTokens = maxTokens;
        if (options?.Temperature is { } temperature) request.Temperature = temperature;
        if (options?.TopP is { } topP) request.TopP = topP;
        if (options?.FrequencyPenalty is { } frequencyPenalty) request.FrequencyPenalty = frequencyPenalty;
        if (options?.PresencePenalty is { } presencePenalty) request.PresencePenalty = presencePenalty;

        foreach (var message in messages)
        {
            var pMessage = new Message { Role = MapRole(message.Role) };
            if (message.Text is string text)
            {
                pMessage.Content.Add(new Content { Text = text });
            }
            request.Messages.Add(pMessage);
        }

        if (options?.Tools is not null)
        {
            foreach (var tool in options.Tools)
            {
                if (tool is AIFunction functionTool)
                {
                    var function = new Function
                    {
                        Name = functionTool.Name,
                        Description = functionTool.Description,
                        Parameters = JsonSerializer.Serialize(functionTool.JsonSchema)
                    };
                    request.Tools.Add(new Tool { Function = function });
                }
            }
        }

        if (options?.ResponseFormat is ChatResponseFormatJson)
        {
            request.ResponseFormat = new ResponseFormat
            {
                FormatType = FormatType.JsonObject
            };
        }
        else
        {
            request.ResponseFormat = new ResponseFormat { FormatType = FormatType.Text };
        }

        return request;
    }

    static MessageRole MapRole(ChatRole role) => role switch
    {
        _ when role == ChatRole.System => MessageRole.RoleSystem,
        _ when role == ChatRole.User => MessageRole.RoleUser,
        _ when role == ChatRole.Assistant => MessageRole.RoleAssistant,
        _ when role == ChatRole.Tool => MessageRole.RoleTool,
        _ => MessageRole.RoleUser
    };
    
    static ChatRole MapRole(MessageRole role) => role switch
    {
        MessageRole.RoleSystem => ChatRole.System,
        MessageRole.RoleUser => ChatRole.User,
        MessageRole.RoleAssistant => ChatRole.Assistant,
        MessageRole.RoleTool => ChatRole.Tool,
        _ => ChatRole.Assistant
    };

    static ChatMessage MapToChatMessage(CompletionMessage message)
    {
        var chatMessage = new ChatMessage(ChatRole.Assistant, message.Content);

        foreach (var toolCall in message.ToolCalls)
        {
            if (toolCall.ToolCase == XaiApi.ToolCall.ToolOneofCase.Function)
            {
                var arguments = !string.IsNullOrEmpty(toolCall.Function.Arguments)
                    ? JsonSerializer.Deserialize<IDictionary<string, object?>>(toolCall.Function.Arguments)
                    : null;

                chatMessage.Contents.Add(new FunctionCallContent(
                    toolCall.Id,
                    toolCall.Function.Name,
                    arguments));
            }
        }

        return chatMessage;
    }

    static ChatFinishReason? MapFinishReason(FinishReason finishReason) => finishReason switch
    {
        FinishReason.ReasonStop => ChatFinishReason.Stop,
        FinishReason.ReasonMaxLen => ChatFinishReason.Length,
        FinishReason.ReasonToolCalls => ChatFinishReason.ToolCalls,
        FinishReason.ReasonMaxContext => ChatFinishReason.Length,
        FinishReason.ReasonTimeLimit => ChatFinishReason.Length,
        _ => null
    };

    static Microsoft.Extensions.AI.UsageDetails? MapToUsage(SamplingUsage usage) => usage == null ? null : new()
    {
        InputTokenCount = (int)usage.PromptTokens,
        OutputTokenCount = (int)usage.CompletionTokens,
        TotalTokenCount = (int)usage.TotalTokens
    };

    public object? GetService(Type serviceType, object? serviceKey = null) => 
        serviceType == typeof(GrokChatClient) ? this : null;

    public void Dispose() {}
}
