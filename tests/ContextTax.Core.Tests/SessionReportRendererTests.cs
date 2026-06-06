using System.Text.Json;
using ContextTax.Cli.Rendering;
using ContextTax.Core.Counting;
using ContextTax.Core.Measurement;
using Xunit;

namespace ContextTax.Core.Tests;

public class SessionReportRendererTests
{
    private static SessionCostReport Sample => new()
    {
        ModelId = "m1",
        Mode = MeasurementMode.Estimate,
        CounterLabel = EstimateTokenCounter.LabelValue,
        TurnCount = 1,
        SchemaTokens = 1_500,
        Turns = new[] { new TurnCost(1, "search_files", 18, 5_012, 5_030, 6_530, 3.265) },
        CallsTotal = 18,
        ResponsesTotal = 5_012,
        PeakContextTokens = 6_530,
        ContextWindowTokens = 200_000,
        PeakPercentWindow = 3.265,
        ResponseToSchemaRatio = 3.34,
        ResponseShareOfContext = 0.767,
    };

    [Fact]
    public void RenderJson_emits_turns_and_totals()
    {
        var json = SessionReportRenderer.RenderJson(Sample);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(1, root.GetProperty("TurnCount").GetInt32());
        Assert.Equal("search_files", root.GetProperty("Turns")[0].GetProperty("ToolName").GetString());
        Assert.Equal(5_012, root.GetProperty("Turns")[0].GetProperty("ResponseTokens").GetInt32());
        Assert.Equal(6_530, root.GetProperty("PeakContextTokens").GetInt32());
        Assert.Equal(1_500, root.GetProperty("SchemaTokens").GetInt32());
    }

    [Fact]
    public void RenderJson_includes_mode_as_string_and_counter_label()
    {
        var json = SessionReportRenderer.RenderJson(Sample);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("Estimate", root.GetProperty("Mode").GetString());
        Assert.Equal(EstimateTokenCounter.LabelValue, root.GetProperty("CounterLabel").GetString());
    }
}
