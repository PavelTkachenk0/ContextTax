namespace ContextTax.Core.Measurement;

/// <summary>The before/after comparison of two response costs. <see cref="DeltaTokens"/> is
/// <c>after - before</c> (negative = leaner = win); <see cref="DeltaPercent"/> is null when
/// <c>before == 0</c> (no meaningful ratio).</summary>
public sealed record ResponseDiffReport
{
    public required string ModelId { get; init; }
    public required MeasurementMode Mode { get; init; }
    public required string CounterLabel { get; init; }
    public required int ContextWindowTokens { get; init; }
    public required ResponseCostReport Before { get; init; }
    public required ResponseCostReport After { get; init; }
    public required int DeltaTokens { get; init; }
    public required double? DeltaPercent { get; init; }
}
