namespace Devlooped.Sponsors;

public static class App
{
    /// <summary>
    /// Whether the CLI app is not interactive (i.e. part of a script run, 
    /// running in CI, or in a non-interactive user session).
    /// </summary>
    public static bool IsNonInteractive => !Environment.UserInteractive
        || Console.IsInputRedirected
        || Console.IsOutputRedirected;
}
