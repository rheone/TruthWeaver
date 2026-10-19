namespace TruthWeaver.Tool.Tui.Workbench;

using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using TruthWeaver.Tool.Tui.Theme;

/// <summary>
/// Applies a theme to the whole terminal library, so every pane and every dialog draws with it. Terminal.Gui keeps its
/// themes and schemes in process-wide state, and a view reads them when it draws.
/// </summary>
/// <remarks>
/// The built-in Terminal.Gui themes leave the background of the panes as <see cref="Color.None"/>, so the panes take the
/// background of the terminal and the light theme draws gray text on a dark terminal. The workbench therefore replaces
/// every scheme with one that paints the background of its <see cref="ThemePalette"/>. A scheme added at run time survives a
/// theme switch, so each call replaces all of them again.
/// </remarks>
internal static class WorkbenchAppearance
{
    /// <summary>Applies a theme.</summary>
    /// <param name="theme">The theme: it selects the palette, and the matching Terminal.Gui theme supplies the borders and the button layout.</param>
    /// <param name="noColor"><see langword="true"/> to replace every scheme with <see cref="MonochromeScheme"/>.</param>
    public static void Apply(ThemeVariant theme, bool noColor)
    {
        ThemeManager.Theme = theme == ThemeVariant.Light ? "Light" : "Dark";
        ThemePalette palette = ThemePalette.For(theme, noColor);
        foreach (string name in SchemeManager.GetSchemeNames())
        {
            SchemeManager.AddScheme(name, SchemeFor(name, palette));
        }
    }

    /// <summary>
    /// Gets the scheme that replaces a Terminal.Gui scheme. The dialogs and the menus paint <see cref="ThemePalette.Surface"/>,
    /// the error scheme writes in <see cref="ThemePalette.Error"/>, and every other scheme paints
    /// <see cref="ThemePalette.Background"/>. A palette with no color gives <see cref="MonochromeScheme"/>.
    /// </summary>
    /// <param name="name">The name of the Terminal.Gui scheme, such as <c>Base</c> or <c>Dialog</c>.</param>
    /// <param name="palette">The colors of the theme.</param>
    /// <returns>The scheme.</returns>
    public static Scheme SchemeFor(string name, ThemePalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);

        if (!palette.HasColor)
        {
            return MonochromeScheme();
        }

        return name switch
        {
            "Dialog" or "Menu" or "Accent" => Painted(palette, palette.Surface, palette.Text),
            "Error" => Painted(palette, palette.Surface, palette.Error),
            _ => Painted(palette, palette.Background, palette.Text),
        };
    }

    /// <summary>
    /// Gets a scheme with no color: the terminal defaults everywhere, and the focus, hot keys and read-only text told apart by
    /// style (reverse, underline, faint).
    /// </summary>
    /// <returns>The scheme.</returns>
    public static Scheme MonochromeScheme()
    {
        static Attribute Plain(TextStyle style = TextStyle.None)
        {
            return new Attribute(Color.None, Color.None, style);
        }

        return new Scheme
        {
            Normal = Plain(),
            HotNormal = Plain(TextStyle.Underline),
            Focus = Plain(TextStyle.Reverse),
            HotFocus = Plain(TextStyle.Reverse | TextStyle.Underline),
            Active = Plain(TextStyle.Reverse),
            HotActive = Plain(TextStyle.Reverse | TextStyle.Underline),
            Highlight = Plain(TextStyle.Reverse),
            Editable = Plain(),
            ReadOnly = Plain(TextStyle.Faint),
            Disabled = Plain(TextStyle.Faint),
        };
    }

    // Sets every role explicitly: a role that Terminal.Gui derives on its own resolves Color.None against the terminal
    // colors, which would bring the terminal background back.
    private static Scheme Painted(ThemePalette palette, Color background, Color foreground)
    {
        Attribute normal = new(foreground, background);
        Attribute focus = new(palette.Text, palette.Selection);
        return new Scheme
        {
            Normal = normal,
            HotNormal = new Attribute(foreground, background, TextStyle.Underline),
            Focus = focus,
            HotFocus = new Attribute(palette.Text, palette.Selection, TextStyle.Underline),
            Active = new Attribute(palette.Text, palette.Selection, TextStyle.Bold),
            HotActive = new Attribute(palette.Text, palette.Selection, TextStyle.Bold | TextStyle.Underline),
            Highlight = focus,
            Editable = new Attribute(palette.Text, background),
            ReadOnly = new Attribute(palette.Skipped, background),
            Disabled = new Attribute(palette.Skipped, background),
            Code = new Attribute(palette.Text, background),
        };
    }
}
