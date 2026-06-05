using ContextTax.Core.Counting;
using ContextTax.Core.Mcp;
using ContextTax.Core.Measurement;

namespace ContextTax.Core.Tests;

/// <summary>
/// Offline, deterministic counter: tokens = Baseline + sum(tool.Name.Length).
/// Lets tests assert exact numbers without a network call. Mode/Label are configurable
/// so tests can verify the measurer forwards a counter's provenance.
/// </summary>
internal sealed class FakeTokenCounter : ITokenCounter
{
    public const int Baseline = 10;

    public FakeTokenCounter(MeasurementMode mode = MeasurementMode.GroundTruth, string label = "fake")
    {
        Mode = mode;
        Label = label;
    }

    public MeasurementMode Mode { get; }

    public string Label { get; }

    public Task<int> CountAsync(string model, IReadOnlyList<McpTool>? tools, CancellationToken cancellationToken = default)
    {
        var n = Baseline;
        if (tools is not null)
            foreach (var t in tools)
                n += t.Name.Length;
        return Task.FromResult(n);
    }
}
