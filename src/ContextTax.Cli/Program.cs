using ContextTax.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("contexttax");
    config.AddCommand<MeasureCommand>("measure")
        .WithDescription("Measure the static schema-token cost of an MCP server's tools.");
    config.AddCommand<SessionCommand>("session")
        .WithDescription("Measure response bloat + lifecycle cost across a recorded transcript.");
    config.AddCommand<ServersCommand>("servers")
        .WithDescription("List MCP servers discovered in your config (no connection).");
});

return await app.RunAsync(args).ConfigureAwait(false);
