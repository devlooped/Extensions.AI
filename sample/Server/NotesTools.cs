using Microsoft.Agents.AI;
using ModelContextProtocol.Server;

public class NotesContextProvider(NotesTools notes) : AIContextProvider
{
    public override ValueTask<AIContext> InvokingAsync(InvokingContext context, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new AIContext
        {
            Instructions =
                $"""
                Your current state is: 
                <notes>
                ${notes.GetNotes()}
                </notes>
                """
        });
}

[McpServerToolType]
public class NotesTools
{
    string notes = "";

    [McpServerTool]
    public string GetNotes() => notes;

    [McpServerTool]
    public void SaveNotes(string notes) => this.notes = notes;
}
