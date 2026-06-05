using System.Text.Json;
using ContextTax.Cli.Rendering;
using ContextTax.Core.Measurement;
using Xunit;

namespace ContextTax.Core.Tests;

public class ReportRendererTests
{
    private static SchemaCostReport Sample => new()
    {
        ModelId = "m1",
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
}
