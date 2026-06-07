using System.Globalization;
using System.Text.Json;
using ContextTax.Cli.Rendering;
using ContextTax.Core.Counting;
using ContextTax.Core.Measurement;
using Spectre.Console.Testing;
using Xunit;

namespace ContextTax.Core.Tests;

public class ReportRendererTests
{
    private static SchemaCostReport Sample => new()
    {
        ModelId = "m1",
        Mode = MeasurementMode.GroundTruth,
        CounterLabel = AnthropicTokenCounter.LabelValue,
        ToolCount = 2,
        TotalSchemaTokens = 7,
        PerTool = new[] { new ToolCost("alpha", 5), new ToolCost("bb", 2) },
        ContextWindowTokens = 200_000,
        ContextWindowPercent = 0.0035,
        DollarCost = 0.000021,
    };

    [Fact]
    public void RenderJson_emits_valid_report_json()
    {
        var json = ReportRenderer.RenderJson(Sample);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(7, root.GetProperty("TotalSchemaTokens").GetInt32());
        Assert.Equal(2, root.GetProperty("ToolCount").GetInt32());
        Assert.Equal("alpha", root.GetProperty("PerTool")[0].GetProperty("Name").GetString());
        Assert.Equal("m1", root.GetProperty("ModelId").GetString());
    }

    [Fact]
    public void RenderJson_includes_mode_as_string_and_counter_label()
    {
        var report = Sample with
        {
            Mode = MeasurementMode.Estimate,
            CounterLabel = EstimateTokenCounter.LabelValue,
        };

        var json = ReportRenderer.RenderJson(report);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("Estimate", root.GetProperty("Mode").GetString());
        Assert.Equal(EstimateTokenCounter.LabelValue, root.GetProperty("CounterLabel").GetString());
    }

    [Fact]
    public void RenderCard_formats_numbers_invariantly_under_comma_locale()
    {
        var prev = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
        try
        {
            var report = Sample with { TotalSchemaTokens = 1145, ContextWindowPercent = 0.6 };
            var console = new TestConsole();
            ReportRenderer.RenderCard(report, console, "x");
            var output = console.Output;

            Assert.Contains("1,145", output);   // thousands separator is a comma, not a space
            Assert.Contains("0.6", output);      // decimal point, not a comma
            Assert.DoesNotContain("0,6", output);
        }
        finally
        {
            CultureInfo.CurrentCulture = prev;
        }
    }
}
