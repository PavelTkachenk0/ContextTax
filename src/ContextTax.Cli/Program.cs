using ContextTax.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("contexttax");
    config.AddCommand<MeasureCommand>("measure")
        .WithDescription("Measure the static schema-token cost of an MCP server's tools.");
});

return await app.RunAsync(args).ConfigureAwait(false);
