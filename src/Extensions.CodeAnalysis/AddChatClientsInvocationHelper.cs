using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Devlooped.Extensions.AI.CodeAnalysis;

/// <summary>
/// Resolves <c>AddChatClients</c> invocation arguments using semantic argument-to-parameter binding.
/// </summary>
public static class AddChatClientsInvocationHelper
{
    public static ArgumentSyntax? FindConfigureArgument(IInvocationOperation invocation)
    {
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.Name == "configure" && argument.Syntax is ArgumentSyntax syntax)
                return syntax;
        }

        return null;
    }

    public static ArgumentSyntax? FindConfigureArgument(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken = default)
    {
        if (semanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation invocationOperation)
            return null;

        return FindConfigureArgument(invocationOperation);
    }

    public static IEnumerable<ArgumentSyntax> GetNonConfigureArguments(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken = default)
    {
        if (semanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation invocationOperation)
        {
            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                if (argument.NameColon?.Name.Identifier.Text != "configure")
                    yield return argument;
            }

            yield break;
        }

        foreach (var argument in invocationOperation.Arguments)
        {
            if (argument.Parameter?.Name != "configure" && argument.Syntax is ArgumentSyntax syntax)
                yield return syntax;
        }
    }
}
