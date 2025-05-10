using System.ComponentModel;
using DotNetConfig;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using static Devlooped.Sponsors.CheckCommand;
using static ThisAssembly.Strings;

namespace Devlooped.Sponsors;

[Description("Checks the current sponsorship status with [lime]devlooped[/], entirely offline")]
[Service]
public class CheckCommand(Config config, Lazy<DevloopedSyncCommand> sync, IAnsiConsole console) : AsyncCommand<CheckSettings>
{
    public class CheckSettings : CommandSettings
    {
        [CommandOption("-q|--quiet", IsHidden = true)]
        public bool Quiet { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CheckSettings settings)
    {
        // Don't render anything if not interactive, so we don't disrupt usage in CI for example.
        // In GH actions, console input/output is redirected, for example, and output is redirected 
        // when using the app in a powershell pipeline or capturing its output in a variable.
        if (App.IsNonInteractive)
            return 0;

        // Check if we have a manifest at all
        var jwtPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".sponsorlink", "github", "devlooped.jwt");

        var url = ThisAssembly.Git.Url;
        if (url.EndsWith(".git"))
            url = url[0..^4];
        var project = $"[blueviolet][link={url}]{ThisAssembly.Info.Product}[/][/]";
        var link = "[link=https://github.com/devlooped/#sponsorlink]devlooped[/]";

        if (!System.IO.File.Exists(jwtPath))
            return MarkupLine(Unknown.Message(project, link));

        var manifest = SponsorLink.GetManifest("devlooped", ThisAssembly.Metadata.Funding.GitHub.devlooped, true);
        if (manifest.Status == ManifestStatus.Valid)
            return 0;

        // If not valid and we can auto-sync, do it now.
        if (config.GetBoolean("sponsorlink", "autosync") == true)
        {
            await sync.Value.ExecuteAsync(context, new() { Unattended = settings.Quiet });
            manifest = SponsorLink.GetManifest("devlooped", ThisAssembly.Metadata.Funding.GitHub.devlooped, true);
            if (manifest.Status == ManifestStatus.Valid)
                return 0;
        }

        if (manifest.Status == ManifestStatus.Unknown || manifest.Status == ManifestStatus.Invalid)
            return MarkupLine(Unknown.Message(project, link));

        if (manifest.Status == ManifestStatus.Expired)
            return MarkupLine(Expired.Message);

        if (settings.Quiet)
            return 0;

        if (manifest.Principal.IsInRole("team"))
            return MarkupLine(Team.Message(link));

        if (manifest.Principal.IsInRole("user"))
            return MarkupLine(ThisAssembly.Strings.Sponsor.Message(project));

        if (manifest.Principal.IsInRole("contrib"))
            return MarkupLine(Contributor.Message(link));

        if (manifest.Principal.IsInRole("org"))
            return MarkupLine(ThisAssembly.Strings.Sponsor.Message(project));

        if (manifest.Principal.IsInRole("oss"))
            return MarkupLine(ThisAssembly.Strings.OpenSource.Message);

        return MarkupLine(Unknown.Message(project, link));
    }

    int MarkupLine(string message)
    {
        console.WriteLine();
        console.MarkupLine(message);
        console.WriteLine();
        return 0;
    }
}
