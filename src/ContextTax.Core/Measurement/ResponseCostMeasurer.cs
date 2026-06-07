using System.Text.Json.Nodes;
using ContextTax.Core.Counting;
using ContextTax.Core.Transcript;

namespace ContextTax.Core.Measurement;

/// <summary>
/// Measures what a single captured tool response costs the context window, as the marginal delta
/// (ADR 0004, message axis) of appending it as a <c>tool_result</c> to a minimal synthetic turn.
/// The constant prefix (a "." user message + the tool_use) cancels in the delta, leaving the pure
/// response contribution. Payload is measured as the literal <b>text</b> of the tool_result — faithful
/// to how MCP results reach the model. Pure: no I/O of its own; works over any <see cref="ITokenCounter"/>.
/// </summary>
public sealed class ResponseCostMeasurer
{
    private const string SyntheticToolUseId = "toolu_ctx";
    private readonly ITokenCounter _counter;

    public ResponseCostMeasurer(ITokenCounter counter) => _counter = counter;

    public async Task<ResponseCostReport> MeasureAsync(
        string source, string responseText, MeasurementOptions options, CancellationToken cancellationToken = default)
    {
        var window = options.ContextWindowTokens;

        var prompt = new TranscriptMessage("user", new ContentBlock[] { new TextBlock(".") });
        var call = new TranscriptMessage("assistant",
            new ContentBlock[] { new ToolUseBlock(SyntheticToolUseId, "tool", new JsonObject()) });
        var result = new TranscriptMessage("user",
            new ContentBlock[] { new ToolResultBlock(SyntheticToolUseId, JsonValue.Create(responseText)!) });

        // The baseline must be a valid count_tokens request, so it carries an EMPTY tool_result
        // (not a dangling tool_use). Its constant framing cancels in the delta, leaving the payload. (ADR 0012)
        var baselineMessages = ToolResultPadding.PadDanglingToolUse(new[] { prompt, call });
        var baseline = await _counter
            .CountAsync(options.Model, new CountInput { Messages = baselineMessages }, cancellationToken)
            .ConfigureAwait(false);
        var withResult = await _counter
            .CountAsync(options.Model, new CountInput { Messages = new[] { prompt, call, result } }, cancellationToken)
            .ConfigureAwait(false);

        var responseTokens = Math.Max(0, withResult - baseline);

        return new ResponseCostReport
        {
            Source = source,
            ModelId = options.Model,
            Mode = _counter.Mode,
            CounterLabel = _counter.Label,
            ResponseTokens = responseTokens,
            ContextWindowTokens = window,
            PercentWindow = window > 0 ? (double)responseTokens / window * 100.0 : 0.0,
        };
    }
}
