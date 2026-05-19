using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Devlooped.Extensions.AI.CodeAnalysis;

/// <summary>
/// Analyzes <c>configure: (name, b) =&gt; ...</c> callbacks for tiered migration to ConfigureChatClientDefaults.
/// </summary>
public static class ConfigureCallbackMigrator
{
    public static bool CanMigrate(ArgumentSyntax configureArgument, bool isServiceCollection, CancellationToken cancellationToken)
        => TryAnalyze(configureArgument, isServiceCollection, cancellationToken) is not null;

    public static MigrationPlan? TryAnalyze(ArgumentSyntax configureArgument, bool isServiceCollection, CancellationToken cancellationToken)
    {
        if (configureArgument.Expression is not LambdaExpressionSyntax lambda)
            return null;

        if (lambda is not ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: 2 } parenthesized)
            return null;

        var nameParam = parenthesized.ParameterList.Parameters[0].Identifier.Text;
        var builderParam = parenthesized.ParameterList.Parameters[1].Identifier.Text;

        if (lambda.Body is ExpressionSyntax expressionBody)
        {
            if (!UsesBuilder(expressionBody, builderParam))
                return null;

            return new MigrationPlan(
                [new SectionDefaults(null, expressionBody, builderParam)],
                isServiceCollection);
        }

        if (lambda.Body is not BlockSyntax block)
            return null;

        var globalStatements = new List<StatementSyntax>();
        var sectionDefaults = new List<SectionDefaults>();
        var hasUnsupported = false;

        foreach (var statement in block.Statements)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsUnconditionalBuilderStatement(statement, builderParam))
            {
                globalStatements.Add(statement);
                continue;
            }

            if (TryParseSectionBranch(statement, nameParam, builderParam, out var sectionPath, out var branchBody))
            {
                sectionDefaults.Add(new SectionDefaults(sectionPath, branchBody, builderParam));
                continue;
            }

            hasUnsupported = true;
            break;
        }

        if (hasUnsupported)
            return null;

        if (globalStatements.Count == 0 && sectionDefaults.Count == 0)
            return null;

        var sections = new List<SectionDefaults>();
        if (globalStatements.Count > 0)
            sections.Add(new SectionDefaults(null, StatementsToBody(globalStatements), builderParam));

        sections.AddRange(sectionDefaults);
        return new MigrationPlan(sections, isServiceCollection);
    }

    static bool IsUnconditionalBuilderStatement(StatementSyntax statement, string builderParam)
        => statement switch
        {
            ExpressionStatementSyntax { Expression: var expr } => UsesBuilder(expr, builderParam),
            _ => false,
        };

    static bool TryParseSectionBranch(
        StatementSyntax statement,
        string nameParam,
        string builderParam,
        out string sectionPath,
        out CSharpSyntaxNode branchBody)
    {
        sectionPath = null!;
        branchBody = null!;

        if (statement is not IfStatementSyntax { Else: null } ifStatement)
            return false;

        if (!TryGetSectionPath(ifStatement.Condition, nameParam, out sectionPath))
            return false;

        if (ifStatement.Statement is BlockSyntax branchBlock)
        {
            if (branchBlock.Statements.Count != 1)
                return false;

            if (!IsUnconditionalBuilderStatement(branchBlock.Statements[0], builderParam))
                return false;

            branchBody = branchBlock.Statements[0] is ExpressionStatementSyntax expr
                ? expr.Expression
                : branchBlock.Statements[0];
            return true;
        }

        if (IsUnconditionalBuilderStatement(ifStatement.Statement, builderParam))
        {
            branchBody = ifStatement.Statement is ExpressionStatementSyntax expr
                ? expr.Expression
                : ifStatement.Statement;
            return true;
        }

        return false;
    }

    static bool TryGetSectionPath(ExpressionSyntax condition, string nameParam, out string sectionPath)
    {
        sectionPath = null!;

        switch (condition)
        {
            case BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } binary
                when binary.Left is IdentifierNameSyntax left && left.Identifier.Text == nameParam
                     && binary.Right is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } literal:
                sectionPath = literal.Token.ValueText;
                return true;

            case InvocationExpressionSyntax invocation
                when invocation.Expression is MemberAccessExpressionSyntax
                {
                    Name.Identifier.Text: "Equals",
                    Expression: IdentifierNameSyntax target,
                }
                && target.Identifier.Text == nameParam:
                return TryGetEqualsSectionPath(invocation, out sectionPath);

            case InvocationExpressionSyntax staticEquals
                when staticEquals.Expression is MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.Text: "string" },
                    Name.Identifier.Text: "Equals",
                }
                && staticEquals.ArgumentList.Arguments.Count >= 2
                && staticEquals.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax first
                && first.Identifier.Text == nameParam:
                return TryGetEqualsSectionPath(staticEquals, out sectionPath);

            default:
                return false;
        }
    }

    static bool TryGetEqualsSectionPath(InvocationExpressionSyntax invocation, out string sectionPath)
    {
        sectionPath = null!;

        if (invocation.ArgumentList.Arguments.Count is 1 or 2)
        {
            var first = invocation.ArgumentList.Arguments[0].Expression;
            if (first is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } literal)
            {
                sectionPath = literal.Token.ValueText;
                return true;
            }
        }

        if (invocation.ArgumentList.Arguments.Count == 3
            && invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax path
            && path.RawKind == (int)SyntaxKind.StringLiteralExpression)
        {
            sectionPath = path.Token.ValueText;
            return true;
        }

        return false;
    }

    static bool UsesBuilder(ExpressionSyntax expression, string builderParam)
    {
        foreach (var identifier in expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
        {
            if (identifier.Identifier.Text == builderParam)
                return true;
        }

        return false;
    }

    static CSharpSyntaxNode StatementsToBody(IReadOnlyList<StatementSyntax> statements)
    {
        if (statements.Count == 1 && statements[0] is ExpressionStatementSyntax expr)
            return expr.Expression;

        return SyntaxFactory.Block(statements);
    }

    public static LambdaExpressionSyntax CreateConfigureDefaultsLambda(SectionDefaults section)
        => ConfigureCallbackSyntax.CreateDefaultsLambda(section);

    public sealed record MigrationPlan(IReadOnlyList<SectionDefaults> Sections, bool IsServiceCollection);

    public sealed record SectionDefaults(string? SectionPath, CSharpSyntaxNode Body, string BuilderParameterName);
}
