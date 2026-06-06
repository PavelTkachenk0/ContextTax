using System.Text.Json.Nodes;

namespace ContextTax.Core.Mcp;

/// <summary>Maps an MCP tool definition's primitives to the domain <see cref="McpTool"/>.</summary>
internal static class McpToolMapper
{
    public static McpTool Map(string name, string? description, JsonNode? inputSchema) =>
        new(name, description, inputSchema?.DeepClone() ?? new JsonObject());
}
