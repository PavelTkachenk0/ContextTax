using ContextTax.Core.Counting;
using ContextTax.Core.Mcp;

namespace ContextTax.Core.Tests;

/// <summary>
/// Offline, deterministic counter: tokens = Baseline + sum(tool.Name.Length).
/// Lets tests assert exact numbers without a network call.
/// </summary>
internal sealed class FakeTokenCounter : ITokenCounter
{
    public const int Baseline = 10;

    public Task<int> CountAsync(string model, IReadOnlyList<McpTool>? tools, CancellationToken cancellationToken = default)
    {
        var n = Baseline;
        if (tools is not null)
            foreach (var t in tools)
                n += t.Name.Length;
        return Task.FromResult(n);
    }
}
