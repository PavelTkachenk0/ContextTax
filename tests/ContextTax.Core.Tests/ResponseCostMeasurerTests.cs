using ContextTax.Core.Counting;
using ContextTax.Core.Measurement;
using ContextTax.Core.Transcript;
using Xunit;

namespace ContextTax.Core.Tests;

public class ResponseCostMeasurerTests
{
    // Captures the CountInput snapshots to assert the synthetic turn's shape.
    private sealed class CapturingCounter : ITokenCounter
    {
        public List<CountInput> Inputs { get; } = new();
        public MeasurementMode Mode => MeasurementMode.Estimate;
        public string Label => "cap";
        public Task<int> CountAsync(string model, CountInput input, CancellationToken ct = default)
        {
            Inputs.Add(input);
            return Task.FromResult(input.Messages?.Count ?? 0);
        }
    }

    [Fact]
    public async Task Measure_returns_the_tool_result_token_delta()
    {
        // FakeTokenCounter weight of a ToolResultBlock == content.ToJsonString().Length.
        // For payload "hello": JsonValue.Create("hello").ToJsonString() == "\"hello\"" (7 chars).
        var counter = new FakeTokenCounter(MeasurementMode.Estimate, "fake");
        var measurer = new ResponseCostMeasurer(counter);

        var report = await measurer.MeasureAsync(
            "weather.json", "hello", new MeasurementOptions { Model = "m1", ContextWindowTokens = 200_000 });

        Assert.Equal(7, report.ResponseTokens);
        Assert.Equal("weather.json", report.Source);
        Assert.Equal("m1", report.ModelId);
        Assert.Equal(MeasurementMode.Estimate, report.Mode);
        Assert.Equal("fake", report.CounterLabel);
        Assert.Equal(200_000, report.ContextWindowTokens);
        Assert.Equal(7.0 / 200_000 * 100, report.PercentWindow, 6);
    }

    [Fact]
    public async Task Measure_builds_a_synthetic_tool_result_turn_with_string_content()
    {
        var counter = new CapturingCounter();

        await new ResponseCostMeasurer(counter).MeasureAsync("s", "PAYLOAD", new MeasurementOptions { Model = "m1" });

        Assert.Equal(2, counter.Inputs.Count);           // baseline, then with-result
        var withResult = counter.Inputs[1];
        Assert.NotNull(withResult.Messages);
        Assert.Equal(3, withResult.Messages!.Count);     // user "." , assistant tool_use, user tool_result
        var last = Assert.IsType<ToolResultBlock>(withResult.Messages[2].Content[0]);
        Assert.Equal("\"PAYLOAD\"", last.Content.ToJsonString());   // payload measured AS A STRING
    }

    [Fact]
    public async Task Measure_clamps_negative_delta_to_zero()
    {
        // A counter that returns a smaller "with-result" than baseline must not yield a negative count.
        var counter = new DecreasingCounter();
        var report = await new ResponseCostMeasurer(counter).MeasureAsync("s", "x", new MeasurementOptions { Model = "m1" });
        Assert.Equal(0, report.ResponseTokens);
    }

    private sealed class DecreasingCounter : ITokenCounter
    {
        private int _n = 100;
        public MeasurementMode Mode => MeasurementMode.Estimate;
        public string Label => "dec";
        public Task<int> CountAsync(string model, CountInput input, CancellationToken ct = default)
            => Task.FromResult(_n -= 50);   // 50 then 0 → delta = -50
    }
}
