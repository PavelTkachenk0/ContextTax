using ContextTax.Core.Mcp;

namespace ContextTax.Core.Tests;

internal sealed class FakeToolSource : IToolSource
{
    private readonly IReadOnlyList<McpTool> _tools;

    public FakeToolSource(IReadOnlyList<McpTool> tools) => _tools = tools;

    public Task<IReadOnlyList<McpTool>> GetToolsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_tools);
}
