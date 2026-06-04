namespace ContextTax.Core;

/// <summary>
/// Static product metadata. Also the liveness marker that keeps the skeleton testable.
/// </summary>
public static class ContextTaxInfo
{
    public const string Name = "ContextTax";

    public const string Tagline =
        "Measure the context-window tax an MCP server imposes — before any useful work.";
}
