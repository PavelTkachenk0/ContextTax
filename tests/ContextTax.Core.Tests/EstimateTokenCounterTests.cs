using System.Text.Json.Nodes;
using ContextTax.Core.Counting;
using ContextTax.Core.Mcp;
using ContextTax.Core.Measurement;
using ContextTax.Core.Transcript;
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

        var baseline = await counter.CountAsync("any-model", CountInput.Empty);

        Assert.True(baseline > 0);
    }

    [Fact]
    public async Task Tools_cost_more_than_baseline()
    {
        var counter = EstimateTokenCounter.CreateO200k();

        var baseline = await counter.CountAsync("any-model", CountInput.Empty);
        var withTool = await counter.CountAsync("any-model", CountInput.ForTools(new[] { Tool("read_file") }));

        Assert.True(withTool > baseline);
    }

    [Fact]
    public async Task Is_deterministic()
    {
        var counter = EstimateTokenCounter.CreateO200k();
        var tools = new[] { Tool("read_file"), Tool("write_file") };

        var first = await counter.CountAsync("any-model", CountInput.ForTools(tools));
        var second = await counter.CountAsync("any-model", CountInput.ForTools(tools));

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Counts_a_transcript_and_is_deterministic()
    {
        var counter = EstimateTokenCounter.CreateO200k();
        var messages = new[]
        {
            new TranscriptMessage("assistant", new ContentBlock[]
            {
                new ToolUseBlock("t1", "read_file", JsonNode.Parse("""{ "path": "/a" }""")!),
            }),
            new TranscriptMessage("user", new ContentBlock[]
            {
                new ToolResultBlock("t1", JsonNode.Parse("\"file body\"")!),
            }),
        };
        var input = new CountInput { Messages = messages };

        var first = await counter.CountAsync("any-model", input);
        var second = await counter.CountAsync("any-model", input);

        Assert.True(first > 0);
        Assert.Equal(first, second);
    }
}
