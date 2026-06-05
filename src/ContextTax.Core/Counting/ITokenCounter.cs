using ContextTax.Core.Mcp;

namespace ContextTax.Core.Counting;

public interface ITokenCounter
{
    /// <summary>Tokens of a minimal request, optionally including the given tools.</summary>
    Task<int> CountAsync(string model, IReadOnlyList<McpTool>? tools, CancellationToken cancellationToken = default);
}
