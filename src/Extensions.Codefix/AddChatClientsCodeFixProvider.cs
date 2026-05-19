using System.Collections.Immutable;
using System.Composition;
using Devlooped.Extensions.AI.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Devlooped.Extensions.AI.CodeFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddChatClientsCodeFixProvider)), Shared]
public sealed class AddChatClientsCodeFixProvider : CodeFixProvider
{
    public const string FixTitle = "Migrate to AddAIClients";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [DiagnosticIds.AddChatClientsRemoved];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Location.SourceTree is null)
                continue;

            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (invocation is null)
                continue;

            var configureArgument = AddChatClientsInvocationHelper.FindConfigureArgument(invocation, semanticModel, context.CancellationToken);
            var isServiceCollection = IsServiceCollectionInvocation(invocation, semanticModel, context.CancellationToken);

            if (configureArgument is not null
                && ConfigureCallbackMigrator.TryAnalyze(configureArgument, isServiceCollection, context.CancellationToken) is null)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    FixTitle,
                    cancellationToken => ApplyFixAsync(context.Document, invocation, semanticModel, cancellationToken),
                    equivalenceKey: FixTitle),
                diagnostic);
        }
    }

    static async Task<Document> ApplyFixAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var configureArgument = AddChatClientsInvocationHelper.FindConfigureArgument(invocation, semanticModel, cancellationToken);

        ExpressionSyntax receiver;
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            receiver = memberAccess.Expression;
        else
            return document;

        var isServiceCollection = IsServiceCollectionInvocation(invocation, semanticModel, cancellationToken);
        var addAiClientsArguments = GetAddAIClientsArguments(invocation, semanticModel, cancellationToken);

        if (configureArgument is not null
            && ConfigureCallbackMigrator.TryAnalyze(configureArgument, isServiceCollection, cancellationToken) is { } plan)
        {
            ExpressionSyntax current = receiver;
            foreach (var section in plan.Sections)
                current = CreateConfigureDefaultsCall(current, section);

            var addAiClients = CreateAddAIClientsInvocation(current, addAiClientsArguments);
            editor.ReplaceNode(invocation, addAiClients);
            return editor.GetChangedDocument();
        }

        var simpleAdd = CreateAddAIClientsInvocation(receiver, addAiClientsArguments);
        if (invocation.Parent is ExpressionSyntax parent && parent is MemberAccessExpressionSyntax or InvocationExpressionSyntax)
        {
            editor.ReplaceNode(invocation, simpleAdd);
        }
        else if (invocation.Parent is ExpressionStatementSyntax statement)
        {
            editor.ReplaceNode(statement, SyntaxFactory.ExpressionStatement(simpleAdd).WithTrailingTrivia(statement.GetTrailingTrivia()));
        }
        else
        {
            editor.ReplaceNode(invocation, simpleAdd);
        }

        return editor.GetChangedDocument();
    }

    static ExpressionSyntax CreateConfigureDefaultsCall(
        ExpressionSyntax receiver,
        ConfigureCallbackMigrator.SectionDefaults section)
    {
        var configureLambda = ConfigureCallbackMigrator.CreateConfigureDefaultsLambda(section);

        if (section.SectionPath is { } path)
        {
            var args = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(path))),
                SyntaxFactory.Argument(configureLambda),
            ]));

            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    receiver,
                    SyntaxFactory.IdentifierName("ConfigureChatClientDefaults")),
                args);
        }

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                receiver,
                SyntaxFactory.IdentifierName("ConfigureChatClientDefaults")),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(configureLambda))));
    }

    static SeparatedSyntaxList<ArgumentSyntax> GetAddAIClientsArguments(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
        => SyntaxFactory.SeparatedList(
            AddChatClientsInvocationHelper.GetNonConfigureArguments(invocation, semanticModel, cancellationToken).ToArray());

    static InvocationExpressionSyntax CreateAddAIClientsInvocation(
        ExpressionSyntax receiver,
        SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var argumentList = arguments.Count == 0
            ? SyntaxFactory.ArgumentList()
            : SyntaxFactory.ArgumentList(arguments);

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                receiver,
                SyntaxFactory.IdentifierName("AddAIClients")),
            argumentList);
    }

    static bool IsServiceCollectionInvocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        var receiverType = semanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken).Type;
        return receiverType?.Name == "IServiceCollection";
    }

    static ArgumentSyntax? GetNamedArgument(InvocationExpressionSyntax invocation, string name)
    {
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (argument.NameColon?.Name.Identifier.Text == name)
                return argument;
        }

        return null;
    }

}
