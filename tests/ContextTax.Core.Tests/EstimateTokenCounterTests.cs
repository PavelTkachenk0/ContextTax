using System.Text.Json.Nodes;
using ContextTax.Core.Counting;
using ContextTax.Core.Mcp;
using ContextTax.Core.Measurement;
using Xunit;

namespace ContextTax.Core.Tests;

public class EstimateTokenCounterTests
{
    private static McpTool Tool(string name) =>
        new(name, "a tool", JsonNode.Parse("""{ "type": "object" }""")!);

    [Fact]
    public void Declares_estimate_mode_and_proxy_label()
    {
        var counter = EstimateTokenCounter.CreateO200k();

        Assert.Equal(MeasurementMode.Estimate, counter.Mode);
        Assert.Equal(EstimateTokenCounter.LabelValue, counter.Label);
    }

    [Fact]
    public async Task Counts_baseline_above_zero()
    {
        var counter = EstimateTokenCounter.CreateO200k();

        var baseline = await counter.CountAsync("any-model", null);

        Assert.True(baseline > 0);
    }

    [Fact]
    public async Task Tools_cost_more_than_baseline()
    {
        var counter = EstimateTokenCounter.CreateO200k();

        var baseline = await counter.CountAsync("any-model", null);
        var withTool = await counter.CountAsync("any-model", new[] { Tool("read_file") });

        Assert.True(withTool > baseline);
    }

    [Fact]
    public async Task Is_deterministic()
    {
        var counter = EstimateTokenCounter.CreateO200k();
        var tools = new[] { Tool("read_file"), Tool("write_file") };

        var first = await counter.CountAsync("any-model", tools);
        var second = await counter.CountAsync("any-model", tools);

        Assert.Equal(first, second);
    }
}
