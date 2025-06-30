using System.ComponentModel;
using Devlooped.Extensions.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Extensions for registering the <see cref="GrokClient"/> as a chat client in the service collection.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GrokServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the <see cref="GrokClient"/> as a chat client in the service collection.
        /// </summary>
        /// <param name="factory">The factory to create the Grok client.</param>
        /// <param name="lifetime">The optional service lifetime.</param>
        /// <returns>The <see cref="ChatClientBuilder"/> to further build the pipeline.</returns>
        public ChatClientBuilder AddGrok(Func<IConfiguration, GrokClient> factory, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            => services.AddChatClient(services
                => factory(services.GetRequiredService<IConfiguration>()), lifetime);
    }
}
