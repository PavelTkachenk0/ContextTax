using ContextTax.Cli.Support;
using ContextTax.Core.Mcp;
using Spectre.Console;

namespace ContextTax.Cli.Interactive;

/// <summary>Spectre prompt helpers for the interactive mode. Pure UI glue — gathers the user's
/// choices into a <see cref="ToolSourceOptions"/> / flags; performs no measurement. Header values
/// are read masked and never echoed. Every prompt is cancellable back to the menu: selections
/// offer "← Back", text inputs treat a blank entry as cancel — so a returned <c>null</c> means
/// "go back".</summary>
public static class InteractivePrompts
{
    private const string Back = "← Back";

    public enum MainAction
    {
        Measure,
        Session,
        Servers,
        Quit,
    }

    public static MainAction ChooseAction(IAnsiConsole console) =>
        console.Prompt(new SelectionPrompt<MainAction>()
            .Title("What do you want to do?")
            .UseConverter(a => a switch
            {
                MainAction.Measure => "Measure a server / tools   (schema cost)",
                MainAction.Session => "Analyze a recorded session (response bloat)",
                MainAction.Servers => "List configured servers",
                MainAction.Quit => "Quit",
                _ => a.ToString(),
            })
            .AddChoices(MainAction.Measure, MainAction.Session, MainAction.Servers, MainAction.Quit));

    /// <summary>Returns the chosen source, or <c>null</c> to go back to the menu.</summary>
    public static ToolSourceOptions? ChooseToolSource(IAnsiConsole console, string? configPath)
    {
        var kind = console.Prompt(new SelectionPrompt<string>()
            .Title("Tool source?")
            .AddChoices("A configured server", "An MCP URL", "A tools JSON file", Back));
        if (kind == Back)
            return null;

        if (kind == "A configured server")
        {
            var servers = McpConfig.Resolver(configPath).List();
            if (servers.Count == 0)
            {
                console.MarkupLine("[yellow]No configured servers found — enter a URL or file instead.[/]");
                return PromptUrlOrFile(console);
            }

            var byLabel = new Dictionary<string, McpServerConfig>(StringComparer.Ordinal);
            foreach (var s in servers)
                byLabel[$"{s.Name}  ({s.Transport.ToString().ToLowerInvariant()} · {s.Source})"] = s;

            var labels = byLabel.Keys.ToList();
            labels.Add(Back);
            var picked = console.Prompt(new SelectionPrompt<string>().Title("Which server?").AddChoices(labels));
            return picked == Back ? null : new ToolSourceOptions { ServerName = byLabel[picked].Name, ConfigPath = configPath };
        }

        return kind == "An MCP URL" ? PromptUrl(console) : PromptFile(console);
    }

    /// <summary>Returns true for estimate, false for ground-truth, or <c>null</c> to go back.</summary>
    public static bool? ChooseEstimate(IAnsiConsole console)
    {
        var mode = console.Prompt(new SelectionPrompt<string>()
            .Title("Counting mode?")
            .AddChoices("Estimate (offline, keyless)", "Ground truth (needs ANTHROPIC_API_KEY)", Back));
        if (mode == Back)
            return null;
        return mode.StartsWith("Estimate", StringComparison.Ordinal);
    }

    private static ToolSourceOptions? PromptUrlOrFile(IAnsiConsole console)
    {
        var kind = console.Prompt(new SelectionPrompt<string>()
            .Title("Source?")
            .AddChoices("An MCP URL", "A tools JSON file", Back));
        if (kind == Back)
            return null;
        return kind == "An MCP URL" ? PromptUrl(console) : PromptFile(console);
    }

    private static ToolSourceOptions? PromptUrl(IAnsiConsole console)
    {
        console.MarkupLine("[grey]An MCP server's HTTP endpoint, e.g. https://example.com/mcp[/]");
        var url = console.Prompt(new TextPrompt<string>("MCP URL [grey](blank to cancel)[/]:").AllowEmpty());
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var headers = new Dictionary<string, string>(StringComparer.Ordinal);
        while (console.Confirm("Add a header?", defaultValue: false))
        {
            var key = console.Prompt(new TextPrompt<string>("  header key:"));
            var value = console.Prompt(new TextPrompt<string>("  header value:").Secret());
            headers[key] = value;
        }
        return new ToolSourceOptions { Url = url, Headers = headers.Count > 0 ? headers : null };
    }

    private static ToolSourceOptions? PromptFile(IAnsiConsole console)
    {
        // No existence validation here — a missing path flows to the runner, which reports
        // "file not found" once and returns to the menu (instead of trapping in a re-prompt loop).
        console.MarkupLine("[grey]An MCP tools/list JSON document, or a bare array of tool definitions.[/]");
        var path = console.Prompt(new TextPrompt<string>("Tools JSON path [grey](blank to cancel)[/]:").AllowEmpty());
        return string.IsNullOrWhiteSpace(path) ? null : new ToolSourceOptions { ToolsPath = path };
    }
}
