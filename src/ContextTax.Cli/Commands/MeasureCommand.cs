using System.ComponentModel;
using ContextTax.Cli.Rendering;
using ContextTax.Cli.Support;
using ContextTax.Core.Counting;
using ContextTax.Core.Mcp;
using ContextTax.Core.Measurement;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ContextTax.Cli.Commands;

public sealed class MeasureCommand : AsyncCommand<MeasureCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--tools <PATH>")]
        [Description("Path to a tools JSON file (MCP tools/list shape, or a bare array).")]
        public string? ToolsPath { get; set; }

        [CommandOption("--server <NAME>")]
        [Description("Measure a live MCP server by name from your MCP config (alternative to --tools).")]
        public string? Server { get; set; }

        [CommandOption("--url <URL>")]
        [Description("Measure a live MCP server at an HTTP endpoint (alternative to --tools).")]
        public string? Url { get; set; }

        [CommandOption("--header <HEADER>")]
        [Description("HTTP header for --url, as \"Key: Value\" (repeatable).")]
        public string[] Headers { get; set; } = [];

        [CommandOption("--config <PATH>")]
        [Description("MCP config file to read --server from (default: ./.mcp.json then ~/.claude.json).")]
        public string? ConfigPath { get; set; }

        [CommandOption("--timeout <SECONDS>")]
        [Description("Connection timeout for --server/--url, in seconds.")]
        public int TimeoutSeconds { get; set; } = 30;

        [CommandOption("--model <ID>")]
        [Description("Model id used for count_tokens.")]
        public string Model { get; set; } = Defaults.Model;

        [CommandOption("--window <TOKENS>")]
        [Description("Context window size used for the % metric.")]
        public int Window { get; set; } = Defaults.ContextWindowTokens;

        [CommandOption("--json")]
        [Description("Emit the report as JSON instead of a table.")]
        public bool Json { get; set; }

        [CommandOption("--price <USD_PER_MTOK>")]
        [Description("Input price per million tokens, in USD (used for the $ cost).")]
        public double Price { get; set; } = Defaults.InputPricePerMTokUsd;

        [CommandOption("--estimate")]
        [Description("Approximate the cost offline with the o200k_base tokenizer (no API key). Counts are labelled ≈ and are not ground-truth.")]
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

        IReadOnlyList<McpTool> tools;
        try
        {
            var resolver = ToolSourceResolver.Default(TimeSpan.FromSeconds(settings.TimeoutSeconds));
            tools = await resolver.ResolveAsync(ToolSourceResolver.OptionsFrom(
                settings.ToolsPath, settings.Server, settings.Url, settings.Headers, settings.ConfigPath))
                .ConfigureAwait(false);
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

        var measurer = new SchemaCostMeasurer(counterFactory.Counter);

        SchemaCostReport report;
        try
        {
            report = await measurer.MeasureAsync(tools, options).ConfigureAwait(false);
        }
        catch (TokenCountException ex)
        {
            await Console.Error.WriteLineAsync($"error: {ex.Message}").ConfigureAwait(false);
            return 1;
        }
        catch (HttpRequestException ex)
        {
            await Console.Error.WriteLineAsync($"error: network failure calling Anthropic: {ex.Message}").ConfigureAwait(false);
            return 1;
        }

        if (settings.Json)
            Console.WriteLine(ReportRenderer.RenderJson(report));
        else
            ReportRenderer.RenderCard(report, AnsiConsole.Console, Title(settings));

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
