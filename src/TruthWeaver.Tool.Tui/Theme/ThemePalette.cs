namespace TruthWeaver.Tool.Tui.Theme;

using Terminal.Gui.Drawing;
using TruthWeaver.Diagnostics;

/// <summary>
/// The colors of one theme: the truth values in the trace and the diagnostic severities in the editor. Every color keeps a
/// contrast ratio of at least <see cref="MinimumContrast"/> against the background it was chosen for. A palette made for
/// <c>NO_COLOR</c> has no color at all: it draws with the terminal defaults, and <see cref="StyleOf"/> tells the severities apart.
/// </summary>
/// <param name="Background">The background that the colors were chosen for. It is a reference: the workbench draws on the terminal background.</param>
/// <param name="True">The color of <c>True</c>.</param>
/// <param name="False">The color of <c>False</c>.</param>
/// <param name="Unknown">The color of <c>Unknown</c>.</param>
/// <param name="Skipped">The color of a node that short-circuiting skipped.</param>
/// <param name="Error">The color of an error in the editor.</param>
/// <param name="Warning">The color of a warning in the editor.</param>
/// <param name="Info">The color of information in the editor.</param>
/// <param name="HasColor"><see langword="false"/> for the palette of <c>NO_COLOR</c>.</param>
internal sealed record ThemePalette(
    Color Background,
    Color True,
    Color False,
    Color Unknown,
    Color Skipped,
    Color Error,
    Color Warning,
    Color Info,
    bool HasColor
)
{
    /// <summary>The smallest contrast ratio (WCAG) that a color keeps against <see cref="Background"/>.</summary>
    public const double MinimumContrast = 4.5;

    /// <summary>Gets the palette for light terminals: dark colors on white.</summary>
    public static ThemePalette Light { get; } =
        new(
            Background: new Color(255, 255, 255),
            True: new Color(0, 110, 0),
            False: new Color(180, 0, 0),
            Unknown: new Color(140, 90, 0),
            Skipped: new Color(96, 96, 96),
            Error: new Color(180, 0, 0),
            Warning: new Color(140, 90, 0),
            Info: new Color(0, 95, 135),
            HasColor: true
        );

    /// <summary>Gets the palette for dark terminals: bright colors on black.</summary>
    public static ThemePalette Dark { get; } =
        new(
            Background: new Color(0, 0, 0),
            True: new Color(80, 255, 80),
            False: new Color(255, 95, 95),
            Unknown: new Color(255, 215, 0),
            Skipped: new Color(150, 150, 150),
            Error: new Color(255, 95, 95),
            Warning: new Color(255, 215, 0),
            Info: new Color(95, 215, 255),
            HasColor: true
        );

    /// <summary>Gets the palette with no color, for <c>NO_COLOR</c>: every color is the terminal default.</summary>
    public static ThemePalette Plain { get; } =
        new(
            Background: Color.None,
            True: Color.None,
            False: Color.None,
            Unknown: Color.None,
            Skipped: Color.None,
            Error: Color.None,
            Warning: Color.None,
            Info: Color.None,
            HasColor: false
        );

    /// <summary>Gets the palette for a theme.</summary>
    /// <param name="theme">The theme.</param>
    /// <param name="noColor"><see langword="true"/> when the author asked for no color (<c>NO_COLOR</c>).</param>
    /// <returns>The palette.</returns>
    public static ThemePalette For(ThemeVariant theme, bool noColor)
    {
        if (noColor)
        {
            return Plain;
        }

        return theme == ThemeVariant.Light ? Light : Dark;
    }

    /// <summary>Gets the WCAG contrast ratio between two opaque colors, from 1 to 21.</summary>
    /// <param name="first">One color.</param>
    /// <param name="second">The other color.</param>
    /// <returns>The ratio.</returns>
    public static double ContrastRatio(Color first, Color second)
    {
        double a = Luminance(first);
        double b = Luminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    /// <summary>Gets the color of a value in the trace.</summary>
    /// <param name="value">The value, or <see langword="null"/> for a node that short-circuiting skipped.</param>
    /// <returns>The color.</returns>
    public Color Of(TruthValue? value)
    {
        return value switch
        {
            TruthValue.True => this.True,
            TruthValue.False => this.False,
            TruthValue.Unknown => this.Unknown,
            _ => this.Skipped,
        };
    }

    /// <summary>Gets the color of a diagnostic severity in the editor.</summary>
    /// <param name="severity">The severity.</param>
    /// <returns>The color.</returns>
    public Color Of(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => this.Error,
            DiagnosticSeverity.Warning => this.Warning,
            _ => this.Info,
        };
    }

    /// <summary>
    /// Gets the text style of a marked span. It is an underline. Without color it also tells the severities apart: an error
    /// is bold and information is faint.
    /// </summary>
    /// <param name="severity">The severity.</param>
    /// <returns>The style.</returns>
    public TextStyle StyleOf(DiagnosticSeverity severity)
    {
        if (this.HasColor)
        {
            return TextStyle.Underline;
        }

        return severity switch
        {
            DiagnosticSeverity.Error => TextStyle.Underline | TextStyle.Bold,
            DiagnosticSeverity.Warning => TextStyle.Underline,
            _ => TextStyle.Underline | TextStyle.Faint,
        };
    }

    private static double Luminance(Color color)
    {
        static double Channel(int value)
        {
            double s = value / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(color.R)) + (0.7152 * Channel(color.G)) + (0.0722 * Channel(color.B));
    }
}
