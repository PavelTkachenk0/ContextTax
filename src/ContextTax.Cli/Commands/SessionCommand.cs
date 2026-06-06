using System.ComponentModel;
using ContextTax.Cli.Rendering;
using ContextTax.Cli.Support;
using ContextTax.Core.Counting;
using ContextTax.Core.Mcp;
using ContextTax.Core.Measurement;
using ContextTax.Core.Transcript;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ContextTax.Cli.Commands;

public sealed class SessionCommand : AsyncCommand<SessionCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--transcript <PATH>")]
        [Description("Path to a transcript JSON file (Anthropic messages with tool_use/tool_result).")]
        public string TranscriptPath { get; set; } = string.Empty;

        [CommandOption("--tools <PATH>")]
        [Description("Path to a tools JSON file (used when the transcript has no embedded tools).")]
        public string? ToolsPath { get; set; }

        [CommandOption("--server <NAME>")]
        [Description("Take tools from a live MCP server by name from your MCP config (alternative to --tools).")]
        public string? Server { get; set; }

        [CommandOption("--url <URL>")]
        [Description("Take tools from a live MCP server at an HTTP endpoint (alternative to --tools).")]
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

        if (string.IsNullOrWhiteSpace(settings.TranscriptPath))
        {
            await Console.Error.WriteLineAsync("error: --transcript <path> is required.").ConfigureAwait(false);
            return 2;
        }

        // Tools are optional for session (the transcript may embed them). Resolve only if a source flag is set.
        IReadOnlyList<McpTool>? externalTools = null;
        if (!string.IsNullOrWhiteSpace(settings.ToolsPath)
            || !string.IsNullOrWhiteSpace(settings.Server)
            || !string.IsNullOrWhiteSpace(settings.Url))
        {
            try
            {
                var resolver = ToolSourceResolver.Default(TimeSpan.FromSeconds(settings.TimeoutSeconds));
                externalTools = await resolver.ResolveAsync(ToolSourceResolver.OptionsFrom(
                    settings.ToolsPath, settings.Server, settings.Url, settings.Headers, settings.ConfigPath))
                    .ConfigureAwait(false);
            }
            catch (ToolSourceException ex)
            {
                await Console.Error.WriteLineAsync($"error: {ex.Message}").ConfigureAwait(false);
                return ex.ExitCode;
            }
        }

        SessionTranscript transcript;
        try
        {
            transcript = TranscriptLoader.LoadFile(settings.TranscriptPath, externalTools);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            await Console.Error.WriteLineAsync($"error: file not found: {settings.TranscriptPath}").ConfigureAwait(false);
            return 2;
        }
        catch (TranscriptException ex)
        {
            await Console.Error.WriteLineAsync($"error: {ex.Message}").ConfigureAwait(false);
            return 2;
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

        var measurer = new SessionCostMeasurer(counterFactory.Counter);

        SessionCostReport report;
        try
        {
            report = await measurer.MeasureAsync(transcript, options).ConfigureAwait(false);
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
        {
            Console.WriteLine(SessionReportRenderer.RenderJson(report));
        }
        else
        {
            var title = Path.GetFileNameWithoutExtension(settings.TranscriptPath).Replace(".session", "", StringComparison.Ordinal);
            SessionReportRenderer.RenderCard(report, AnsiConsole.Console, title);
        }

        return 0;
    }
}
