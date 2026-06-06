using System.Text.Json.Nodes;
using ContextTax.Core.Mcp;
using ContextTax.Core.Transcript;
using Xunit;

namespace ContextTax.Core.Tests;

public class TranscriptLoaderTests
{
    [Fact]
    public void Parses_bare_array_with_string_and_block_content()
    {
        const string json = """
        [
          { "role": "user", "content": "hello" },
          { "role": "assistant", "content": [
              { "type": "text", "text": "ok" },
              { "type": "tool_use", "id": "t1", "name": "read_file", "input": { "path": "/a" } }
          ]},
          { "role": "user", "content": [
              { "type": "tool_result", "tool_use_id": "t1", "content": "FILE" }
          ]}
        ]
        """;

        var transcript = TranscriptLoader.Load(json);

        Assert.Empty(transcript.Tools);
        Assert.Equal(3, transcript.Messages.Count);

        var userText = Assert.IsType<TextBlock>(transcript.Messages[0].Content[0]);
        Assert.Equal("hello", userText.Text);

        var use = Assert.IsType<ToolUseBlock>(transcript.Messages[1].Content[1]);
        Assert.Equal("read_file", use.Name);
        Assert.Equal("t1", use.Id);
        Assert.Equal("/a", use.Input["path"]!.GetValue<string>());

        var result = Assert.IsType<ToolResultBlock>(transcript.Messages[2].Content[0]);
        Assert.Equal("t1", result.ToolUseId);
    }

    [Fact]
    public void Wrapped_object_with_embedded_tools_wins_over_external()
    {
        const string json = """
        {
          "tools": [ { "name": "embedded", "inputSchema": { "type": "object" } } ],
          "messages": [ { "role": "user", "content": "hi" } ]
        }
        """;
        var external = new[] { new McpTool("external", null, new JsonObject()) };

        var transcript = TranscriptLoader.Load(json, external);

        Assert.Single(transcript.Tools);
        Assert.Equal("embedded", transcript.Tools[0].Name);
    }

    [Fact]
    public void Wrapped_object_without_tools_falls_back_to_external()
    {
        const string json = """{ "messages": [ { "role": "user", "content": "hi" } ] }""";
        var external = new[] { new McpTool("external", null, new JsonObject()) };

        var transcript = TranscriptLoader.Load(json, external);

        Assert.Single(transcript.Tools);
        Assert.Equal("external", transcript.Tools[0].Name);
    }

    [Fact]
    public void Invalid_json_throws_transcript_exception()
    {
        var ex = Assert.Throws<TranscriptException>(() => TranscriptLoader.Load("{ not json"));
        Assert.Contains("Invalid JSON", ex.Message);
    }

    [Fact]
    public void Unknown_block_type_throws_transcript_exception()
    {
        const string json = """
        [ { "role": "assistant", "content": [ { "type": "image", "data": "x" } ] } ]
        """;
        Assert.Throws<TranscriptException>(() => TranscriptLoader.Load(json));
    }

    [Fact]
    public void Tool_use_with_non_string_id_throws_transcript_exception()
    {
        const string json = """
        [ { "role": "assistant", "content": [ { "type": "tool_use", "id": 42, "name": "x", "input": {} } ] } ]
        """;
        Assert.Throws<TranscriptException>(() => TranscriptLoader.Load(json));
    }
}
