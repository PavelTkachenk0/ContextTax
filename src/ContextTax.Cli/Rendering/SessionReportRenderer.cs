using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContextTax.Core.Measurement;
using Spectre.Console;

namespace ContextTax.Cli.Rendering;

public static class SessionReportRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string RenderJson(SessionCostReport report) =>
        JsonSerializer.Serialize(report, JsonOptions);

    public static void RenderCard(SessionCostReport report, IAnsiConsole console, string title)
    {
        var isEstimate = report.Mode == MeasurementMode.Estimate;
        var badge = isEstimate ? "[yellow]≈ ESTIMATE[/]" : "[green]✓ GROUND TRUTH[/]";
        var approx = isEstimate ? "~" : string.Empty;

        console.MarkupLine(
            $"[bold]ContextTax[/] · session · [green]{Markup.Escape(title)}[/]   {badge}   "
            + $"[grey]{report.TurnCount.ToString(CultureInfo.InvariantCulture)} turns · {report.ContextWindowTokens.ToString("N0", CultureInfo.InvariantCulture)} window[/]");

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("#");
        table.AddColumn("Tool");
        table.AddColumn(new TableColumn("Call").RightAligned());
        table.AddColumn(new TableColumn("Response").RightAligned());
        table.AddColumn(new TableColumn("Added").RightAligned());
        table.AddColumn(new TableColumn("Cumulative").RightAligned());
        table.AddColumn(new TableColumn("% window").RightAligned());
        foreach (var t in report.Turns)
        {
            table.AddRow(
                t.Index.ToString(CultureInfo.InvariantCulture),
                Markup.Escape(t.ToolName),
                $"{approx}{t.CallTokens.ToString("N0", CultureInfo.InvariantCulture)}",
                $"{approx}{t.ResponseTokens.ToString("N0", CultureInfo.InvariantCulture)}",
                $"{approx}{t.AddedTokens.ToString("N0", CultureInfo.InvariantCulture)}",
                $"{approx}{t.CumulativeTokens.ToString("N0", CultureInfo.InvariantCulture)}",
                $"{t.PercentWindow.ToString("F1", CultureInfo.InvariantCulture)}%");
        }

        console.Write(table);

        var totals = new Table().Border(TableBorder.Rounded);
        totals.AddColumn("Metric");
        totals.AddColumn(new TableColumn("Value").RightAligned());
        totals.AddRow("Schema (menu, paid once)", $"{approx}{report.SchemaTokens.ToString("N0", CultureInfo.InvariantCulture)}");
        totals.AddRow("Calls total", $"{approx}{report.CallsTotal.ToString("N0", CultureInfo.InvariantCulture)}");
        totals.AddRow("Responses total", $"{approx}{report.ResponsesTotal.ToString("N0", CultureInfo.InvariantCulture)}");
        var peakColor = TaxSeverity.Color(report.PeakPercentWindow);
        totals.AddRow(
            "Peak context",
            $"{approx}{report.PeakContextTokens.ToString("N0", CultureInfo.InvariantCulture)}  ([{peakColor}]{report.PeakPercentWindow.ToString("F1", CultureInfo.InvariantCulture)}% window[/])");
        totals.AddRow("Counted with",
            Markup.Escape(isEstimate ? report.CounterLabel : $"{report.CounterLabel} · {report.ModelId}"));
        console.Write(totals);

        console.MarkupLine(
            $"[bold]Headline:[/] responses are {report.ResponseToSchemaRatio.ToString("F1", CultureInfo.InvariantCulture)}× the schema; "
            + $"{(report.ResponseShareOfContext * 100).ToString("F0", CultureInfo.InvariantCulture)}% of session context is tool responses.");

        if (isEstimate)
        {
            console.MarkupLine(
                "[yellow]≈ Approximate[/] — o200k_base tokenizer (a non-Claude proxy), not "
                + "Anthropic count_tokens. Set ANTHROPIC_API_KEY for exact numbers.");
        }
    }
}
