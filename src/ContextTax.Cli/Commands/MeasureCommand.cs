using System.ComponentModel;
using ContextTax.Cli.Rendering;
using ContextTax.Cli.Support;
using ContextTax.Core.Measurement;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ContextTax.Cli.Commands;

public sealed class MeasureCommand : AsyncCommand<MeasureCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-t|--tools <PATH>")]
        [Description("Path to a tools JSON file (MCP tools/list shape or a bare array), e.g. ./fs.tools.json.")]
        public string? ToolsPath { get; set; }

        [CommandOption("-s|--server <NAME>")]
        [Description("Live MCP server name from your config, e.g. everything (alternative to --tools).")]
        public string? Server { get; set; }

        [CommandOption("-u|--url <URL>")]
        [Description("Live MCP server HTTP endpoint, e.g. https://host/mcp (alternative to --tools).")]
        public string? Url { get; set; }

        [CommandOption("-H|--header <HEADER>")]
        [Description("HTTP header for --url, e.g. \"Authorization: Bearer abc\" (repeatable).")]
        public string[] Headers { get; set; } = [];

        [CommandOption("-c|--config <PATH>")]
        [Description("MCP config file for --server (default: ./.mcp.json then ~/.claude.json).")]
        public string? ConfigPath { get; set; }

        [CommandOption("--timeout <SECONDS>")]
        [Description("Connection timeout for --server/--url in seconds, e.g. 30.")]
        public int TimeoutSeconds { get; set; } = 30;

        [CommandOption("-m|--model <ID>")]
        [Description("Model id for count_tokens, e.g. claude-sonnet-4-5.")]
        public string Model { get; set; } = Defaults.Model;

        [CommandOption("-w|--window <TOKENS>")]
        [Description("Context window for the % metric, e.g. 200000.")]
        public int Window { get; set; } = Defaults.ContextWindowTokens;

        [CommandOption("-j|--json")]
        [Description("Emit the report as JSON instead of a table.")]
        public bool Json { get; set; }

        [CommandOption("-p|--price <USD_PER_MTOK>")]
        [Description("Input price per million tokens in USD for the $ cost, e.g. 3.0.")]
        public double Price { get; set; } = Defaults.InputPricePerMTokUsd;

        [CommandOption("-e|--estimate")]
        [Description("Approximate offline with o200k_base (no API key). Counts labelled ≈, not ground-truth.")]
        public bool Estimate { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (context is null)
        {
            await Console.Error.WriteLineAsync("error: command context is null.").ConfigureAwait(false);
            return 2;
        }

        if (settings is null)
        {
            await Console.Error.WriteLineAsync("error: settings are null.").ConfigureAwait(false);
            return 2;
        }

        ToolSourceOptions source;
        try
        {
            source = ToolSourceResolver.OptionsFrom(
                settings.ToolsPath, settings.Server, settings.Url, settings.Headers, settings.ConfigPath);
        }
        catch (ToolSourceException ex)
        {
            await Console.Error.WriteLineAsync($"error: {ex.Message}").ConfigureAwait(false);
            return ex.ExitCode;
        }

        using var counterFactory = CounterFactory.Create(settings.Estimate, Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
        if (counterFactory.Counter is null)
        {
            await Console.Error.WriteLineAsync($"error: {counterFactory.Error}").ConfigureAwait(false);
            return 2;
        }

        var options = new MeasurementOptions
        {
            Model = settings.Model,
            ContextWindowTokens = settings.Window,
            InputPricePerMTokUsd = settings.Price,
        };

        var runner = MeasurementRunner.Default(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        var result = await runner.RunSchemaAsync(source, options, counterFactory.Counter).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            await Console.Error.WriteLineAsync($"error: {result.ErrorMessage}").ConfigureAwait(false);
            return result.ExitCode;
        }

        if (settings.Json)
            Console.WriteLine(ReportRenderer.RenderJson(result.Report!));
        else
            ReportRenderer.RenderCard(result.Report!, AnsiConsole.Console, Title(settings));

        return 0;
    }

    private static string Title(Settings s)
    {
        if (!string.IsNullOrWhiteSpace(s.Server))
            return s.Server;
        if (!string.IsNullOrWhiteSpace(s.ToolsPath))
            return Path.GetFileNameWithoutExtension(s.ToolsPath).Replace(".tools", "", StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(s.Url) ? "server" : ToolSourceResolver.DisplayName(s.Url);
    }
}
