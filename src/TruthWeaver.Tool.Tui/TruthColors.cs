namespace TruthWeaver.Tool.Tui;

using Terminal.Gui.Drawing;

/// <summary>The color of a truth value in the trace: green for True, red for False, yellow for Unknown and gray for a skipped node.</summary>
internal static class TruthColors
{
    /// <summary>Gets the foreground color of a value.</summary>
    /// <param name="value">The value, or <see langword="null"/> for a node that short-circuiting skipped.</param>
    /// <returns>The color.</returns>
    public static Color Of(TruthValue? value)
    {
        return value switch
        {
            TruthValue.True => new Color(ColorName16.BrightGreen),
            TruthValue.False => new Color(ColorName16.BrightRed),
            TruthValue.Unknown => new Color(ColorName16.BrightYellow),
            _ => new Color(ColorName16.DarkGray),
        };
    }

    /// <summary>Gets a scheme that draws a tree row in the color of a value, and the selected row reversed.</summary>
    /// <param name="value">The value, or <see langword="null"/> for a skipped node.</param>
    /// <param name="background">The background of the view.</param>
    /// <returns>The scheme.</returns>
    public static Scheme SchemeOf(TruthValue? value, Color background)
    {
        Color color = Of(value);
        Attribute normal = new(color, background);
        Attribute selected = new(background, color);
        return new Scheme
        {
            Normal = normal,
            Focus = selected,
            Active = selected,
        };
    }
}
