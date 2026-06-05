namespace ContextTax.Core.Measurement;

public sealed record MeasurementOptions
{
    public required string Model { get; init; }
    public int ContextWindowTokens { get; init; } = Defaults.ContextWindowTokens;
    public double InputPricePerMTokUsd { get; init; } = Defaults.InputPricePerMTokUsd;
}
