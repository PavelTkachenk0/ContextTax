using System.ComponentModel;
using ContextTax.Cli.Rendering;
using ContextTax.Cli.Support;
using ContextTax.Core.Mcp;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ContextTax.Cli.Commands;

public sealed class ServersCommand : Command<ServersCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--config <PATH>")]
        [Description("MCP config file to read (default: ./.mcp.json then ~/.claude.json).")]
        public string? ConfigPath { get; set; }

        [CommandOption("--json")]
        [Description("Emit JSON instead of a table.")]
        public bool Json { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        IReadOnlyList<McpServerConfig> servers;
        try
        {
            servers = McpConfig.Resolver(settings.ConfigPath).List();
        }
        catch (McpConfigException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 2;
        }

        if (settings.Json)
            Console.WriteLine(ServersRenderer.RenderJson(servers));
        else
            ServersRenderer.RenderTable(servers, AnsiConsole.Console);

        return 0;
    }
}
