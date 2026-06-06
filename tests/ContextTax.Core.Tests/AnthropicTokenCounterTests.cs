using System.Text.Json.Nodes;
using ContextTax.Core.Counting;
using ContextTax.Core.Mcp;
using ContextTax.Core.Measurement;
using ContextTax.Core.Transcript;
using Xunit;

namespace ContextTax.Core.Tests;

public class AnthropicTokenCounterTests
{
    [Fact]
    public async Task Tools_cost_more_than_baseline_against_real_api()
    {
        var key = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrWhiteSpace(key))
            return; // integration test: skipped when ANTHROPIC_API_KEY is not set

        using var http = new HttpClient();
        var counter = new AnthropicTokenCounter(new AnthropicCountTokensClient(http, key));

        var tool = new McpTool("read_file", "Reads a file from disk",
            JsonNode.Parse("""{ "type": "object", "properties": { "path": { "type": "string" } } }""")!);

        var baseline = await counter.CountAsync(Defaults.Model, CountInput.Empty);
        var withTool = await counter.CountAsync(Defaults.Model, CountInput.ForTools(new[] { tool }));

        Assert.True(baseline > 0);
        Assert.True(withTool > baseline);
    }

    [Fact]
    public void Anthropic_counter_reports_ground_truth_mode()
    {
        using var http = new HttpClient();
        var counter = new AnthropicTokenCounter(new AnthropicCountTokensClient(http, "unused-key"));

        Assert.Equal(MeasurementMode.GroundTruth, counter.Mode);
        Assert.Equal(AnthropicTokenCounter.LabelValue, counter.Label);
    }

    [Fact]
    public async Task Messages_cost_more_than_tools_only_against_real_api()
    {
        var key = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrWhiteSpace(key))
            return; // integration test: skipped when ANTHROPIC_API_KEY is not set

        using var http = new HttpClient();
        var counter = new AnthropicTokenCounter(new AnthropicCountTokensClient(http, key));

        var tool = new McpTool("read_file", "Reads a file from disk",
            JsonNode.Parse("""{ "type": "object", "properties": { "path": { "type": "string" } } }""")!);
        var tools = new[] { tool };

        var messages = new[]
        {
            new TranscriptMessage("assistant", new ContentBlock[]
            {
                new ToolUseBlock("tu_1", "read_file", JsonNode.Parse("""{ "path": "/etc/hosts" }""")!),
            }),
            new TranscriptMessage("user", new ContentBlock[]
            {
                new ToolResultBlock("tu_1", JsonNode.Parse("\"127.0.0.1 localhost\\n::1 localhost\"")!),
            }),
        };

        var toolsOnly = await counter.CountAsync(Defaults.Model, CountInput.ForTools(tools));
        var withMessages = await counter.CountAsync(
            Defaults.Model, new CountInput { Tools = tools, Messages = messages });

        Assert.True(toolsOnly > 0);
        Assert.True(withMessages > toolsOnly);
    }
}
