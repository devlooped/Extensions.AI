using System.ComponentModel;
using Devlooped.Sponsors;
using DotNetConfig;
using Spectre.Console.Cli;

namespace Devlooped.Extensions.AI;

[Description("Validates and displays your sponsor manifest for [lime]devlooped[/], if present")]
class DevloopedViewCommand(Config config, IHttpClientFactory http) : ViewCommand<DevloopedViewCommand.DevloopedViewSettings>(http)
{
    public class DevloopedViewSettings : ViewSettings, ISponsorableSettings
    {
        public string[]? Sponsorable { get; set; } = ["devlooped"];
    }

    public override async Task<int> ExecuteAsync(CommandContext context, DevloopedViewSettings settings)
    {
        if (context.ShouldRunWelcome(config, settings))
        {
            if (new WelcomeCommand(config).Execute(context, new WelcomeCommand.WelcomeSettings { ToS = settings.ToS }) is var result && result != 0)
                return result;
        }

        settings.Sponsorable = ["devlooped"];
        return await base.ExecuteAsync(context, settings);
    }
}
