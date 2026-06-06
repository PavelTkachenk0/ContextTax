using ContextTax.Cli.Support;
using ContextTax.Core.Counting;
using ContextTax.Core.Measurement;
using Xunit;

namespace ContextTax.Core.Tests;

public class CounterFactoryTests
{
    [Fact]
    public void Estimate_selects_offline_counter_no_key_needed()
    {
        using var result = CounterFactory.Create(estimate: true, apiKey: null);

        Assert.NotNull(result.Counter);
        Assert.Equal(MeasurementMode.Estimate, result.Counter!.Mode);
    }

    [Fact]
    public void Ground_truth_with_key_selects_anthropic_counter()
    {
        using var result = CounterFactory.Create(estimate: false, apiKey: "k");

        Assert.NotNull(result.Counter);
        Assert.Equal(MeasurementMode.GroundTruth, result.Counter!.Mode);
    }

    [Fact]
    public void Ground_truth_without_key_returns_null_counter_and_a_hint()
    {
        using var result = CounterFactory.Create(estimate: false, apiKey: null);

        Assert.Null(result.Counter);
        Assert.Contains("--estimate", result.Error);
    }
}
