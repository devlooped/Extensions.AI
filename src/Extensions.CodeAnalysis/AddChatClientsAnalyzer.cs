using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Devlooped.Extensions.AI.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AddChatClientsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [DiagnosticDescriptors.AddChatClientsRemoved, DiagnosticDescriptors.ConfigureCallbackNotMigratable];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation { TargetMethod: { } method } invocation)
            return;

        if (!IsAddChatClients(method))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.AddChatClientsRemoved,
            invocation.Syntax.GetLocation()));

        var configureArgument = AddChatClientsInvocationHelper.FindConfigureArgument(invocation);
        if (configureArgument is null)
            return;

        var isServiceCollection = IsServiceCollectionInvocation(invocation, method);

        if (ConfigureCallbackMigrator.CanMigrate(configureArgument, isServiceCollection, context.CancellationToken))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ConfigureCallbackNotMigratable,
            configureArgument.GetLocation()));
    }

    static bool IsAddChatClients(IMethodSymbol method)
    {
        var unreduced = method.ReducedFrom ?? method;
        return unreduced.Name == "AddChatClients"
               && unreduced.ContainingType?.Name == "ConfigurableChatClientExtensions"
               && unreduced.ContainingType.ContainingNamespace?.ToDisplayString() == "Microsoft.Extensions.DependencyInjection";
    }

    static bool IsServiceCollectionInvocation(IInvocationOperation invocation, IMethodSymbol method)
    {
        var unreduced = method.ReducedFrom ?? method;
        if (unreduced.Parameters.FirstOrDefault()?.Type.Name == "IServiceCollection")
            return true;

        if (invocation.Instance?.Type?.Name is "IServiceCollection")
            return true;

        return invocation.Arguments.Any(a => a.Parameter?.Name == "services");
    }
}
