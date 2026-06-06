using System.Text.Json.Nodes;
using ContextTax.Cli.Support;
using ContextTax.Core.Mcp;
using Xunit;

namespace ContextTax.Core.Tests;

public class ToolSourceResolverTests
{
    private static McpTool Tool(string n) => new(n, null, new JsonObject());

    [Fact]
    public async Task Tools_path_loads_from_file()
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, """{ "tools": [ { "name": "rf", "inputSchema": { "type": "object" } } ] }""");

        var resolver = new ToolSourceResolver(_ => new FakeToolSource(new[] { Tool("unused") }));
        var tools = await resolver.ResolveAsync(new ToolSourceOptions { ToolsPath = path });

        Assert.Single(tools);
        Assert.Equal("rf", tools[0].Name);
        File.Delete(path);
    }

    [Fact]
    public async Task Url_builds_http_config_and_uses_the_live_source()
    {
        McpServerConfig? captured = null;
        var resolver = new ToolSourceResolver(cfg => { captured = cfg; return new FakeToolSource(new[] { Tool("live") }); });

        var tools = await resolver.ResolveAsync(new ToolSourceOptions { Url = "https://h/mcp", Headers = new() { ["x-api-key"] = "k" } });

        Assert.Equal("live", tools[0].Name);
        Assert.Equal(McpTransport.Http, captured!.Transport);
        Assert.Equal("https://h/mcp", captured.Url);
        Assert.Equal("k", captured.Headers!["x-api-key"]);
    }

    [Fact]
    public async Task No_source_throws_usage_error()
    {
        var resolver = new ToolSourceResolver(_ => new FakeToolSource(Array.Empty<McpTool>()));
        await Assert.ThrowsAsync<ToolSourceException>(() => resolver.ResolveAsync(new ToolSourceOptions()));
    }

    [Fact]
    public async Task Multiple_sources_throw_usage_error()
    {
        var resolver = new ToolSourceResolver(_ => new FakeToolSource(Array.Empty<McpTool>()));
        await Assert.ThrowsAsync<ToolSourceException>(() =>
            resolver.ResolveAsync(new ToolSourceOptions { ToolsPath = "a", Url = "b" }));
    }

    [Fact]
    public async Task Url_with_query_credentials_is_not_echoed_in_the_server_name()
    {
        McpServerConfig? captured = null;
        var resolver = new ToolSourceResolver(cfg => { captured = cfg; return new FakeToolSource(Array.Empty<McpTool>()); });

        await resolver.ResolveAsync(new ToolSourceOptions { Url = "https://h/mcp?token=SECRET" });

        Assert.DoesNotContain("SECRET", captured!.Name);
        Assert.Equal("https://h/mcp?token=SECRET", captured.Url); // full URL still used to connect
    }

    [Fact]
    public async Task Malformed_explicit_config_surfaces_a_clear_error()
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, "{ not json");

        var resolver = new ToolSourceResolver(_ => new FakeToolSource(Array.Empty<McpTool>()));
        await Assert.ThrowsAsync<ToolSourceException>(() =>
            resolver.ResolveAsync(new ToolSourceOptions { ServerName = "x", ConfigPath = path }));

        File.Delete(path);
    }
}
