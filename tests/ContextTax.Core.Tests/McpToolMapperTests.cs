using System.Text.Json.Nodes;
using ContextTax.Core.Mcp;
using Xunit;

namespace ContextTax.Core.Tests;

public class McpToolMapperTests
{
    [Fact]
    public void Maps_name_description_and_schema()
    {
        var schema = JsonNode.Parse("""{ "type": "object", "properties": { "p": { "type": "string" } } }""")!;

        var tool = McpToolMapper.Map("read_file", "Reads a file", schema);

        Assert.Equal("read_file", tool.Name);
        Assert.Equal("Reads a file", tool.Description);
        Assert.Equal("object", tool.InputSchema["type"]!.GetValue<string>());
    }

    [Fact]
    public void Null_description_and_schema_become_safe_defaults()
    {
        var tool = McpToolMapper.Map("t", null, null);

        Assert.Equal("t", tool.Name);
        Assert.Null(tool.Description);
        Assert.NotNull(tool.InputSchema); // empty object, never null
    }
}
