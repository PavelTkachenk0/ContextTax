using ContextTax.Cli.Interactive;
using ContextTax.Cli.Rendering;
using ContextTax.Cli.Support;
using ContextTax.Core.Measurement;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ContextTax.Cli.Commands;

/// <summary>The interactive menu-loop — the default command when run with no arguments. Reuses the
/// same <see cref="MeasurementRunner"/> the flag commands use; each action returns to the menu, and
/// a failed or cancelled action never crashes the app. Any prompt can be cancelled back to the menu
/// (selections offer "← Back"; text inputs accept a blank entry).</summary>
public sealed class InteractiveCommand : AsyncCommand
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var console = AnsiConsole.Console;
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[bold]ContextTax[/]").LeftJustified());
            AnsiConsole.MarkupLine("[grey]Measure the context-window tax of MCP servers[/]");
            AnsiConsole.WriteLine();

            var action = InteractivePrompts.ChooseAction(console);
            if (action == InteractivePrompts.MainAction.Quit)
                return 0;

            try
            {
                switch (action)
                {
                    case InteractivePrompts.MainAction.Measure:
                        await RunMeasureAsync(console).ConfigureAwait(false);
                        break;
                    case InteractivePrompts.MainAction.Session:
                        await RunSessionAsync(console).ConfigureAwait(false);
                        break;
                    case InteractivePrompts.MainAction.Servers:
                        ServersRenderer.RenderTable(McpConfig.Resolver(null).List(), console);
                        break;
                }
            }
#pragma warning disable CA1031 // A single failed action (e.g. a malformed MCP config) must never crash the menu loop — show it and return to the menu.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                console.MarkupLine($"[red]error:[/] {Markup.Escape(ex.Message)}");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Press Enter to return to the menu…[/]");
            Console.ReadLine();
        }
    }

    private static async Task RunMeasureAsync(IAnsiConsole console)
    {
        var source = InteractivePrompts.ChooseToolSource(console, configPath: null);
        if (source is null)
            return;

        var estimate = InteractivePrompts.ChooseEstimate(console);
        if (estimate is null)
            return;

        using var cf = CounterFactory.Create(estimate.Value, Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
        if (cf.Counter is null)
        {
            console.MarkupLine($"[yellow]{Markup.Escape(cf.Error!)}[/]");
            return;
        }

        var options = new MeasurementOptions { Model = Defaults.Model };
        var runner = MeasurementRunner.Default(Timeout);
        var result = await AnsiConsole.Status().StartAsync("measuring…",
            async _ => await runner.RunSchemaAsync(source, options, cf.Counter).ConfigureAwait(false))
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            console.MarkupLine($"[red]error:[/] {Markup.Escape(result.ErrorMessage!)}");
            return;
        }

        ReportRenderer.RenderCard(result.Report!, console, source.ServerName ?? source.Url ?? "tools");
    }

    private static async Task RunSessionAsync(IAnsiConsole console)
    {
        console.MarkupLine("[grey]A recorded Anthropic transcript — assistant tool_use + user tool_result messages.[/]");
        var transcript = console.Prompt(new TextPrompt<string>("Transcript JSON path [grey](blank to cancel)[/]:").AllowEmpty());
        if (string.IsNullOrWhiteSpace(transcript))
            return;

        ToolSourceOptions source;
        if (console.Confirm("Attach external tools (else use the transcript's own)?", defaultValue: false))
        {
            var chosen = InteractivePrompts.ChooseToolSource(console, configPath: null);
            if (chosen is null)
                return;
            source = chosen;
        }
        else
        {
            source = new ToolSourceOptions();
        }

        var estimate = InteractivePrompts.ChooseEstimate(console);
        if (estimate is null)
            return;

        using var cf = CounterFactory.Create(estimate.Value, Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
        if (cf.Counter is null)
        {
            console.MarkupLine($"[yellow]{Markup.Escape(cf.Error!)}[/]");
            return;
        }

        var options = new MeasurementOptions { Model = Defaults.Model };
        var runner = MeasurementRunner.Default(Timeout);
        var result = await AnsiConsole.Status().StartAsync("measuring…",
            async _ => await runner.RunSessionAsync(transcript, source, options, cf.Counter).ConfigureAwait(false))
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            console.MarkupLine($"[red]error:[/] {Markup.Escape(result.ErrorMessage!)}");
            return;
        }

        SessionReportRenderer.RenderCard(result.Report!, console, Path.GetFileNameWithoutExtension(transcript));
    }
}
