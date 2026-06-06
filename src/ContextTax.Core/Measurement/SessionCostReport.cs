namespace ContextTax.Core.Measurement;

/// <summary>One row of the session timeline. Call/response are the marginal deltas of the
/// assistant (tool_use) and user (tool_result) messages of the turn; cumulative is the full
/// context snapshot after the turn.</summary>
public sealed record TurnCost(
    int Index,
    string ToolName,
    int CallTokens,
    int ResponseTokens,
    int AddedTokens,
    int CumulativeTokens,
    double PercentWindow);

public sealed record SessionCostReport
{
    public required string ModelId { get; init; }
    public required MeasurementMode Mode { get; init; }
    public required string CounterLabel { get; init; }
    public required int TurnCount { get; init; }
    public required int SchemaTokens { get; init; }
    public required IReadOnlyList<TurnCost> Turns { get; init; }
    public required int CallsTotal { get; init; }
    public required int ResponsesTotal { get; init; }
    public required int PeakContextTokens { get; init; }
    public required int ContextWindowTokens { get; init; }
    public required double PeakPercentWindow { get; init; }
    public required double ResponseToSchemaRatio { get; init; }
    public required double ResponseShareOfContext { get; init; }
}
