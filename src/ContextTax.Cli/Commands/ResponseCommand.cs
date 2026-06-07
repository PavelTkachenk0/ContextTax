using System.ComponentModel;
using ContextTax.Cli.Rendering;
using ContextTax.Cli.Support;
using ContextTax.Core.Measurement;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ContextTax.Cli.Commands;

public sealed class ResponseCommand : AsyncCommand<ResponseCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[PATH]")]
        [Description("Captured tool response file (JSON or text), e.g. ./response.json. Omit to read from a pipe (stdin).")]
        public string ResponsePath { get; set; } = string.Empty;

        [CommandOption("-d|--delta <PATH>")]
        [Description("Second (optimised) response file to diff against, e.g. ./after.json.")]
        public string? Delta { get; set; }

        [CommandOption("-m|--model <ID>")]
        [Description("Model id for count_tokens, e.g. claude-sonnet-4-5.")]
        public string Model { get; set; } = Defaults.Model;

        [CommandOption("-w|--window <TOKENS>")]
        [Description("Context window for the % metric, e.g. 200000.")]
        public int Window { get; set; } = Defaults.ContextWindowTokens;

        [CommandOption("-j|--json")]
        [Description("Emit the report as JSON instead of a card.")]
        public bool Json { get; set; }

        [CommandOption("-e|--estimate")]
        [Description("Approximate offline with o200k_base (no API key). Counts labelled ≈, not ground-truth.")]
        public bool Estimate { get; set; }

        [CommandOption("-C|--clipboard")]
        [Description("Measure the text currently on the clipboard (macOS) instead of a file. Single response only.")]
        public bool Clipboard { get; set; }
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

        using var counterFactory = CounterFactory.Create(settings.Estimate, Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
        if (counterFactory.Counter is null)
        {
            await Console.Error.WriteLineAsync($"error: {counterFactory.Error}").ConfigureAwait(false);
            return 2;
        }

        var options = new MeasurementOptions { Model = settings.Model, ContextWindowTokens = settings.Window };

        // The timeout is inert for `response` (no live MCP connection is made — the runner resolves no
        // tool source on this path); a value is only needed to construct the shared runner.
        var runner = MeasurementRunner.Default(TimeSpan.FromSeconds(30));

        if (settings.Clipboard)
        {
            if (!string.IsNullOrEmpty(settings.Delta))
            {
                await Console.Error.WriteLineAsync("error: a diff compares two files; --clipboard measures a single response.").ConfigureAwait(false);
                return 2;
            }

            var (clip, clipError) = ClipboardReader.Read();
            if (clip is null)
            {
                await Console.Error.WriteLineAsync($"error: {clipError}").ConfigureAwait(false);
                return 2;
            }

            var clipResult = await runner.RunResponseTextAsync("(clipboard)", clip, options, counterFactory.Counter).ConfigureAwait(false);
            if (!clipResult.IsSuccess)
            {
                await Console.Error.WriteLineAsync($"error: {clipResult.ErrorMessage}").ConfigureAwait(false);
                return clipResult.ExitCode;
            }

            if (settings.Json)
                Console.WriteLine(ResponseReportRenderer.RenderJson(clipResult.Report!));
            else
                ResponseReportRenderer.RenderCard(clipResult.Report!, AnsiConsole.Console);
            return 0;
        }

        // Resolve the primary input: an explicit file path, or — when omitted and stdin is piped —
        // the captured response from stdin. (Spectre rejects a literal "-" positional, so we detect a
        // redirected stdin instead; the runner still reads stdin via its "-" path.)
        var inputPath = settings.ResponsePath;
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            if (!Console.IsInputRedirected)
            {
                await Console.Error.WriteLineAsync(
                    "error: a response <path> is required, or pipe one via stdin "
                    + "(e.g. cat resp.json | contexttax response --estimate), or use --clipboard.").ConfigureAwait(false);
                return 2;
            }

            inputPath = "-";
        }

        if (!string.IsNullOrEmpty(settings.Delta))
        {
            var diff = await runner.RunResponseDeltaAsync(inputPath, settings.Delta, options, counterFactory.Counter).ConfigureAwait(false);
            if (!diff.IsSuccess)
            {
                await Console.Error.WriteLineAsync($"error: {diff.ErrorMessage}").ConfigureAwait(false);
                return diff.ExitCode;
            }

            if (settings.Json)
                Console.WriteLine(ResponseReportRenderer.RenderJson(diff.Report!));
            else
                ResponseReportRenderer.RenderDiffCard(diff.Report!, AnsiConsole.Console);
            return 0;
        }

        var result = await runner.RunResponseAsync(inputPath, options, counterFactory.Counter).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            await Console.Error.WriteLineAsync($"error: {result.ErrorMessage}").ConfigureAwait(false);
            return result.ExitCode;
        }

        if (settings.Json)
            Console.WriteLine(ResponseReportRenderer.RenderJson(result.Report!));
        else
            ResponseReportRenderer.RenderCard(result.Report!, AnsiConsole.Console);
        return 0;
    }
}
