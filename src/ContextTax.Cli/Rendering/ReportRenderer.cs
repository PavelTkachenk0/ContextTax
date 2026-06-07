using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContextTax.Core.Measurement;
using Spectre.Console;

namespace ContextTax.Cli.Rendering;

public static class ReportRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string RenderJson(SchemaCostReport report) =>
        JsonSerializer.Serialize(report, JsonOptions);

    public static void RenderCard(SchemaCostReport report, IAnsiConsole console, string title)
    {
        var isEstimate = report.Mode == MeasurementMode.Estimate;
        var badge = isEstimate ? "[yellow]≈ ESTIMATE[/]" : "[green]✓ GROUND TRUTH[/]";
        var approx = isEstimate ? "~" : string.Empty;

        console.MarkupLine($"[bold]ContextTax[/] · report card · [green]{Markup.Escape(title)}[/]   {badge}");

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Metric");
        table.AddColumn(new TableColumn("Value").RightAligned());
        table.AddRow("Schema (tools loaded)", $"{approx}{report.TotalSchemaTokens.ToString("N0", CultureInfo.InvariantCulture)} tok");
        table.AddRow("Tools", report.ToolCount.ToString(CultureInfo.InvariantCulture));
        table.AddRow("Context tax", $"{approx}{report.ContextWindowPercent.ToString("F1", CultureInfo.InvariantCulture)} % of a {report.ContextWindowTokens.ToString("N0", CultureInfo.InvariantCulture)} window");
        table.AddRow(
            isEstimate ? "Est. API-equivalent" : "Cost to load (API)",
            $"{approx}${report.DollarCost.ToString("F2", CultureInfo.InvariantCulture)}");
        table.AddRow(
            "Counted with",
            Markup.Escape(isEstimate ? report.CounterLabel : $"{report.CounterLabel} · {report.ModelId}"));
        console.Write(table);

        if (report.PerTool.Count > 0)
        {
            var offenders = new Table().Border(TableBorder.Rounded).Title("Top offenders");
            offenders.AddColumn("Tool");
            offenders.AddColumn(new TableColumn("Tokens").RightAligned());
            foreach (var tool in report.PerTool.Take(10))
                offenders.AddRow(Markup.Escape(tool.Name), $"{approx}{tool.Tokens.ToString("N0", CultureInfo.InvariantCulture)}");
            console.Write(offenders);
        }

        if (isEstimate)
        {
            console.MarkupLine(
                "[yellow]≈ Approximate[/] — o200k_base tokenizer (a non-Claude proxy), not " +
                "Anthropic count_tokens. Set ANTHROPIC_API_KEY for exact numbers.");
        }
    }
}
