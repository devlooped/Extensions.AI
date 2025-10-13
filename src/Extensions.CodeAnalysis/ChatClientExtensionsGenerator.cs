using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Devlooped.Extensions.AI;

/// <summary>
/// This generator produces the <see cref="ChatClientExtensions"/> source code so that it 
/// exists in the user's target compilation and can successfully overload (and override) 
/// the <c>OpenAIClientExtensions.AsIChatClient</c> that would otherwise be used. We 
/// need this to ensure that the <see cref="ChatClient"/> can be used directly as an 
/// <c>IChatClient</c> instead of wrapping it in the M.E.AI.OpenAI adapter.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class ChatClientExtensionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.AnalyzerConfigOptionsProvider
            .Select((options, _) => options.GlobalOptions.TryGetValue("build_property.HasDevloopedExtensionsAI", out var hasExtensions) ? !string.IsNullOrEmpty(hasExtensions) : false)
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(provider,
            (spc, source) =>
            {
                // TODO: the condition should actually be: ALWAYS ensure our extension method 
                // is accessible in the current compilation. If it's via a project reference and 
                // internals visible to, don't emit the source code, but if it's not available, 
                // we need to emit it. Since analyzers are transitive, we'd Just Work without 
                // the user having to determine if they need to add the package reference or not.
                if (!source.Left)
                    return;

                spc.AddSource(
                    $"Devlooped.{nameof(ThisAssembly.Resources.ChatClientExtensions)}.g.cs",
                    SourceText.From(ThisAssembly.Resources.ChatClientExtensions.Text, Encoding.UTF8));
            });
    }
}
