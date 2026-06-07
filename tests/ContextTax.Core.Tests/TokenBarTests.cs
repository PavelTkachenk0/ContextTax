using ContextTax.Cli.Rendering;
using Xunit;

namespace ContextTax.Core.Tests;

public class TokenBarTests
{
    [Fact]
    public void Render_full_when_value_equals_max() =>
        Assert.Equal("██████████", TokenBar.Render(100, 100, 10));

    [Fact]
    public void Render_empty_when_value_zero() =>
        Assert.Equal("░░░░░░░░░░", TokenBar.Render(0, 100, 10));

    [Fact]
    public void Render_half_when_value_half() =>
        Assert.Equal("█████░░░░░", TokenBar.Render(50, 100, 10));

    [Fact]
    public void Render_clamps_when_value_exceeds_max() =>
        Assert.Equal("██████████", TokenBar.Render(200, 100, 10));

    [Fact]
    public void Render_empty_string_when_no_max() =>
        Assert.Equal("░░░░░░░░░░", TokenBar.Render(5, 0, 10));
}
