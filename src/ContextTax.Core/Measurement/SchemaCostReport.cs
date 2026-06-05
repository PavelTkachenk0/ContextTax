namespace ContextTax.Core.Measurement;

public sealed record ToolCost(string Name, int Tokens);

public sealed record SchemaCostReport
{
    public required string ModelId { get; init; }
    public required int ToolCount { get; init; }
    public required int TotalSchemaTokens { get; init; }
    public required IReadOnlyList<ToolCost> PerTool { get; init; }
    public required int ContextWindowTokens { get; init; }
    public required double ContextWindowPercent { get; init; }
    public required double DollarCost { get; init; }
}
