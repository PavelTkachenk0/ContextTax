using ContextTax.Core.Counting;
using ContextTax.Core.Measurement;
using ContextTax.Core.Transcript;

namespace ContextTax.Core.Tests;

/// <summary>
/// Offline, deterministic counter. tokens = Baseline + Σ tool.Name.Length + Σ message-block weights.
/// Each message contributes its own additive weight (no boundary effects), so tests can assert
/// exact per-turn call/response deltas. Mode/Label are configurable to verify provenance forwarding.
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

    public Task<int> CountAsync(string model, CountInput input, CancellationToken cancellationToken = default)
    {
        var n = Baseline;
        if (input.Tools is not null)
            foreach (var t in input.Tools)
                n += t.Name.Length;
        if (input.Messages is not null)
            foreach (var m in input.Messages)
                foreach (var b in m.Content)
                    n += Weight(b);
        return Task.FromResult(n);
    }

    private static int Weight(ContentBlock block) => block switch
    {
        TextBlock t => t.Text.Length,
        ToolUseBlock u => u.Name.Length + u.Input.ToJsonString().Length,
        ToolResultBlock r => r.Content.ToJsonString().Length,
        _ => 0,
    };
}
