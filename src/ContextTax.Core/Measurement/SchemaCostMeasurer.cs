using ContextTax.Core.Counting;
using ContextTax.Core.Mcp;

namespace ContextTax.Core.Measurement;

/// <summary>
/// Computes the static schema-token cost of a set of MCP tools using marginal deltas
/// over an <see cref="ITokenCounter"/>. Pure orchestration: no I/O of its own.
/// </summary>
public sealed class SchemaCostMeasurer
{
    private readonly ITokenCounter _counter;

    public SchemaCostMeasurer(ITokenCounter counter) => _counter = counter;

    public async Task<SchemaCostReport> MeasureAsync(
        IReadOnlyList<McpTool> tools, MeasurementOptions options, CancellationToken cancellationToken = default)
    {
        var baseline = await _counter.CountAsync(options.Model, CountInput.Empty, cancellationToken).ConfigureAwait(false);

        var total = 0;
        var perTool = new List<ToolCost>(tools.Count);

        if (tools.Count > 0)
        {
            var withAll = await _counter.CountAsync(options.Model, CountInput.ForTools(tools), cancellationToken).ConfigureAwait(false);
            total = Math.Max(0, withAll - baseline);

            foreach (var tool in tools)
            {
                var withOne = await _counter.CountAsync(options.Model, CountInput.ForTools(new[] { tool }), cancellationToken).ConfigureAwait(false);
                perTool.Add(new ToolCost(tool.Name, Math.Max(0, withOne - baseline)));
            }

            perTool.Sort((a, b) => b.Tokens.CompareTo(a.Tokens));
        }

        var percent = options.ContextWindowTokens > 0
            ? (double)total / options.ContextWindowTokens * 100.0
            : 0.0;

        return new SchemaCostReport
        {
            ModelId = options.Model,
            Mode = _counter.Mode,
            CounterLabel = _counter.Label,
            ToolCount = tools.Count,
            TotalSchemaTokens = total,
            PerTool = perTool,
            ContextWindowTokens = options.ContextWindowTokens,
            ContextWindowPercent = percent,
            DollarCost = total / 1_000_000.0 * options.InputPricePerMTokUsd,
        };
    }
}
