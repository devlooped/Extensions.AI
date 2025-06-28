using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Devlooped.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

// Some users reported not getting emoji on Windows, so we force UTF-8 encoding.
// This not great, but I couldn't find a better way to do it.
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    Console.InputEncoding = Console.OutputEncoding = Encoding.UTF8;

args = [.. ExpandResponseFiles(args)];

// Alias -? to -h for help
if (args.Contains("-?"))
    args = [.. args.Select(x => x == "-?" ? "-h" : x)];

if (args.Contains("--debug"))
{
    Debugger.Launch();
    args = [.. args.Where(x => x != "--debug")];
}

var app = App.Create(out var registrar);

#if DEBUG
app.Configure(c => c.PropagateExceptions());
#else
if (args.Contains("--exceptions"))
{
    app.Configure(c => c.PropagateExceptions());
    args = [.. args.Where(x => x != "--exceptions")];
}
#endif

app.Configure(config => config.SetApplicationName(ThisAssembly.Project.ToolCommandName));

if (args.Contains("--version"))
{
    app.ShowVersion();
    await app.ShowUpdatesAsync(args);
    return 0;
}

var result = 0;

#if DEBUG
result = await app.RunAsync(args);
#else
result = await app.RunWithUpdatesAsync(args);
#endif

// --quiet does not report sponsor tier on every run.
await ((IServiceProvider)registrar).GetRequiredService<CheckCommand>().ExecuteAsync(
    new CommandContext(["--quiet"], RemainingArguments.Empty, "check", null),
    new CheckCommand.CheckSettings { Quiet = true });

return result;

static IEnumerable<string> ExpandResponseFiles(IEnumerable<string> args)
{
    foreach (var arg in args)
    {
        if (arg.StartsWith('@'))
        {
            var filePath = arg[1..];

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Response file not found: {filePath}");

            foreach (var line in File.ReadAllLines(filePath))
            {
                yield return line;
            }
        }
        else
        {
            yield return arg;
        }
    }
}

class RemainingArguments : IRemainingArguments
{
    public static IRemainingArguments Empty { get; } = new RemainingArguments();

    RemainingArguments() { }

    public ILookup<string, string?> Parsed => Enumerable.Empty<string>().ToLookup(x => x, x => default(string));

    public IReadOnlyList<string> Raw => [];
}