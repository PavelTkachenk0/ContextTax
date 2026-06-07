namespace ContextTax.Cli.Rendering;

/// <summary>Renders a fixed-width ASCII bar (█ filled, ░ empty) for a value's share of a max.</summary>
public static class TokenBar
{
    public static string Render(int value, int max, int width)
    {
        if (width <= 0)
            return string.Empty;
        if (max <= 0 || value <= 0)
            return new string('░', width);

        var filled = (int)Math.Round((double)value / max * width, MidpointRounding.AwayFromZero);
        filled = Math.Clamp(filled, 0, width);
        return new string('█', filled) + new string('░', width - filled);
    }
}
