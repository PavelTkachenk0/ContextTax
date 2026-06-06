using System.Text.Json;
using System.Text.Json.Serialization;
using ContextTax.Core.Mcp;
using Spectre.Console;

namespace ContextTax.Cli.Rendering;

public static class ServersRenderer
{
    private sealed record ServerView(string Name, McpTransport Transport, string Source, IReadOnlyList<string> HeaderKeys);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    // Header VALUES are never emitted — only key names.
    private static ServerView View(McpServerConfig c) =>
        new(c.Name, c.Transport, c.Source, c.Headers?.Keys.ToArray() ?? []);

    public static string RenderJson(IReadOnlyList<McpServerConfig> servers) =>
        JsonSerializer.Serialize(servers.Select(View).ToArray(), JsonOptions);

    public static void RenderTable(IReadOnlyList<McpServerConfig> servers, IAnsiConsole console)
    {
        console.MarkupLine($"[bold]ContextTax[/] · MCP servers · [green]{servers.Count}[/] found");
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Name");
        table.AddColumn("Transport");
        table.AddColumn("Headers");
        table.AddColumn("Source");
        foreach (var s in servers)
        {
            var v = View(s);
            table.AddRow(
                Markup.Escape(v.Name),
                Markup.Escape(v.Transport.ToString().ToLowerInvariant()),
                Markup.Escape(v.HeaderKeys.Count > 0 ? string.Join(", ", v.HeaderKeys) : "—"),
                Markup.Escape(v.Source));
        }
        console.Write(table);
    }
}
