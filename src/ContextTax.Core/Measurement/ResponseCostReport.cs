namespace ContextTax.Core.Measurement;

/// <summary>The token cost a single captured tool response imposes on the context window.
/// <see cref="Source"/> is a display label only (filename or "(stdin)").</summary>
public sealed record ResponseCostReport
{
    public required string Source { get; init; }
    public required string ModelId { get; init; }
    public required MeasurementMode Mode { get; init; }
    public required string CounterLabel { get; init; }
    public required int ResponseTokens { get; init; }
    public required int ContextWindowTokens { get; init; }
    public required double PercentWindow { get; init; }
}
