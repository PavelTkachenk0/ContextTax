using System.Globalization;
using ContextTax.Cli.Commands;
using Spectre.Console.Cli;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var app = new CommandApp();
app.SetDefaultCommand<InteractiveCommand>();
app.Configure(config =>
{
    config.SetApplicationName("contexttax");
    config.AddExample("measure", "-s", "everything", "-e");
    config.AddExample("measure", "-t", "./fs.tools.json", "-e");
    config.AddExample("session", "-f", "./run.json", "-t", "./fs.tools.json", "-e");
    config.AddExample("servers");
    config.AddCommand<InteractiveCommand>("interactive")
        .WithDescription("Launch the interactive menu (also the default when run with no arguments).");
    config.AddCommand<MeasureCommand>("measure")
        .WithDescription("Measure the static schema-token cost of an MCP server's tools.");
    config.AddCommand<SessionCommand>("session")
        .WithDescription("Measure response bloat + lifecycle cost across a recorded transcript.");
    config.AddCommand<ServersCommand>("servers")
        .WithDescription("List MCP servers discovered in your config (no connection).");
});

return await app.RunAsync(args).ConfigureAwait(false);
