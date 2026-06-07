using ContextTax.Cli.Support;
using Xunit;

namespace ContextTax.Core.Tests;

public class VersionInfoTests
{
    [Theory]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("1.2.3+abcdef0", "1.2.3")]                  // SourceLink build metadata stripped
    [InlineData("1.0.0-preview.6+deadbeef", "1.0.0-preview.6")]
    [InlineData(null, "0.0.0")]
    [InlineData("", "0.0.0")]
    [InlineData("   ", "0.0.0")]
    public void Normalize_strips_build_metadata_and_defaults(string? input, string expected)
        => Assert.Equal(expected, VersionInfo.Normalize(input));
}
