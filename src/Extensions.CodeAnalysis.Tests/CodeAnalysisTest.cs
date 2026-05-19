using Microsoft.CodeAnalysis;

namespace Devlooped.Extensions.AI.CodeAnalysis.Tests;

public sealed class AddChatClientsAnalyzerTests
{
    [Fact]
    public async Task AddChatClients_reports_DEAI001()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;

            class Program
            {
                static void Main()
                {
                    var builder = new HostApplicationBuilder();
                    builder.AddChatClients();
                }
            }
            """;

        var diagnostics = await RoslynTestHarness.GetAnalyzerDiagnosticsAsync(source);
        Assert.Contains(diagnostics, d => d.Id == DiagnosticIds.AddChatClientsRemoved);
    }

    [Fact]
    public async Task Complex_configure_reports_DEAI002()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;

            class Program
            {
                static void Main()
                {
                    var builder = new HostApplicationBuilder();
                    builder.AddChatClients(configure: (name, b) =>
                    {
                        switch (name)
                        {
                            case "AI:Clients:Grok": break;
                        }
                    });
                }
            }
            """;

        var diagnostics = await RoslynTestHarness.GetAnalyzerDiagnosticsAsync(source);
        Assert.Contains(diagnostics, d => d.Id == DiagnosticIds.AddChatClientsRemoved);
        Assert.Contains(diagnostics, d => d.Id == DiagnosticIds.ConfigureCallbackNotMigratable);
    }
}

public sealed class AddChatClientsCodeFixTests
{
    static string Normalize(string value) => string.Join('\n', value.Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd()));

    [Fact]
    public async Task Builder_without_configure_migrates_to_AddAIClients()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;

            class Program
            {
                static void Main()
                {
                    var builder = new HostApplicationBuilder();
                    builder.AddChatClients();
                }
            }
            """;

        const string expected = """
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;

            class Program
            {
                static void Main()
                {
                    var builder = new HostApplicationBuilder();
                    builder.AddAIClients();
                }
            }
            """;

        var fixedSource = await RoslynTestHarness.ApplyCodeFixAsync(source);
        Assert.Equal(Normalize(expected), Normalize(fixedSource));
    }

    [Fact]
    public async Task Services_with_positional_configure_migrates()
    {
        const string source = """
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.AI;

            class Program
            {
                static void Main(IConfiguration configuration, IServiceCollection services)
                {
                    services.AddChatClients(configuration, (name, builder) => { builder.GetType(); }, useDefaultProviders: false);
                }
            }
            """;

        var fixedSource = await RoslynTestHarness.ApplyCodeFixAsync(source);
        Assert.Contains("ConfigureChatClientDefaults", fixedSource);
        Assert.Contains("AddAIClients(configuration", fixedSource);
        Assert.Contains("useDefaultProviders: false", fixedSource);
        Assert.DoesNotContain("AddChatClients", fixedSource);
    }

    [Fact]
    public async Task Services_with_prefix_migrates_and_preserves_args()
    {
        const string source = """
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;

            class Program
            {
                static void Main(IConfiguration configuration, IServiceCollection services)
                {
                    services.AddChatClients(configuration, prefix: "ai:clients");
                }
            }
            """;

        const string expected = """
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;

            class Program
            {
                static void Main(IConfiguration configuration, IServiceCollection services)
                {
                    services.AddAIClients(configuration, prefix: "ai:clients");
                }
            }
            """;

        var fixedSource = await RoslynTestHarness.ApplyCodeFixAsync(source);
        Assert.Equal(Normalize(expected), Normalize(fixedSource));
    }

    [Fact]
    public async Task Configure_expression_body_migrates_to_global_defaults()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;
            using Microsoft.Extensions.AI;

            class Program
            {
                static void Main()
                {
                    var builder = new HostApplicationBuilder();
                    builder.AddChatClients(configure: (name, b) => { b.GetType(); });
                }
            }
            """;

        var fixedSource = await RoslynTestHarness.ApplyCodeFixAsync(source);
        Assert.Contains("ConfigureChatClientDefaults(b=>b.GetType())", Normalize(fixedSource).Replace(" ", ""));
        Assert.Contains("AddAIClients", fixedSource);
        Assert.DoesNotContain("AddChatClients", fixedSource);
    }

    [Fact]
    public async Task Positional_configure_expression_renames_builder_parameter()
    {
        const string source = """
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.AI;

            class Program
            {
                static void Main(IConfiguration configuration, IServiceCollection collection)
                {
                    collection.AddChatClients(configuration, (name, builder) => builder.UseLogging(), useDefaultProviders: false);
                }
            }
            """;

        var fixedSource = await RoslynTestHarness.ApplyCodeFixAsync(source);
        var normalized = Normalize(fixedSource).Replace(" ", "");
        Assert.Contains("ConfigureChatClientDefaults(b=>b.UseLogging())", normalized);
        Assert.Contains("AddAIClients(configuration,useDefaultProviders:false)", normalized);
        Assert.DoesNotContain("builder.UseLogging", normalized);
        Assert.DoesNotContain("AddChatClients", fixedSource);
    }

    [Fact]
    public async Task Configure_with_section_branch_migrates()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;
            using Microsoft.Extensions.AI;

            class Program
            {
                static void Main()
                {
                    var builder = new HostApplicationBuilder();
                    builder.AddChatClients(configure: (name, b) =>
                    {
                        b.GetType();
                        if (name == "AI:Clients:Grok")
                            b.GetHashCode();
                    });
                }
            }
            """;

        var fixedSource = await RoslynTestHarness.ApplyCodeFixAsync(source);
        Assert.Contains("ConfigureChatClientDefaults", fixedSource);
        Assert.Contains("ConfigureChatClientDefaults(\"AI:Clients:Grok\"", fixedSource);
        Assert.Contains("AddAIClients", fixedSource);
    }

    [Fact]
    public async Task Complex_configure_offers_no_code_fix()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;

            class Program
            {
                static void Main()
                {
                    var builder = new HostApplicationBuilder();
                    builder.AddChatClients(configure: (name, b) =>
                    {
                        switch (name)
                        {
                            case "AI:Clients:Grok": break;
                        }
                    });
                }
            }
            """;

        await Assert.ThrowsAsync<InvalidOperationException>(() => RoslynTestHarness.ApplyCodeFixAsync(source));
    }
}
