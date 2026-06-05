using ContextTax.Core.Mcp;
using Xunit;

namespace ContextTax.Core.Tests;

public class ToolsJsonLoaderTests
{
    [Fact]
    public void Loads_bare_array()
    {
        var json = """
        [
          { "name": "a", "description": "first", "inputSchema": { "type": "object" } },
          { "name": "b", "inputSchema": { "type": "object" } }
        ]
        """;
        var tools = ToolsJsonLoader.Load(json);
        Assert.Equal(2, tools.Count);
        Assert.Equal("a", tools[0].Name);
        Assert.Equal("first", tools[0].Description);
        Assert.Null(tools[1].Description);
        Assert.Equal("object", tools[0].InputSchema!["type"]!.GetValue<string>());
    }

    [Fact]
    public void Loads_object_with_tools_array_and_input_schema_alias()
    {
        var json = """
        { "tools": [ { "name": "a", "input_schema": { "type": "object" } } ] }
        """;
        var tools = ToolsJsonLoader.Load(json);
        Assert.Single(tools);
        Assert.Equal("a", tools[0].Name);
    }

    [Fact]
    public void Empty_array_yields_no_tools()
    {
        Assert.Empty(ToolsJsonLoader.Load("[]"));
    }

    [Fact]
    public void Malformed_json_throws_ToolsJsonException()
    {
        Assert.Throws<ToolsJsonException>(() => ToolsJsonLoader.Load("{ not json"));
    }

    [Fact]
    public void Missing_name_throws_ToolsJsonException()
    {
        Assert.Throws<ToolsJsonException>(
            () => ToolsJsonLoader.Load("""[ { "description": "x" } ]"""));
    }
}
