using System.Collections.Immutable;
using Basic.Reference.Assemblies;
using Devlooped.Extensions.AI.CodeFix;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Devlooped.Extensions.AI.CodeAnalysis.Tests;

static class RoslynTestHarness
{
    internal static readonly MetadataReference[] References =
    [
        ..Net80.References.All,
        MetadataReference.CreateFromFile(typeof(ConfigurableChatClientExtensions).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.AI.ChatClientBuilder).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(HostApplicationBuilder).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(IHostApplicationBuilder).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(IConfiguration).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(ILoggerFactory).Assembly.Location),
    ];

    public static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(string source, CancellationToken cancellationToken = default)
    {
        var (compilation, _) = await CreateDocumentAndCompilationAsync(source, cancellationToken).ConfigureAwait(false);
        var analyzer = new AddChatClientsAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<string> ApplyCodeFixAsync(string source, CancellationToken cancellationToken = default)
    {
        var (compilation, document) = await CreateDocumentAndCompilationAsync(source, cancellationToken).ConfigureAwait(false);
        var analyzer = new AddChatClientsAnalyzer();
        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync(cancellationToken)
            .ConfigureAwait(false);

        var deai001 = diagnostics.First(d => d.Id == DiagnosticIds.AddChatClientsRemoved);

        var provider = new AddChatClientsCodeFixProvider();
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            deai001.Location.SourceSpan,
            ImmutableArray.Create(deai001),
            (action, _) => actions.Add(action),
            cancellationToken);

        await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        if (actions.Count == 0)
            throw new InvalidOperationException("No code fix was registered.");

        var operations = await actions[0].GetOperationsAsync(cancellationToken).ConfigureAwait(false);
        var solution = document.Project.Solution;
        foreach (var operation in operations)
        {
            if (operation is ApplyChangesOperation apply)
                solution = apply.ChangedSolution;
        }

        var fixedDocument = solution.GetDocument(document.Id)!;
        return (await fixedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false)).ToString();
    }

    static async Task<(Compilation Compilation, Document Document)> CreateDocumentAndCompilationAsync(
        string source,
        CancellationToken cancellationToken)
    {
        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .AddMetadataReferences(projectId, References)
            .AddDocument(documentId, "Test.cs", source);

        workspace.TryApplyChanges(solution);
        var document = workspace.CurrentSolution.GetDocument(documentId)!;
        var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Failed to create compilation.");
        return (compilation, document);
    }
}
