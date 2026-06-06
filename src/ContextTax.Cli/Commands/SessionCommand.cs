using System.ComponentModel;
using ContextTax.Cli.Rendering;
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

        IReadOnlyList<McpTool>? externalTools = null;
        if (!string.IsNullOrWhiteSpace(settings.ToolsPath))
        {
            try
            {
                externalTools = ToolsJsonLoader.LoadFile(settings.ToolsPath);
            }
            catch (FileNotFoundException)
            {
                await Console.Error.WriteLineAsync($"error: file not found: {settings.ToolsPath}").ConfigureAwait(false);
                return 2;
            }
            catch (DirectoryNotFoundException)
            {
                await Console.Error.WriteLineAsync($"error: file not found: {settings.ToolsPath}").ConfigureAwait(false);
                return 2;
            }
            catch (ToolsJsonException ex)
            {
                await Console.Error.WriteLineAsync($"error: {ex.Message}").ConfigureAwait(false);
                return 2;
            }
        }

        SessionTranscript transcript;
        try
        {
            transcript = TranscriptLoader.LoadFile(settings.TranscriptPath, externalTools);
        }
        catch (FileNotFoundException)
        {
            await Console.Error.WriteLineAsync($"error: file not found: {settings.TranscriptPath}").ConfigureAwait(false);
            return 2;
        }
        catch (DirectoryNotFoundException)
        {
            await Console.Error.WriteLineAsync($"error: file not found: {settings.TranscriptPath}").ConfigureAwait(false);
            return 2;
        }
        catch (TranscriptException ex)
        {
            await Console.Error.WriteLineAsync($"error: {ex.Message}").ConfigureAwait(false);
            return 2;
        }

        using var http = settings.Estimate ? null : new HttpClient();

        ITokenCounter counter;
        if (settings.Estimate)
        {
            counter = EstimateTokenCounter.CreateO200k();
        }
        else
        {
            var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                await Console.Error.WriteLineAsync(
                    "error: ANTHROPIC_API_KEY is not set. Run with --estimate for a keyless approximate count, "
                    + "or set the key (or use 'dotnet user-secrets') for exact ground-truth.").ConfigureAwait(false);
                return 2;
            }

            counter = new AnthropicTokenCounter(new AnthropicCountTokensClient(http!, apiKey));
        }

        var options = new MeasurementOptions
        {
            Model = settings.Model,
            ContextWindowTokens = settings.Window,
        };

        var measurer = new SessionCostMeasurer(counter);

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
