namespace Devlooped.Extensions.AI;

static class ConsoleExtensions
{
    public static bool IsConsoleInteractive =>
        !Console.IsInputRedirected &&
        !Console.IsOutputRedirected &&
        !Console.IsErrorRedirected &&
        Environment.UserInteractive;
}
