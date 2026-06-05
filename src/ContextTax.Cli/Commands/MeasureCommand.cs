using System.ComponentModel;
using ContextTax.Cli.Rendering;
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
        public string ToolsPath { get; set; } = string.Empty;

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

        if (string.IsNullOrWhiteSpace(settings.ToolsPath))
        {
            await Console.Error.WriteLineAsync("error: --tools <path> is required.").ConfigureAwait(false);
            return 2;
        }

        IReadOnlyList<McpTool> tools;
        try
        {
            tools = ToolsJsonLoader.LoadFile(settings.ToolsPath);
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
            InputPricePerMTokUsd = settings.Price,
        };

        var measurer = new SchemaCostMeasurer(counter);

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
        {
            Console.WriteLine(ReportRenderer.RenderJson(report));
        }
        else
        {
            var title = Path.GetFileNameWithoutExtension(settings.ToolsPath).Replace(".tools", "", StringComparison.Ordinal);
            ReportRenderer.RenderCard(report, AnsiConsole.Console, title);
        }

        return 0;
    }
}
