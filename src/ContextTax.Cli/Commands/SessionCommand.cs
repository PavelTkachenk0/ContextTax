using System.ComponentModel;
using ContextTax.Cli.Rendering;
using ContextTax.Cli.Support;
using ContextTax.Core.Measurement;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ContextTax.Cli.Commands;

public sealed class SessionCommand : AsyncCommand<SessionCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-f|--transcript <PATH>")]
        [Description("Transcript JSON (Anthropic messages with tool_use/tool_result), e.g. ./run.json.")]
        public string TranscriptPath { get; set; } = string.Empty;

        [CommandOption("-t|--tools <PATH>")]
        [Description("Tools JSON, used when the transcript has no embedded tools, e.g. ./fs.tools.json.")]
        public string? ToolsPath { get; set; }

        [CommandOption("-s|--server <NAME>")]
        [Description("Live MCP server name from your config (alternative to --tools).")]
        public string? Server { get; set; }

        [CommandOption("-u|--url <URL>")]
        [Description("Live MCP server HTTP endpoint (alternative to --tools).")]
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

        if (string.IsNullOrWhiteSpace(settings.TranscriptPath))
        {
            await Console.Error.WriteLineAsync("error: --transcript <path> is required.").ConfigureAwait(false);
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
        };

        var runner = MeasurementRunner.Default(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        var result = await runner.RunSessionAsync(settings.TranscriptPath, source, options, counterFactory.Counter).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            await Console.Error.WriteLineAsync($"error: {result.ErrorMessage}").ConfigureAwait(false);
            return result.ExitCode;
        }

        if (settings.Json)
        {
            Console.WriteLine(SessionReportRenderer.RenderJson(result.Report!));
        }
        else
        {
            var title = Path.GetFileNameWithoutExtension(settings.TranscriptPath).Replace(".session", "", StringComparison.Ordinal);
            SessionReportRenderer.RenderCard(result.Report!, AnsiConsole.Console, title);
        }

        return 0;
    }
}
