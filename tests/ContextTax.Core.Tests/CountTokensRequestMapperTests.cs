using System.Text.Json;
using System.Text.Json.Nodes;
using ContextTax.Core.Counting;
using ContextTax.Core.Mcp;
using Xunit;

namespace ContextTax.Core.Tests;

public class CountTokensRequestMapperTests
{
    private static string Serialize(string model, IReadOnlyList<McpTool>? tools) =>
        JsonSerializer.Serialize(CountTokensRequestMapper.Map(model, tools), CountTokensJson.Options);

    [Fact]
    public void Builds_minimal_request_without_tools()
    {
        var json = Serialize("m1", null);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("m1", root.GetProperty("model").GetString());
        Assert.Equal(1, root.GetProperty("messages").GetArrayLength());
        Assert.False(root.TryGetProperty("tools", out _));
    }

    [Fact]
    public void Maps_inputSchema_to_input_schema()
    {
        var tool = new McpTool("read_file", "Reads a file",
            JsonNode.Parse("""{ "type": "object", "properties": { "path": { "type": "string" } } }""")!);

        var json = Serialize("m1", new[] { tool });
        using var doc = JsonDocument.Parse(json);
        var t0 = doc.RootElement.GetProperty("tools")[0];

        Assert.Equal("read_file", t0.GetProperty("name").GetString());
        Assert.Equal("Reads a file", t0.GetProperty("description").GetString());
        Assert.Equal("object", t0.GetProperty("input_schema").GetProperty("type").GetString());
    }
}
