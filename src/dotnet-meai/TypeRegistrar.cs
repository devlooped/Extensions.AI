using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Devlooped.Extensions.AI;

#pragma warning disable DDI001
public sealed class TypeRegistrar(IServiceCollection? builder = default) : ITypeRegistrar, IServiceProvider
{
    readonly IServiceCollection builder = builder ?? new ServiceCollection();
    IServiceProvider? services;

    public IServiceCollection Services => builder;

    public object? GetService(Type serviceType) => Build().GetService(serviceType);

    public void Register(Type service, Type implementation)
    {
        ResetServiceProvider();
        builder.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        ResetServiceProvider();
        builder.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> func)
    {
        ThrowIfNull(func);
        ResetServiceProvider();
        builder.AddSingleton(service, (provider) => func());
    }

    IServiceProvider Build() => services ??= builder.BuildServiceProvider();

    ITypeResolver ITypeRegistrar.Build() => new TypeResolver(this);

    void ResetServiceProvider()
    {
        // Reset the service provider
        lock (this)
        {
            (services as IDisposable)?.Dispose();
            services = null;
        }
    }

    sealed class TypeResolver(TypeRegistrar registrar) : ITypeResolver, IServiceProvider, IDisposable
    {
        IServiceProvider services = registrar.Build();

        public object? Resolve(Type? type) => type == null ? null : services.GetService(type);

        public void Dispose() => registrar.ResetServiceProvider();

        public object? GetService(Type serviceType) => services.GetService(serviceType);
    }
}