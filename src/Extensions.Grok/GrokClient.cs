using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using Grpc.Core;
using Grpc.Net.Client;

namespace Devlooped.Extensions.AI.Grok;

public class GrokClient
{
    static ConcurrentDictionary<(Uri, string), GrpcChannel> channels = [];

    public GrokClient(string apiKey) : this(apiKey, new GrokClientOptions()) { }

    public GrokClient(string apiKey, GrokClientOptions options)
    {
        ApiKey = apiKey;
        Options = options;
        Endpoint = options.Endpoint;
    }

    public string ApiKey { get; }

    /// <summary>Gets or sets the endpoint for the service.</summary>
    public Uri Endpoint { get; set; }

    /// <summary>Gets the options used to configure the client.</summary>
    public GrokClientOptions Options { get; }

    internal GrpcChannel Channel => channels.GetOrAdd((Endpoint, ApiKey), key =>
    {
        var handler = new AuthenticationHeaderHandler(ApiKey)
        { 
            InnerHandler = Options.ChannelOptions?.HttpHandler ?? new HttpClientHandler() 
        };

        var options = Options.ChannelOptions ?? new GrpcChannelOptions();
        options.HttpHandler = handler;

        return GrpcChannel.ForAddress(Endpoint, options);
    });

    class AuthenticationHeaderHandler(string apiKey) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
