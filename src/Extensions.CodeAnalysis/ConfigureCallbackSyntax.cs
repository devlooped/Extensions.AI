using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Devlooped.Extensions.AI.CodeAnalysis;

static class ConfigureCallbackSyntax
{
    internal const string DefaultsBuilderParameterName = "b";

    public static LambdaExpressionSyntax CreateDefaultsLambda(ConfigureCallbackMigrator.SectionDefaults section)
    {
        var body = RenameBuilderIdentifier(section.Body, section.BuilderParameterName, DefaultsBuilderParameterName);

        if (body is ExpressionSyntax expression)
            return SyntaxFactory.SimpleLambdaExpression(CreateBuilderParameter(), expression);

        if (body is BlockSyntax { Statements.Count: 1 } block
            && block.Statements[0] is ExpressionStatementSyntax { Expression: var single })
        {
            var expr = RenameBuilderIdentifier(single, section.BuilderParameterName, DefaultsBuilderParameterName);
            return SyntaxFactory.SimpleLambdaExpression(CreateBuilderParameter(), expr);
        }

        var blockBody = body as BlockSyntax ?? SyntaxFactory.Block((StatementSyntax)body);
        return SyntaxFactory.ParenthesizedLambdaExpression(
            SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(CreateBuilderParameter())),
            blockBody);
    }

    static ParameterSyntax CreateBuilderParameter()
        => SyntaxFactory.Parameter(SyntaxFactory.Identifier(DefaultsBuilderParameterName));

    static CSharpSyntaxNode RenameBuilderIdentifier(CSharpSyntaxNode node, string oldName, string newName)
    {
        if (oldName == newName)
            return node;

        return (CSharpSyntaxNode)new BuilderParameterRenameRewriter(oldName, newName).Visit(node)! ?? node;
    }

    sealed class BuilderParameterRenameRewriter(string oldName, string newName) : CSharpSyntaxRewriter
    {
        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            if (token.IsKind(SyntaxKind.IdentifierToken) && token.Text == oldName)
                return SyntaxFactory.Identifier(newName).WithTriviaFrom(token);

            return base.VisitToken(token);
        }
    }

}
