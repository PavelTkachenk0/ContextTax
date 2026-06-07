namespace ContextTax.Cli.Rendering;

/// <summary>Visual severity bucket for a context-tax percentage (presentation only).</summary>
public enum TaxLevel { Low, Medium, High }

/// <summary>The one place CLI tax-severity thresholds + colours live.</summary>
public static class TaxSeverity
{
    /// <param name="percentWindow">A percent value, e.g. 12.4 for 12.4% of the window.</param>
    public static TaxLevel Of(double percentWindow) =>
        percentWindow < 5.0 ? TaxLevel.Low
        : percentWindow < 10.0 ? TaxLevel.Medium
        : TaxLevel.High;

    /// <summary>A Spectre markup colour name for the level.</summary>
    public static string Color(double percentWindow) =>
        Of(percentWindow) switch
        {
            TaxLevel.Low => "green",
            TaxLevel.Medium => "yellow",
            _ => "red",
        };
}
