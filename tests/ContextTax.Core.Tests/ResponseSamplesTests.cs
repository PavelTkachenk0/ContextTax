using System.Runtime.CompilerServices;
using ContextTax.Cli.Support;
using ContextTax.Core.Counting;
using ContextTax.Core.Measurement;
using Xunit;

namespace ContextTax.Core.Tests;

public class ResponseSamplesTests
{
    // tests/ContextTax.Core.Tests/ResponseSamplesTests.cs  →  ../../  →  repo root
    private static string RepoRoot([CallerFilePath] string thisFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", ".."));

    private static string Sample(string name) =>
        Path.Combine(RepoRoot(), "samples", "responses", name);

    [Fact]
    public async Task Estimate_diff_of_weather_samples_shows_a_leaner_after()
    {
        var runner = MeasurementRunner.Default(TimeSpan.FromSeconds(30));
        var counter = EstimateTokenCounter.CreateO200k();
        var options = new MeasurementOptions { Model = Defaults.Model };

        var result = await runner.RunResponseDeltaAsync(
            Sample("weather.before.json"), Sample("weather.after.json"), options, counter);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(result.Report!.Before.ResponseTokens > result.Report!.After.ResponseTokens);
        Assert.True(result.Report!.DeltaTokens < 0);
        Assert.Equal(MeasurementMode.Estimate, result.Report!.Mode);
    }
}
