namespace ContextTax.Core.Measurement;

/// <summary>Pure before/after comparison of two <see cref="ResponseCostReport"/>s. Shared
/// provenance (model/mode/label/window) is taken from <paramref name="after"/>; both sides are
/// produced by the same counter + options, so they match.</summary>
public static class ResponseDiff
{
    public static ResponseDiffReport Between(ResponseCostReport before, ResponseCostReport after)
    {
        var deltaTokens = after.ResponseTokens - before.ResponseTokens;
        double? deltaPercent = before.ResponseTokens > 0
            ? (double)deltaTokens / before.ResponseTokens * 100.0
            : null;

        return new ResponseDiffReport
        {
            ModelId = after.ModelId,
            Mode = after.Mode,
            CounterLabel = after.CounterLabel,
            ContextWindowTokens = after.ContextWindowTokens,
            Before = before,
            After = after,
            DeltaTokens = deltaTokens,
            DeltaPercent = deltaPercent,
        };
    }
}
