using System.Text.Json;
using System.Text.Json.Nodes;
using ContextTax.Core.Counting;
using ContextTax.Core.Mcp;
using ContextTax.Core.Transcript;
using Xunit;

namespace ContextTax.Core.Tests;

public class CountTokensRequestMapperTests
{
    private static string Serialize(CountInput input) =>
        JsonSerializer.Serialize(CountTokensRequestMapper.Map("m1", input), CountTokensJson.Options);

    [Fact]
    public void Empty_input_builds_minimal_baseline_request()
    {
        var json = Serialize(CountInput.Empty);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("m1", root.GetProperty("model").GetString());
        Assert.Equal(1, root.GetProperty("messages").GetArrayLength());
        Assert.False(root.TryGetProperty("tools", out _));

        var block0 = root.GetProperty("messages")[0].GetProperty("content")[0];
        Assert.Equal("text", block0.GetProperty("type").GetString());
        Assert.Equal(".", block0.GetProperty("text").GetString());
    }

    [Fact]
    public void Maps_inputSchema_to_input_schema()
    {
        var tool = new McpTool("read_file", "Reads a file",
            JsonNode.Parse("""{ "type": "object", "properties": { "path": { "type": "string" } } }""")!);

        var json = Serialize(CountInput.ForTools(new[] { tool }));
        using var doc = JsonDocument.Parse(json);
        var t0 = doc.RootElement.GetProperty("tools")[0];

        Assert.Equal("read_file", t0.GetProperty("name").GetString());
        Assert.Equal("Reads a file", t0.GetProperty("description").GetString());
        Assert.Equal("object", t0.GetProperty("input_schema").GetProperty("type").GetString());
    }

    [Fact]
    public void Maps_tool_use_and_tool_result_blocks_to_anthropic_shape()
    {
        var messages = new[]
        {
            new TranscriptMessage("assistant", new ContentBlock[]
            {
                new ToolUseBlock("tu_1", "read_file", JsonNode.Parse("""{ "path": "/a" }""")!),
            }),
            new TranscriptMessage("user", new ContentBlock[]
            {
                new ToolResultBlock("tu_1", JsonNode.Parse("\"FILE BODY\"")!),
            }),
        };

        var json = Serialize(new CountInput { Messages = messages });
        using var doc = JsonDocument.Parse(json);
        var msgs = doc.RootElement.GetProperty("messages");

        var use = msgs[0].GetProperty("content")[0];
        Assert.Equal("tool_use", use.GetProperty("type").GetString());
        Assert.Equal("tu_1", use.GetProperty("id").GetString());
        Assert.Equal("read_file", use.GetProperty("name").GetString());
        Assert.Equal("/a", use.GetProperty("input").GetProperty("path").GetString());

        var result = msgs[1].GetProperty("content")[0];
        Assert.Equal("tool_result", result.GetProperty("type").GetString());
        Assert.Equal("tu_1", result.GetProperty("tool_use_id").GetString());
        Assert.Equal("FILE BODY", result.GetProperty("content").GetString());
    }

    [Fact]
    public void Maps_text_block_in_message_to_text_type()
    {
        var messages = new[]
        {
            new TranscriptMessage("assistant", new ContentBlock[]
            {
                new TextBlock("hello"),
            }),
        };

        var json = Serialize(new CountInput { Messages = messages });
        using var doc = JsonDocument.Parse(json);
        var block = doc.RootElement.GetProperty("messages")[0].GetProperty("content")[0];

        Assert.Equal("text", block.GetProperty("type").GetString());
        Assert.Equal("hello", block.GetProperty("text").GetString());
    }
}
