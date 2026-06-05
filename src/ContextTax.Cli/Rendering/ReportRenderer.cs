using System.Globalization;
using System.Text.Json;
using ContextTax.Core.Measurement;
using Spectre.Console;

namespace ContextTax.Cli.Rendering;

public static class ReportRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string RenderJson(SchemaCostReport report) =>
        JsonSerializer.Serialize(report, JsonOptions);

    public static void RenderCard(SchemaCostReport report, IAnsiConsole console, string title)
    {
        console.MarkupLine($"[bold]ContextTax[/] · report card · [green]{Markup.Escape(title)}[/]");

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Metric");
        table.AddColumn(new TableColumn("Value").RightAligned());
        table.AddRow("Schema (tools loaded)", $"{report.TotalSchemaTokens:N0} tok");
        table.AddRow("Tools", report.ToolCount.ToString(CultureInfo.InvariantCulture));
        table.AddRow("Context tax", $"{report.ContextWindowPercent:F1} % of a {report.ContextWindowTokens:N0} window");
        table.AddRow("Cost to load (API)", $"${report.DollarCost:F2}");
        table.AddRow("Model", Markup.Escape(report.ModelId));
        console.Write(table);

        if (report.PerTool.Count > 0)
        {
            var offenders = new Table().Border(TableBorder.Rounded).Title("Top offenders");
            offenders.AddColumn("Tool");
            offenders.AddColumn(new TableColumn("Tokens").RightAligned());
            foreach (var tool in report.PerTool.Take(10))
                offenders.AddRow(Markup.Escape(tool.Name), $"{tool.Tokens:N0}");
            console.Write(offenders);
        }
    }
}
