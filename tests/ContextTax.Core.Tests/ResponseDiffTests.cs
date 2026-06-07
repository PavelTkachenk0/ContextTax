using ContextTax.Core.Measurement;
using Xunit;

namespace ContextTax.Core.Tests;

public class ResponseDiffTests
{
    private static ResponseCostReport Report(string source, int tokens, double pct) => new()
    {
        Source = source,
        ModelId = "m1",
        Mode = MeasurementMode.Estimate,
        CounterLabel = "o200k",
        ResponseTokens = tokens,
        ContextWindowTokens = 200_000,
        PercentWindow = pct,
    };

    [Fact]
    public void Between_computes_signed_token_and_percent_delta()
    {
        var before = Report("before.json", 1_000, 0.5);
        var after = Report("after.json", 400, 0.2);

        var diff = ResponseDiff.Between(before, after);

        Assert.Equal(-600, diff.DeltaTokens);
        Assert.Equal(-60.0, diff.DeltaPercent!.Value, 6);
        Assert.Same(before, diff.Before);
        Assert.Same(after, diff.After);
        Assert.Equal(MeasurementMode.Estimate, diff.Mode);
        Assert.Equal(200_000, diff.ContextWindowTokens);
    }

    [Fact]
    public void Between_reports_increase_as_positive()
    {
        var diff = ResponseDiff.Between(Report("a", 400, 0.2), Report("b", 1_000, 0.5));
        Assert.Equal(600, diff.DeltaTokens);
        Assert.Equal(150.0, diff.DeltaPercent!.Value, 6);
    }

    [Fact]
    public void Between_guards_zero_before_with_null_percent()
    {
        var diff = ResponseDiff.Between(Report("a", 0, 0.0), Report("b", 400, 0.2));
        Assert.Equal(400, diff.DeltaTokens);
        Assert.Null(diff.DeltaPercent);
    }
}
