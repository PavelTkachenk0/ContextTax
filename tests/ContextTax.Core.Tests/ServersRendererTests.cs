using System.Text.Json;
using ContextTax.Cli.Rendering;
using ContextTax.Core.Mcp;
using Xunit;

namespace ContextTax.Core.Tests;

public class ServersRendererTests
{
    private static readonly McpServerConfig[] Sample =
    [
        new() { Name = "remote-db", Transport = McpTransport.Http, Source = "~/.claude.json (global)", Url = "https://h/mcp",
                Headers = new Dictionary<string, string> { ["x-api-key"] = "SECRET-TOKEN-VALUE" } },
        new() { Name = "fs", Transport = McpTransport.Stdio, Source = "./.mcp.json", Command = "npx" },
    ];

    [Fact]
    public void RenderJson_lists_name_transport_source_and_header_keys_only()
    {
        var json = ServersRenderer.RenderJson(Sample);

        Assert.DoesNotContain("SECRET-TOKEN-VALUE", json); // never leak header values
        using var doc = JsonDocument.Parse(json);
        var first = doc.RootElement[0];
        Assert.Equal("remote-db", first.GetProperty("Name").GetString());
        Assert.Equal("Http", first.GetProperty("Transport").GetString());
        Assert.Equal("x-api-key", first.GetProperty("HeaderKeys")[0].GetString());
    }
}
