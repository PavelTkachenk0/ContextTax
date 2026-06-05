using System.Text.Json.Nodes;
using ContextTax.Core.Counting;
using ContextTax.Core.Mcp;
using ContextTax.Core.Measurement;
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

        var baseline = await counter.CountAsync(Defaults.Model, null);
        var withTool = await counter.CountAsync(Defaults.Model, new[] { tool });

        Assert.True(baseline > 0);
        Assert.True(withTool > baseline);
    }
}
