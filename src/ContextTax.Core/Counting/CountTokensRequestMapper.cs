using ContextTax.Core.Mcp;

namespace ContextTax.Core.Counting;

/// <summary>Maps the domain MCP tools to the count_tokens wire request.</summary>
internal static class CountTokensRequestMapper
{
    public static CountTokensRequest Map(string model, IReadOnlyList<McpTool>? tools)
    {
        var messages = new[] { new CountTokensMessage("user", ".") };
        var wireTools = tools is { Count: > 0 }
            ? tools.Select(static t => new CountTokensTool(t.Name, t.Description ?? string.Empty, t.InputSchema)).ToArray()
            : null;
        return new CountTokensRequest(model, messages, wireTools);
    }
}
