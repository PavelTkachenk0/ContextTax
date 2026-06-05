using System.Text.Json.Nodes;
using ContextTax.Core.Mcp;
using ContextTax.Core.Measurement;
using Xunit;

namespace ContextTax.Core.Tests;

public class SchemaCostMeasurerTests
{
    private static readonly string[] ExpectedSortedNames = ["alpha", "bb"];

    private static McpTool Tool(string name) => new(name, null, new JsonObject());

    private static MeasurementOptions Options => new()
    {
        Model = "test-model",
        ContextWindowTokens = 200_000,
        InputPricePerMTokUsd = 3.0,
    };

    [Fact]
    public async Task Computes_total_per_tool_and_derived_values()
    {
        var measurer = new SchemaCostMeasurer(new FakeTokenCounter());
        var tools = new[] { Tool("bb"), Tool("alpha") }; // lengths 2 and 5

        var report = await measurer.MeasureAsync(tools, Options);

        Assert.Equal(2, report.ToolCount);
        Assert.Equal(7, report.TotalSchemaTokens);                 // (10+2+5) - 10
        Assert.Equal(ExpectedSortedNames, report.PerTool.Select(p => p.Name)); // sorted desc
        Assert.Equal(5, report.PerTool[0].Tokens);
        Assert.Equal(2, report.PerTool[1].Tokens);
        Assert.Equal(200_000, report.ContextWindowTokens);
        Assert.Equal(7.0 / 200_000 * 100, report.ContextWindowPercent, 6);
        Assert.Equal(7.0 / 1_000_000 * 3.0, report.DollarCost, 12);
        Assert.Equal("test-model", report.ModelId);
    }

    [Fact]
    public async Task Empty_tools_yields_zero()
    {
        var measurer = new SchemaCostMeasurer(new FakeTokenCounter());
        var report = await measurer.MeasureAsync(Array.Empty<McpTool>(), Options);

        Assert.Equal(0, report.ToolCount);
        Assert.Equal(0, report.TotalSchemaTokens);
        Assert.Empty(report.PerTool);
        Assert.Equal(0, report.ContextWindowPercent);
        Assert.Equal(0, report.DollarCost);
    }

    [Fact]
    public async Task Single_tool()
    {
        var measurer = new SchemaCostMeasurer(new FakeTokenCounter());
        var report = await measurer.MeasureAsync(new[] { Tool("x") }, Options);

        Assert.Equal(1, report.TotalSchemaTokens);
        Assert.Single(report.PerTool);
        Assert.Equal(("x", 1), (report.PerTool[0].Name, report.PerTool[0].Tokens));
    }

    [Fact]
    public async Task Forwards_counter_mode_and_label_into_report()
    {
        var counter = new FakeTokenCounter(MeasurementMode.Estimate, "o200k_base (offline proxy)");
        var measurer = new SchemaCostMeasurer(counter);

        var report = await measurer.MeasureAsync(new[] { Tool("x") }, Options);

        Assert.Equal(MeasurementMode.Estimate, report.Mode);
        Assert.Equal("o200k_base (offline proxy)", report.CounterLabel);
    }
}
