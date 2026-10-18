namespace TruthWeaver.Tool.Tui.Theme;

using Terminal.Gui.Drawing;

/// <summary>
/// How the trace shows a truth value. The color comes from a <see cref="ThemePalette"/> and is never the only mark: the text
/// of a trace line always starts with <see cref="Mark"/>, and a term in the list starts with <see cref="Letter"/>. With
/// <c>NO_COLOR</c> the palette has no color, and the text still tells the values apart.
/// </summary>
internal static class TruthColors
{
    /// <summary>The text mark of a node that short-circuiting skipped.</summary>
    public const string SkippedMark = "skipped";

    /// <summary>Gets the text mark of a value: the name of the value, or <see cref="SkippedMark"/>.</summary>
    /// <param name="value">The value, or <see langword="null"/> for a node that short-circuiting skipped.</param>
    /// <returns>The mark.</returns>
    public static string Mark(TruthValue? value)
    {
        return value is { } known ? known.ToString() : SkippedMark;
    }

    /// <summary>Gets the one-letter mark of a value: <c>T</c>, <c>F</c> or <c>U</c>, and <c>-</c> for a skipped node.</summary>
    /// <param name="value">The value, or <see langword="null"/> for a node that short-circuiting skipped.</param>
    /// <returns>The letter.</returns>
    public static char Letter(TruthValue? value)
    {
        return value switch
        {
            TruthValue.True => 'T',
            TruthValue.False => 'F',
            TruthValue.Unknown => 'U',
            _ => '-',
        };
    }

    /// <summary>Gets a scheme that draws a tree row in the color of a value, and the selected row reversed.</summary>
    /// <param name="value">The value, or <see langword="null"/> for a skipped node.</param>
    /// <param name="palette">The colors of the theme.</param>
    /// <param name="background">The background of the view.</param>
    /// <returns>The scheme.</returns>
    public static Scheme SchemeOf(TruthValue? value, ThemePalette palette, Color background)
    {
        ArgumentNullException.ThrowIfNull(palette);

        Attribute normal;
        Attribute selected;
        if (palette.HasColor)
        {
            Color color = palette.Of(value);
            normal = new Attribute(color, background);
            selected = new Attribute(background, color);
        }
        else
        {
            // No color: the row keeps the terminal defaults, and the selected row is reversed.
            normal = new Attribute(Color.None, Color.None);
            selected = new Attribute(Color.None, Color.None, TextStyle.Reverse);
        }

        return new Scheme
        {
            Normal = normal,
            Focus = selected,
            Active = selected,
        };
    }
}
