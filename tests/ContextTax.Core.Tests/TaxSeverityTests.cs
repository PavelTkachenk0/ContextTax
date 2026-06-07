using ContextTax.Cli.Rendering;
using Xunit;

namespace ContextTax.Core.Tests;

public class TaxSeverityTests
{
    [Theory]
    [InlineData(0.0, TaxLevel.Low)]
    [InlineData(4.9, TaxLevel.Low)]
    [InlineData(5.0, TaxLevel.Medium)]
    [InlineData(9.9, TaxLevel.Medium)]
    [InlineData(10.0, TaxLevel.High)]
    public void Of_classifies_by_threshold(double percent, TaxLevel expected) =>
        Assert.Equal(expected, TaxSeverity.Of(percent));

    [Theory]
    [InlineData(1.0, "green")]
    [InlineData(7.0, "yellow")]
    [InlineData(15.0, "red")]
    public void Color_maps_level_to_spectre_colour(double percent, string expected) =>
        Assert.Equal(expected, TaxSeverity.Color(percent));
}
