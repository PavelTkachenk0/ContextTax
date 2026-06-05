using System.Text.Json.Nodes;

namespace ContextTax.Core.Mcp;

/// <summary>One MCP tool definition (the shape from an MCP tools/list result).</summary>
public sealed record McpTool(string Name, string? Description, JsonNode InputSchema);
