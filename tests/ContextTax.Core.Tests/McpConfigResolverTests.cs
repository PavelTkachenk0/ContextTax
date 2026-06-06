using System.Text.Json.Nodes;
using ContextTax.Core.Mcp;
using Xunit;

namespace ContextTax.Core.Tests;

public class McpConfigResolverTests
{
    private static readonly string[] StdioArgs = ["-y", "srv"];
    private static readonly string[] ListedNames = ["a", "b"];

    private static JsonObject Obj(string json) => (JsonObject)JsonNode.Parse(json)!;

    private static McpConfigResolver Resolver(Func<string, string?>? env = null, params (string source, string mcpServers)[] layers) =>
        new(layers.Select(l => new ConfigLayer(l.source, Obj(l.mcpServers))).ToArray(), env ?? (_ => null));

    [Fact]
    public void Resolves_http_server_with_headers_and_env()
    {
        var resolver = Resolver(
            env: v => v == "TOK" ? "secret123" : null,
            layers: ("global", """{ "db": { "type": "http", "url": "https://h/mcp", "headers": { "x-api-key": "${TOK}" } } }"""));

        var cfg = resolver.Resolve("db");

        Assert.Equal(McpTransport.Http, cfg.Transport);
        Assert.Equal("https://h/mcp", cfg.Url);
        Assert.Equal("secret123", cfg.Headers!["x-api-key"]);
        Assert.Equal("global", cfg.Source);
    }

    [Fact]
    public void Resolves_stdio_server_from_command()
    {
        var resolver = Resolver(layers: ("proj", """{ "fs": { "command": "npx", "args": ["-y", "srv"] } }"""));

        var cfg = resolver.Resolve("fs");

        Assert.Equal(McpTransport.Stdio, cfg.Transport);
        Assert.Equal("npx", cfg.Command);
        Assert.Equal(StdioArgs, cfg.Args);
    }

    [Fact]
    public void Earlier_layer_wins_on_name_collision()
    {
        var resolver = Resolver(null,
            ("project", """{ "db": { "type": "http", "url": "https://project/mcp" } }"""),
            ("global", """{ "db": { "type": "http", "url": "https://global/mcp" } }"""));

        Assert.Equal("https://project/mcp", resolver.Resolve("db").Url);
        Assert.Single(resolver.List(), s => s.Name == "db");
    }

    [Fact]
    public void List_returns_all_names_with_source()
    {
        var resolver = Resolver(null,
            ("project", """{ "a": { "type": "http", "url": "https://a" } }"""),
            ("global", """{ "b": { "command": "x" } }"""));

        var names = resolver.List().Select(s => s.Name).OrderBy(n => n);
        Assert.Equal(ListedNames, names);
    }

    [Fact]
    public void Unknown_name_throws()
    {
        var resolver = Resolver(layers: ("global", "{}"));
        Assert.Throws<McpConfigException>(() => resolver.Resolve("nope"));
    }

    [Fact]
    public void Missing_env_var_throws_naming_the_variable_not_the_value()
    {
        var resolver = Resolver(layers: ("global", """{ "db": { "type": "http", "url": "https://h", "headers": { "x-api-key": "${MISSING}" } } }"""));
        var ex = Assert.Throws<McpConfigException>(() => resolver.Resolve("db"));
        Assert.Contains("MISSING", ex.Message);
    }

    [Fact]
    public void Non_string_command_throws_typed_exception()
    {
        var resolver = Resolver(layers: ("global", """{ "fs": { "command": 42 } }"""));
        Assert.Throws<McpConfigException>(() => resolver.Resolve("fs"));
    }

    [Fact]
    public void Non_string_arg_element_throws_typed_exception()
    {
        var resolver = Resolver(layers: ("global", """{ "fs": { "command": "npx", "args": ["-y", 9000] } }"""));
        Assert.Throws<McpConfigException>(() => resolver.Resolve("fs"));
    }
}
