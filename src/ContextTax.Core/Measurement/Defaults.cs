namespace ContextTax.Core.Measurement;

/// <summary>
/// Defaults that drift over time (model IDs, prices). Kept in one place and easy to bump.
/// </summary>
public static class Defaults
{
    // Bump this if count_tokens returns 400/404 for an unknown model.
    public const string Model = "claude-sonnet-4-5";
    public const int ContextWindowTokens = 200_000;
    public const double InputPricePerMTokUsd = 3.00;
}
