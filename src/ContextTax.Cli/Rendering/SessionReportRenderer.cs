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
            + $"[grey]{report.TurnCount} turns · {report.ContextWindowTokens:N0} window[/]");

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
                $"{approx}{t.CallTokens:N0}",
                $"{approx}{t.ResponseTokens:N0}",
                $"{approx}{t.AddedTokens:N0}",
                $"{approx}{t.CumulativeTokens:N0}",
                $"{t.PercentWindow:F1}%");
        }

        console.Write(table);

        var totals = new Table().Border(TableBorder.Rounded);
        totals.AddColumn("Metric");
        totals.AddColumn(new TableColumn("Value").RightAligned());
        totals.AddRow("Schema (menu, paid once)", $"{approx}{report.SchemaTokens:N0}");
        totals.AddRow("Calls total", $"{approx}{report.CallsTotal:N0}");
        totals.AddRow("Responses total", $"{approx}{report.ResponsesTotal:N0}");
        totals.AddRow("Peak context", $"{approx}{report.PeakContextTokens:N0}  ({report.PeakPercentWindow:F1}% window)");
        totals.AddRow("Counted with",
            Markup.Escape(isEstimate ? report.CounterLabel : $"{report.CounterLabel} · {report.ModelId}"));
        console.Write(totals);

        console.MarkupLine(
            $"[bold]Headline:[/] responses are {report.ResponseToSchemaRatio:F1}× the schema; "
            + $"{report.ResponseShareOfContext * 100:F0}% of session context is tool responses.");

        if (isEstimate)
        {
            console.MarkupLine(
                "[yellow]≈ Approximate[/] — o200k_base tokenizer (a non-Claude proxy), not "
                + "Anthropic count_tokens. Set ANTHROPIC_API_KEY for exact numbers.");
        }
    }
}
