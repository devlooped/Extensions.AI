using Devlooped.Sponsors;
using DotNetConfig;
using Spectre.Console.Cli;

namespace Devlooped.Extensions.AI;

static class SponsorsAppExtensions
{
    public static ICommandApp UseSponsors(this ICommandApp app)
    {
        app.Configure(config =>
        {
            config.AddBranch("sponsor", group =>
            {
                group.AddCommand<CheckCommand>("check");
                group.AddCommand<ConfigCommand>("config");
                group.AddCommand<DevloopedViewCommand>("view");
                group.AddCommand<DevloopedSyncCommand>("sync");
                group.AddCommand<WelcomeCommand>("welcome").IsHidden();
            });
        });
        return app;
    }

    public static bool ShouldRunWelcome(this CommandContext context, Config config, ToSSettings settings)
    {
        // If we don't have ToS acceptance, we don't run any command other than welcome.
        var tos = config.TryGetBoolean("sponsorlink", "tos", out var completed) && completed;
        if (!tos && settings.ToS == true)
        {
            // Implicit acceptance on first run of another tool, like `sponsor sync --tos`
            new ConfigCommand(config).Execute(context, new ConfigCommand.ConfigSettings { ToS = true, Quiet = true });
            return false;
        }

        return tos == false;
    }
}