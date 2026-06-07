using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContextTax.Core.Measurement;
using Spectre.Console;

namespace ContextTax.Cli.Rendering;

/// <summary>Renders a response-cost card (single or before/after diff) — sibling of
/// <see cref="ReportRenderer"/> / <see cref="SessionReportRenderer"/>, reusing the same Spectre
/// language (rounded table, mode badge, <see cref="TaxSeverity"/> colour, <see cref="TokenBar"/>),
/// invariant numbers, ASCII minus.</summary>
public static class ResponseReportRenderer
{
    private const string EstimateFooter =
        "[yellow]≈ Approximate[/] — o200k_base tokenizer (a non-Claude proxy), not " +
        "Anthropic count_tokens. Set ANTHROPIC_API_KEY for exact numbers.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string RenderJson(ResponseCostReport report) => JsonSerializer.Serialize(report, JsonOptions);

    public static string RenderJson(ResponseDiffReport report) => JsonSerializer.Serialize(report, JsonOptions);

    public static void RenderCard(ResponseCostReport report, IAnsiConsole console)
    {
        var isEstimate = report.Mode == MeasurementMode.Estimate;
        var badge = isEstimate ? "[yellow]≈ ESTIMATE[/]" : "[green]✓ GROUND TRUTH[/]";
        var approx = isEstimate ? "~" : string.Empty;

        console.MarkupLine($"[bold]ContextTax[/] · response · [green]{Markup.Escape(report.Source)}[/]   {badge}");

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Metric");
        table.AddColumn(new TableColumn("Value").RightAligned());
        table.AddRow("Response tokens", $"{approx}{report.ResponseTokens.ToString("N0", CultureInfo.InvariantCulture)} tok");
        var taxColor = TaxSeverity.Color(report.PercentWindow);
        table.AddRow(
            "Context tax",
            $"[{taxColor}]{approx}{report.PercentWindow.ToString("F1", CultureInfo.InvariantCulture)} %[/] of a {report.ContextWindowTokens.ToString("N0", CultureInfo.InvariantCulture)} window");
        table.AddRow("Counted with", Markup.Escape(isEstimate ? report.CounterLabel : $"{report.CounterLabel} · {report.ModelId}"));
        console.Write(table);

        if (isEstimate)
            console.MarkupLine(EstimateFooter);
    }

    public static void RenderDiffCard(ResponseDiffReport report, IAnsiConsole console)
    {
        var isEstimate = report.Mode == MeasurementMode.Estimate;
        var badge = isEstimate ? "[yellow]≈ ESTIMATE[/]" : "[green]✓ GROUND TRUTH[/]";
        var approx = isEstimate ? "~" : string.Empty;
        var leaner = report.DeltaTokens <= 0;
        var deltaColor = leaner ? "green" : "red";

        console.MarkupLine(
            $"[bold]ContextTax[/] · response diff   {badge}   [grey]{report.ContextWindowTokens.ToString("N0", CultureInfo.InvariantCulture)} window[/]");

        var max = Math.Max(report.Before.ResponseTokens, report.After.ResponseTokens);
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Source");
        table.AddColumn(string.Empty);
        table.AddColumn(new TableColumn("Tokens").RightAligned());
        table.AddColumn(new TableColumn("% window").RightAligned());
        AddSide(table, report.Before, max, approx, "grey");
        AddSide(table, report.After, max, approx, deltaColor);
        console.Write(table);

        var absTokens = Math.Abs(report.DeltaTokens).ToString("N0", CultureInfo.InvariantCulture);
        var pctText = report.DeltaPercent is null
            ? "n/a"
            : report.DeltaPercent.Value.ToString("+0.0;-0.0", CultureInfo.InvariantCulture) + "%";
        var verb = leaner ? "saved" : "added";
        var word = leaner ? "leaner" : "heavier";
        console.MarkupLine($"[bold]Headline:[/] [{deltaColor}]{verb} {absTokens} tok ({pctText}) — {word}[/]");

        if (isEstimate)
            console.MarkupLine(EstimateFooter);
    }

    private static void AddSide(Table table, ResponseCostReport side, int max, string approx, string barColor)
    {
        table.AddRow(
            Markup.Escape(side.Source),
            $"[{barColor}]{TokenBar.Render(side.ResponseTokens, max, 12)}[/]",
            $"{approx}{side.ResponseTokens.ToString("N0", CultureInfo.InvariantCulture)} tok",
            $"{approx}{side.PercentWindow.ToString("F1", CultureInfo.InvariantCulture)} %");
    }
}
