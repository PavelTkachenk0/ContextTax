namespace ContextTax.Core.Measurement;

/// <summary>How a measurement's token counts were produced.</summary>
public enum MeasurementMode
{
    /// <summary>Exact counts from Anthropic's count_tokens endpoint.</summary>
    GroundTruth,

    /// <summary>Approximate counts from an offline tokenizer proxy (not Claude).</summary>
    Estimate,
}
