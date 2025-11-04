using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Devlooped.Agents.AI;

/// <summary>Provides a mechanism to configure AI agents.</summary>
public interface IAIAgentsBuilder : IHostApplicationBuilder
{
}

class DefaultAIAgentsBuilder(IHostApplicationBuilder builder) : IAIAgentsBuilder
{
    public IDictionary<object, object> Properties => builder.Properties;

    public IConfigurationManager Configuration => builder.Configuration;

    public IHostEnvironment Environment => builder.Environment;

    public ILoggingBuilder Logging => builder.Logging;

    public IMetricsBuilder Metrics => builder.Metrics;

    public IServiceCollection Services => builder.Services;

    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null) where TContainerBuilder : notnull => builder.ConfigureContainer(factory, configure);
}