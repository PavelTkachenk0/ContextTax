using System.Globalization;
using ContextTax.Cli.Commands;
using ContextTax.Cli.Support;
using Spectre.Console.Cli;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

// Spectre.Console.Cli routes the built-in -v/--version to the default command when
// SetDefaultCommand is used (so the advertised flag never prints). Handle it up front.
if (args is ["-v"] or ["--version"])
{
    Console.WriteLine(VersionInfo.Current);
    return 0;
}

var app = new CommandApp();
app.SetDefaultCommand<InteractiveCommand>();
app.Configure(config =>
{
    config.SetApplicationName("contexttax");
    config.SetApplicationVersion(VersionInfo.Current);
    config.AddExample("measure", "-s", "everything", "-e");
    config.AddExample("measure", "-t", "./fs.tools.json", "-e");
    config.AddExample("session", "-f", "./run.json", "-t", "./fs.tools.json", "-e");
    config.AddExample("response", "./response.json", "-e");
    config.AddExample("response", "./before.json", "-d", "./after.json", "-e");
    config.AddExample("servers");
    config.AddCommand<InteractiveCommand>("interactive")
        .WithDescription("Launch the interactive menu (also the default when run with no arguments).");
    config.AddCommand<MeasureCommand>("measure")
        .WithDescription("Measure the static schema-token cost of an MCP server's tools.");
    config.AddCommand<SessionCommand>("session")
        .WithDescription("Measure response bloat + lifecycle cost across a recorded transcript.");
    config.AddCommand<ResponseCommand>("response")
        .WithDescription("Measure a captured tool response's token cost (and diff before/after optimisation).");
    config.AddCommand<ServersCommand>("servers")
        .WithDescription("List MCP servers discovered in your config (no connection).");
});

return await app.RunAsync(args).ConfigureAwait(false);
