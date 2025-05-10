using System.Diagnostics.CodeAnalysis;
using Devlooped.Sponsors;
using GitCredentialManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Devlooped.Extensions.AI;

public static class App
{
    static readonly CancellationTokenSource shutdownCancellation = new();

    static App() => Console.CancelKeyPress += (s, e) => shutdownCancellation.Cancel();

    /// <summary>
    /// Whether the CLI app is not interactive (i.e. part of a script run, 
    /// running in CI, or in a non-interactive user session).
    /// </summary>
    public static bool IsNonInteractive => !Environment.UserInteractive
        || Console.IsInputRedirected
        || Console.IsOutputRedirected;

    public static CommandApp Create() => Create(out _);

    public static CommandApp Create([NotNull] out ITypeRegistrar registrar)
        => Create(new ServiceCollection(), out registrar);

    public static CommandApp Create(IAnsiConsole console) => Create(console, out _);

    public static CommandApp Create(IAnsiConsole console, [NotNull] out ITypeRegistrar registrar)
    {
#pragma warning disable DDI001
        var app = Create(new ServiceCollection().AddSingleton(console), out registrar);
#pragma warning restore DDI001
        app.Configure(config => config.ConfigureConsole(console));
        return app;
    }

    static CommandApp Create(IServiceCollection collection, [NotNull] out ITypeRegistrar registrar)
    {
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
#if DEBUG
            .AddUserSecrets<TypeRegistrar>()
#endif
            .Build();

        collection.AddSingleton(config)
            .AddSingleton<IConfiguration>(_ => config)
            .AddSingleton(_ => CredentialManager.Create(ThisAssembly.Project.PackageId));

        collection.AddSingleton(shutdownCancellation);
        collection.AddServices();
        collection.ConfigureSponsors();

        registrar = new TypeRegistrar(collection);
        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            // Allows emitting help markdown on build
            if (Environment.GetEnvironmentVariables().Contains("NO_COLOR"))
                config.Settings.HelpProviderStyles = null;
        });

        app.UseSponsors();

        return app;
    }
}
