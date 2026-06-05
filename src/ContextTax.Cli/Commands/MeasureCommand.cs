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

        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            await Console.Error.WriteLineAsync(
                "error: ANTHROPIC_API_KEY is not set. Set it (or use 'dotnet user-secrets') to measure.").ConfigureAwait(false);
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

        var options = new MeasurementOptions
        {
            Model = settings.Model,
            ContextWindowTokens = settings.Window,
            InputPricePerMTokUsd = settings.Price,
        };

        using var http = new HttpClient();
        var measurer = new SchemaCostMeasurer(new AnthropicTokenCounter(new AnthropicCountTokensClient(http, apiKey)));

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
