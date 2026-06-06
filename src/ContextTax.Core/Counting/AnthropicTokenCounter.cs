using System.Text.Json;
using ContextTax.Core.Measurement;

namespace ContextTax.Core.Counting;

/// <summary>
/// Ground-truth token counter via Anthropic count_tokens: maps tools to the wire model,
/// serializes, delegates the HTTP call to <see cref="AnthropicCountTokensClient"/>, and
/// parses the returned token count.
/// </summary>
public sealed class AnthropicTokenCounter : ITokenCounter
{
    private readonly AnthropicCountTokensClient _client;

    public AnthropicTokenCounter(AnthropicCountTokensClient client) => _client = client;

    /// <summary>Human-readable label exposed as a constant so test fixtures can reference it without duplication.</summary>
    public const string LabelValue = "Anthropic count_tokens";

    public MeasurementMode Mode => MeasurementMode.GroundTruth;

    public string Label => LabelValue;

    public async Task<int> CountAsync(string model, CountInput input, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(CountTokensRequestMapper.Map(model, input), CountTokensJson.Options);
        var body = await _client.PostAsync(payload, cancellationToken).ConfigureAwait(false);
        var response = JsonSerializer.Deserialize<CountTokensResponse>(body, CountTokensJson.Options)
            ?? throw new TokenCountException(200, $"unexpected count_tokens response body: {body}");
        return response.InputTokens;
    }
}
