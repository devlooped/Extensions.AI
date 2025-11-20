using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Grpc.Net.Client;

namespace Devlooped.Extensions.AI.Grok;

public class GrokClient(string apiKey, GrokClientOptions options)
{
    static ConcurrentDictionary<(Uri, string), GrpcChannel> channels = [];

    public GrokClient(string apiKey) : this(apiKey, new GrokClientOptions()) { }

    public string ApiKey { get; } = apiKey;

    /// <summary>Gets or sets the endpoint for the service.</summary>
    public Uri Endpoint { get; set; } = options.Endpoint;

    /// <summary>Gets the options used to configure the client.</summary>
    public GrokClientOptions Options { get; } = options;

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
