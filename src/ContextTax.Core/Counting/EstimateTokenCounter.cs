using System.Text.Json;
using ContextTax.Core.Mcp;
using ContextTax.Core.Measurement;
using Microsoft.ML.Tokenizers;

namespace ContextTax.Core.Counting;

/// <summary>
/// Offline, approximate token counter. Tokenizes the same wire payload the API path
/// would send (reusing the count_tokens mapper + JSON options) with the o200k_base
/// tokenizer — a non-Claude proxy. Keyless and network-free; counts are an estimate,
/// not ground truth.
/// </summary>
public sealed class EstimateTokenCounter : ITokenCounter
{
    /// <summary>Human-readable label exposed as a constant so test fixtures can reference it without duplication.</summary>
    public const string LabelValue = "o200k_base (offline proxy)";

    private readonly Tokenizer _tokenizer;

    public EstimateTokenCounter(Tokenizer tokenizer) => _tokenizer = tokenizer;

    /// <summary>
    /// Builds an estimator backed by the embedded o200k_base vocabulary. Each call loads
    /// the vocabulary, so create one instance and reuse it across multiple counts.
    /// </summary>
    public static EstimateTokenCounter CreateO200k() =>
        new(TiktokenTokenizer.CreateForEncoding("o200k_base"));

    public MeasurementMode Mode => MeasurementMode.Estimate;

    public string Label => LabelValue;

    public Task<int> CountAsync(string model, IReadOnlyList<McpTool>? tools, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(CountTokensRequestMapper.Map(model, tools), CountTokensJson.Options);
        return Task.FromResult(_tokenizer.CountTokens(payload));
    }
}
