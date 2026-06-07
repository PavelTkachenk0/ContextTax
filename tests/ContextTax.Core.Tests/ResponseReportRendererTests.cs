using System.Globalization;
using System.Text.Json;
using ContextTax.Cli.Rendering;
using ContextTax.Core.Measurement;
using Spectre.Console.Testing;
using Xunit;

namespace ContextTax.Core.Tests;

public class ResponseReportRendererTests
{
    private static ResponseCostReport Single => new()
    {
        Source = "weather.before.json",
        ModelId = "m1",
        Mode = MeasurementMode.Estimate,
        CounterLabel = "o200k_base (offline)",
        ResponseTokens = 1_847,
        ContextWindowTokens = 200_000,
        PercentWindow = 0.92,
    };

    private static ResponseDiffReport Diff()
    {
        var before = Single;
        var after = before with { Source = "weather.after.json", ResponseTokens = 612, PercentWindow = 0.31 };
        return ResponseDiff.Between(before, after);   // delta -1235 tok, -66.86% → "-66.9"
    }

    [Fact]
    public void RenderJson_single_emits_tokens_mode_source()
    {
        var json = ResponseReportRenderer.RenderJson(Single);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(1_847, root.GetProperty("ResponseTokens").GetInt32());
        Assert.Equal("Estimate", root.GetProperty("Mode").GetString());
        Assert.Equal("weather.before.json", root.GetProperty("Source").GetString());
    }

    [Fact]
    public void RenderJson_diff_emits_delta()
    {
        var json = ResponseReportRenderer.RenderJson(Diff());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(-1_235, root.GetProperty("DeltaTokens").GetInt32());
        Assert.Equal(612, root.GetProperty("After").GetProperty("ResponseTokens").GetInt32());
    }

    [Fact]
    public void RenderCard_shows_badge_tokens_and_a_colour()
    {
        var console = new TestConsole().EmitAnsiSequences();
        ResponseReportRenderer.RenderCard(Single, console);
        var output = console.Output;
        Assert.Contains("ESTIMATE", output);
        Assert.Contains("1,847", output);
        Assert.Contains("[", output);     // an ANSI colour sequence was emitted
    }

    [Fact]
    public void RenderDiffCard_headline_is_green_with_ascii_minus_when_leaner()
    {
        var console = new TestConsole().EmitAnsiSequences();
        ResponseReportRenderer.RenderDiffCard(Diff(), console);
        var output = console.Output;
        Assert.Contains("saved", output);
        Assert.Contains("1,235", output);
        Assert.Contains("-66.9", output);        // ASCII minus + invariant decimal
        Assert.DoesNotContain("−" + "66.9", output);  // NOT the Unicode minus (U+2212)
        Assert.Contains("█", output);        // a TokenBar block is present
    }

    [Fact]
    public void RenderDiffCard_zero_before_shows_added_and_na()
    {
        var before = Single with { ResponseTokens = 0, PercentWindow = 0.0 };
        var after = Single with { Source = "after.json", ResponseTokens = 400, PercentWindow = 0.2 };
        var console = new TestConsole();
        ResponseReportRenderer.RenderDiffCard(ResponseDiff.Between(before, after), console);
        var output = console.Output;
        Assert.Contains("added", output);
        Assert.Contains("n/a", output);
    }

    [Fact]
    public void RenderCard_formats_invariantly_under_comma_locale()
    {
        var prev = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
        try
        {
            var console = new TestConsole();
            ResponseReportRenderer.RenderCard(Single, console);
            var output = console.Output;
            Assert.Contains("1,847", output);
            Assert.Contains("0.9", output);
            Assert.DoesNotContain("0,9", output);
        }
        finally
        {
            CultureInfo.CurrentCulture = prev;
        }
    }
}
