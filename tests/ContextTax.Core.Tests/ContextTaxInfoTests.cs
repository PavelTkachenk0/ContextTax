using ContextTax.Core;
using Xunit;

namespace ContextTax.Core.Tests;

public class ContextTaxInfoTests
{
    [Fact]
    public void Name_is_ContextTax()
    {
        Assert.Equal("ContextTax", ContextTaxInfo.Name);
    }

    [Fact]
    public void Tagline_is_not_empty()
    {
        Assert.False(string.IsNullOrWhiteSpace(ContextTaxInfo.Tagline));
    }
}
