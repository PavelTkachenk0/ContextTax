using ContextTax.Core.Mcp;
using Xunit;

namespace ContextTax.Core.Tests;

public class LiveToolSourceTests
{
    [Fact]
    public async Task Stdio_lists_tools_from_a_public_server()
    {
        // Gated integration test: opt-in via CONTEXTTAX_LIVE_TESTS=1. This is the only test that
        // spawns a real MCP server (npx) and reaches the network, so it stays OUT of CI to keep
        // the suite hermetic — no network calls, no npm packages pulled. CI never sets the
        // variable, so it skips there. Run locally (npx required):
        //   CONTEXTTAX_LIVE_TESTS=1 dotnet test
        if (Environment.GetEnvironmentVariable("CONTEXTTAX_LIVE_TESTS") != "1")
            return;

        var config = new McpServerConfig
        {
            Name = "everything",
            Transport = McpTransport.Stdio,
            Source = "test",
            Command = "npx",
            Args = ["-y", "@modelcontextprotocol/server-everything"],
        };
        var source = new LiveToolSource(config, TimeSpan.FromSeconds(60));

        var tools = await source.GetToolsAsync();

        Assert.NotEmpty(tools);
        Assert.All(tools, t => Assert.False(string.IsNullOrWhiteSpace(t.Name)));
    }
}
