using ContextTax.Core.Counting;
using ContextTax.Core.Transcript;

namespace ContextTax.Core.Measurement;

/// <summary>
/// Computes per-turn token cost along a recorded transcript using marginal deltas over an
/// <see cref="ITokenCounter"/> (ADR 0004, applied to the message axis). Pure: no I/O of its own.
/// </summary>
/// <remarks>
/// <b>v1 pairing assumptions and limitations:</b>
/// <list type="bullet">
///   <item>A <em>turn</em> pairs an assistant <c>tool_use</c> message with the <b>next</b>
///   <c>tool_result</c> message; pairing is positional — the implementation does not match
///   by <c>tool_use_id</c>.</item>
///   <item>If an assistant message contains multiple <c>tool_use</c> blocks, the turn is
///   attributed to the <b>first</b> tool's name (one turn per assistant message in v1); a
///   message carrying both a <c>tool_use</c> and a <c>tool_result</c> is treated as the call.</item>
///   <item>A <c>tool_result</c> with no open turn (e.g. an unmatched/leading result, or a
///   non-tool message interleaved between a call and its result) becomes a <c>"—"</c> row.
///   Its tokens still count in <c>Added</c>/<c>Cumulative</c> — so the <c>% window</c>
///   central metric stays correct — but it is not attributed to <c>ResponsesTotal</c>.</item>
///   <item>Cost is approximately <c>2N+2</c> <c>CountAsync</c> calls for N messages, each
///   re-encoding the full tool list — <c>O(N·|tools|)</c>, acceptable for typical short
///   recorded transcripts; batching is a possible later optimisation.</item>
/// </list>
/// </remarks>
public sealed class SessionCostMeasurer
{
    private readonly ITokenCounter _counter;

    public SessionCostMeasurer(ITokenCounter counter) => _counter = counter;

    public async Task<SessionCostReport> MeasureAsync(
        SessionTranscript transcript, MeasurementOptions options, CancellationToken cancellationToken = default)
    {
        var tools = transcript.Tools;
        var messages = transcript.Messages;
        var window = options.ContextWindowTokens;

        var emptyBaseline = await _counter.CountAsync(options.Model, CountInput.Empty, cancellationToken).ConfigureAwait(false);
        var start = await _counter.CountAsync(options.Model, CountInput.ForTools(tools), cancellationToken).ConfigureAwait(false);
        var schemaTokens = Math.Max(0, start - emptyBaseline);

        var builders = new List<TurnBuilder>();
        TurnBuilder? current = null;
        var prev = start;

        for (var i = 0; i < messages.Count; i++)
        {
            var prefix = messages.Take(i + 1).ToArray();
            var cumulative = await _counter
                .CountAsync(options.Model, new CountInput { Tools = tools, Messages = prefix }, cancellationToken)
                .ConfigureAwait(false);
            var added = Math.Max(0, cumulative - prev);
            prev = cumulative;

            var (hasUse, toolName, hasResult) = Classify(messages[i]);

            if (hasUse)
            {
                current = new TurnBuilder { Tool = toolName ?? "(tool)", Call = added, Added = added, Cumulative = cumulative };
                builders.Add(current);
            }
            else if (hasResult && current is { ResponseSet: false })
            {
                current.Response = added;
                current.ResponseSet = true;
                current.Added += added;
                current.Cumulative = cumulative;
            }
            else
            {
                builders.Add(new TurnBuilder { Tool = "—", Added = added, Cumulative = cumulative });
                current = null;
            }
        }

        var turns = new List<TurnCost>(builders.Count);
        for (var i = 0; i < builders.Count; i++)
        {
            var b = builders[i];
            turns.Add(new TurnCost(i + 1, b.Tool, b.Call, b.Response, b.Added, b.Cumulative, Percent(b.Cumulative, window)));
        }

        var callsTotal = builders.Sum(b => b.Call);
        var responsesTotal = builders.Sum(b => b.Response);
        var peak = prev; // context is append-only, so the last cumulative is the peak (== start when there are no messages)

        return new SessionCostReport
        {
            ModelId = options.Model,
            Mode = _counter.Mode,
            CounterLabel = _counter.Label,
            TurnCount = builders.Count,
            SchemaTokens = schemaTokens,
            Turns = turns,
            CallsTotal = callsTotal,
            ResponsesTotal = responsesTotal,
            PeakContextTokens = peak,
            ContextWindowTokens = window,
            PeakPercentWindow = Percent(peak, window),
            ResponseToSchemaRatio = schemaTokens > 0 ? (double)responsesTotal / schemaTokens : 0.0,
            ResponseShareOfContext = peak > 0 ? (double)responsesTotal / peak : 0.0,
        };
    }

    private static double Percent(int tokens, int window) =>
        window > 0 ? (double)tokens / window * 100.0 : 0.0;

    private static (bool HasToolUse, string? ToolName, bool HasToolResult) Classify(TranscriptMessage message)
    {
        string? name = null;
        var use = false;
        var result = false;
        foreach (var block in message.Content)
        {
            if (block is ToolUseBlock u)
            {
                use = true;
                name ??= u.Name; // first tool_use wins (one turn per assistant message in v1)
            }
            else if (block is ToolResultBlock)
            {
                result = true;
            }
        }

        return (use, name, result);
    }

    private sealed class TurnBuilder
    {
        public string Tool = "—";
        public int Call;
        public int Response;
        public bool ResponseSet;
        public int Added;
        public int Cumulative;
    }
}
