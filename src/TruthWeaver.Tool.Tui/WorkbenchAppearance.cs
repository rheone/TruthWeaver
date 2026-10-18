namespace TruthWeaver.Tool.Tui;

using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;

/// <summary>
/// Applies a theme to the whole terminal library, so every pane and every dialog draws with it. Terminal.Gui keeps its
/// themes and schemes in process-wide state, and a view reads them when it draws.
/// </summary>
internal static class WorkbenchAppearance
{
    /// <summary>Applies a theme.</summary>
    /// <param name="theme">The theme: the matching Terminal.Gui theme supplies the colors of the panes and dialogs.</param>
    /// <param name="noColor"><see langword="true"/> to replace every scheme with <see cref="MonochromeScheme"/>.</param>
    public static void Apply(ThemeVariant theme, bool noColor)
    {
        ThemeManager.Theme = theme == ThemeVariant.Light ? "Light" : "Dark";
        if (noColor)
        {
            Scheme plain = MonochromeScheme();
            foreach (string name in SchemeManager.GetSchemeNames())
            {
                SchemeManager.AddScheme(name, plain);
            }
        }
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
}
