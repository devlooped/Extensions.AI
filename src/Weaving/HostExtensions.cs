using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Weaving;

/// <summary>
/// Extensions for configuring the host application builder and accessing services.
/// </summary>
public static class HostExtensions
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        /// <summary>
        /// Configures the host configuration.
        /// </summary>
        public TBuilder Configure(Action<IConfigurationManager> configure)
        {
            configure?.Invoke(builder.Configuration);
            return builder;
        }

        /// <summary>
        /// Configures services in the host.
        /// </summary>
        public TBuilder ConfigureServices(Action<IServiceCollection> configure)
        {
            configure?.Invoke(builder.Services);
            return builder;
        }
    }

    extension(IHost app)
    {
        /// <summary>
        /// Gest the host app configuration;
        /// </summary>
        public IConfiguration Configuration => app.Services.GetRequiredService<IConfiguration>();

        /// <summary>
        /// Gets the default AI client for the app.
        /// </summary>
        public IChatClient ChatClient => app.Services.GetRequiredService<IChatClient>();
    }
}
