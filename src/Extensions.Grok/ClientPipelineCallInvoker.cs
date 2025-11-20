using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Google.Protobuf;
using Grpc.Core;
using System.Buffers;
using System.Buffers.Binary;

namespace Devlooped.Extensions.AI.Grok;

class ClientPipelineCallInvoker(ClientPipeline pipeline, Uri endpoint) : CallInvoker
{
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
    {
        using var message = CreateMessage(method, options, request);
        pipeline.Send(message);
        
        var response = message.Response;
        EnsureSuccess(response);

        using var stream = response.ContentStream;
        if (stream == null) throw new RpcException(new Status(StatusCode.Internal, "Response content stream is null"));
        return ReadSingleResponse<TResponse>(stream);
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
    {
        var task = SendAsync(method, options, request);
        return new AsyncUnaryCall<TResponse>(task, Task.FromResult(new Metadata()), static () => Status.DefaultSuccess, static () => [], static () => { });
    }

    async Task<TResponse> SendAsync<TRequest, TResponse>(Method<TRequest, TResponse> method, CallOptions options, TRequest request)
        where TRequest : class
        where TResponse : class
    {
        using var message = CreateMessage(method, options, request);
        await pipeline.SendAsync(message).ConfigureAwait(false);

        var response = message.Response;
        EnsureSuccess(response);

        using var stream = response.ContentStream;
        if (stream == null) throw new RpcException(new Status(StatusCode.Internal, "Response content stream is null"));
        return await ReadSingleResponseAsync<TResponse>(stream).ConfigureAwait(false);
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
    {
        var responseStream = new ClientPipelineResponseStream<TResponse>(pipeline, CreateMessage(method, options, request));
        // We need to start the request.
        
        return new AsyncServerStreamingCall<TResponse>(
            responseStream,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => [],
            () => { }
        );
    }

    PipelineMessage CreateMessage<TRequest, TResponse>(Method<TRequest, TResponse> method, CallOptions options, TRequest request)
        where TRequest : class
        where TResponse : class
    {
        var message = pipeline.CreateMessage();
        message.ResponseClassifier = PipelineMessageClassifier.Create([200]);
        var req = message.Request;
        req.Method = "POST";
        req.Uri = new Uri(endpoint, method.FullName);
        req.Headers.Add("Content-Type", "application/grpc");
        req.Headers.Add("TE", "trailers");
        
        if (request is IMessage msg)
        {
            var length = msg.CalculateSize();
            var frame = new byte[length + 5];
            frame[0] = 0; // No compression
            BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(1), (uint)length);
            msg.WriteTo(frame.AsSpan(5));
            req.Content = BinaryContent.Create(BinaryData.FromBytes(frame));
        }

        if (options.CancellationToken != default)
        {
            message.Apply(new RequestOptions { CancellationToken = options.CancellationToken });
        }
        
        return message;
    }

    static void EnsureSuccess([NotNull] PipelineResponse? response)
    {
        if (response == null || response.IsError)
        {
            throw new RpcException(new Status(StatusCode.Internal, response?.ReasonPhrase ?? "Unknown error"));
        }
        if (response.Headers.TryGetValue("grpc-status", out var statusStr) && int.TryParse(statusStr, out var status) && status != 0)
        {
             response.Headers.TryGetValue("grpc-message", out var grpcMessage);
             throw new RpcException(new Status((StatusCode)status, grpcMessage ?? "Unknown gRPC error"));
        }
    }

    static TResponse ReadSingleResponse<TResponse>(Stream stream)
    {
        Span<byte> header = stackalloc byte[5];
        var read = 0;
        while (read < 5)
        {
            var r = stream.Read(header.Slice(read));
            if (r == 0) throw new IOException("Unexpected end of stream reading gRPC header");
            read += r;
        }
        
        var length = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(1));
        var data = ArrayPool<byte>.Shared.Rent((int)length);
        try
        {
            read = 0;
            while (read < length)
            {
                var r = stream.Read(data, read, (int)(length - read));
                if (r == 0) throw new IOException("Unexpected end of stream reading gRPC payload");
                read += r;
            }

            var instance = Activator.CreateInstance<TResponse>();
            if (instance is IMessage message)
            {
                message.MergeFrom(data.AsSpan(0, (int)length));
                return (TResponse)message;
            }
            throw new InvalidOperationException($"Type {typeof(TResponse)} is not a Protobuf Message.");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }

    static async Task<TResponse> ReadSingleResponseAsync<TResponse>(Stream stream)
    {
        var header = new byte[5];
        var read = 0;
        while (read < 5)
        {
            var r = await stream.ReadAsync(header.AsMemory(read, 5 - read)).ConfigureAwait(false);
            if (r == 0) throw new IOException("Unexpected end of stream reading gRPC header");
            read += r;
        }
        
        var length = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(1));
        var data = ArrayPool<byte>.Shared.Rent((int)length);
        try
        {
            read = 0;
            while (read < length)
            {
                var r = await stream.ReadAsync(data.AsMemory(read, (int)(length - read))).ConfigureAwait(false);
                if (r == 0) throw new IOException("Unexpected end of stream reading gRPC payload");
                read += r;
            }

            var instance = Activator.CreateInstance<TResponse>();
            if (instance is IMessage message)
            {
                message.MergeFrom(data.AsSpan(0, (int)length));
                return (TResponse)message;
            }
            throw new InvalidOperationException($"Type {typeof(TResponse)} is not a Protobuf Message.");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options)
    {
        throw new NotSupportedException("Client streaming is not supported over this adapter.");
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options)
    {
        throw new NotSupportedException("Duplex streaming is not supported over this adapter.");
    }
}

class ClientPipelineResponseStream<TResponse>(ClientPipeline pipeline, PipelineMessage message) : IAsyncStreamReader<TResponse> where TResponse : class
{
    IAsyncEnumerator<TResponse>? enumerator;
    TResponse? current;

    public Task StartAsync() => Task.CompletedTask;

    public TResponse Current => current ?? throw new InvalidOperationException("No current element");

    public async Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        if (enumerator == null)
        {
            await pipeline.SendAsync(message).ConfigureAwait(false);
            var response = message.Response;
            
            if (response == null || response.IsError)
            {
                 throw new RpcException(new Status(StatusCode.Internal, response?.ReasonPhrase ?? "Unknown error"));
            }
            if (response.Headers.TryGetValue("grpc-status", out var statusStr) && int.TryParse(statusStr, out var status) && status != 0)
            {
                 response.Headers.TryGetValue("grpc-message", out var grpcMessage);
                 throw new RpcException(new Status((StatusCode)status, grpcMessage ?? "Unknown gRPC error"));
            }

            enumerator = ReadStream(response.ContentStream!, cancellationToken).GetAsyncEnumerator(cancellationToken);
        }

        if (await enumerator.MoveNextAsync().ConfigureAwait(false))
        {
            current = enumerator.Current;
            return true;
        }
        
        return false;
    }

    async IAsyncEnumerable<TResponse> ReadStream(Stream stream, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var header = new byte[5];
        while (true)
        {
            var read = 0;
            while (read < 5)
            {
                var r = await stream.ReadAsync(header, read, 5 - read, cancellationToken).ConfigureAwait(false);
                if (r == 0)
                {
                    if (read == 0) yield break; // End of stream
                    throw new IOException("Unexpected end of stream reading gRPC header");
                }
                read += r;
            }

            var length = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(1));
            var data = ArrayPool<byte>.Shared.Rent((int)length);
            try
            {
                read = 0;
                while (read < length)
                {
                    var r = await stream.ReadAsync(data, read, (int)(length - read), cancellationToken).ConfigureAwait(false);
                    if (r == 0) throw new IOException("Unexpected end of stream reading gRPC payload");
                    read += r;
                }

                var instance = Activator.CreateInstance<TResponse>();
                if (instance is IMessage message)
                {
                    message.MergeFrom(data.AsSpan(0, (int)length));
                    yield return (TResponse)message;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(data);
            }
        }
    }
}
