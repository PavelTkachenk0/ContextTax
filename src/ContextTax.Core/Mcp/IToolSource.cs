namespace ContextTax.Core.Mcp;

/// <summary>A source of MCP tool definitions: a live server (SDK) or a fake in tests. The file
/// path is handled directly by <c>ToolsJsonLoader</c>, not through this seam.</summary>
public interface IToolSource
{
    Task<IReadOnlyList<McpTool>> GetToolsAsync(CancellationToken cancellationToken = default);
}
