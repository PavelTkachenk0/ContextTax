using ContextTax.Core.Measurement;

namespace ContextTax.Core.Counting;

public interface ITokenCounter
{
    /// <summary>The provenance of the counts this counter produces.</summary>
    MeasurementMode Mode { get; }

    /// <summary>Human-readable label of what counted (e.g. "Anthropic count_tokens").</summary>
    string Label { get; }

    /// <summary>Tokens of the given request snapshot (tools and/or message history).</summary>
    Task<int> CountAsync(string model, CountInput input, CancellationToken cancellationToken = default);
}
