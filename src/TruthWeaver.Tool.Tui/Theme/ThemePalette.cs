namespace TruthWeaver.Tool.Tui.Theme;

using Terminal.Gui.Drawing;
using TruthWeaver.Diagnostics;

/// <summary>
/// The colors of one theme: the backgrounds and the text of the panes and dialogs, the truth values in the trace and the
/// diagnostic severities in the editor. Every text color keeps a contrast ratio of at least <see cref="MinimumContrast"/>
/// against each of the three backgrounds. A palette made for <c>NO_COLOR</c> has no color at all: it draws with the terminal
/// defaults, and <see cref="StyleOf(DiagnosticSeverity)"/> tells the severities apart.
/// </summary>
/// <param name="Background">The background that the workbench paints in every pane.</param>
/// <param name="Surface">The background of the dialogs and the menus, a little apart from <paramref name="Background"/>.</param>
/// <param name="Selection">The background of the focused or selected element.</param>
/// <param name="Text">The color of plain text.</param>
/// <param name="True">The color of <c>True</c>.</param>
/// <param name="False">The color of <c>False</c>.</param>
/// <param name="Unknown">The color of <c>Unknown</c>.</param>
/// <param name="Skipped">The color of a node that short-circuiting skipped.</param>
/// <param name="Error">The color of an error in the editor.</param>
/// <param name="Warning">The color of a warning in the editor.</param>
/// <param name="Info">The color of information in the editor.</param>
/// <param name="Predicate">The color of a predicate in the rule text and in the predicate lists.</param>
/// <param name="Operator">The color of an operator in the rule text and in the lists.</param>
/// <param name="Variable">The color of a variable (a <c>from(...)</c> read, a definition parameter or a quantifier variable).</param>
/// <param name="Constant">The color of a constant (<c>True</c>, <c>False</c>, <c>Unknown</c>).</param>
/// <param name="Description">The color of description text: a comment in the rule text and the description of a list entry.</param>
/// <param name="HasColor"><see langword="false"/> for the palette of <c>NO_COLOR</c>.</param>
internal sealed record ThemePalette(
    Color Background,
    Color Surface,
    Color Selection,
    Color Text,
    Color True,
    Color False,
    Color Unknown,
    Color Skipped,
    Color Error,
    Color Warning,
    Color Info,
    Color Predicate,
    Color Operator,
    Color Variable,
    Color Constant,
    Color Description,
    bool HasColor
)
{
    /// <summary>
    /// The smallest contrast ratio (WCAG) that a text color keeps against <see cref="Background"/>, <see cref="Surface"/> and
    /// <see cref="Selection"/>.
    /// </summary>
    public const double MinimumContrast = 4.5;

    /// <summary>Gets the light palette: dark text on white.</summary>
    public static ThemePalette Light { get; } =
        new(
            Background: new Color(255, 255, 255),
            Surface: new Color(240, 240, 240),
            Selection: new Color(214, 232, 255),
            Text: new Color(24, 24, 24),
            True: new Color(0, 110, 0),
            False: new Color(180, 0, 0),
            Unknown: new Color(140, 90, 0),
            Skipped: new Color(96, 96, 96),
            Error: new Color(180, 0, 0),
            Warning: new Color(140, 90, 0),
            Info: new Color(0, 95, 135),
            Predicate: new Color(0, 64, 170),
            Operator: new Color(112, 40, 170),
            Variable: new Color(0, 105, 95),
            Constant: new Color(165, 0, 95),
            Description: new Color(80, 90, 100),
            HasColor: true
        );

    /// <summary>Gets the dark palette: light text on black.</summary>
    public static ThemePalette Dark { get; } =
        new(
            Background: new Color(0, 0, 0),
            Surface: new Color(32, 32, 32),
            Selection: new Color(16, 40, 72),
            Text: new Color(230, 230, 230),
            True: new Color(80, 255, 80),
            False: new Color(255, 95, 95),
            Unknown: new Color(255, 215, 0),
            Skipped: new Color(150, 150, 150),
            Error: new Color(255, 95, 95),
            Warning: new Color(255, 215, 0),
            Info: new Color(95, 215, 255),
            Predicate: new Color(120, 170, 255),
            Operator: new Color(205, 150, 255),
            Variable: new Color(80, 220, 200),
            Constant: new Color(255, 140, 200),
            Description: new Color(150, 165, 180),
            HasColor: true
        );

    /// <summary>Gets the palette with no color, for <c>NO_COLOR</c>: every color is the terminal default.</summary>
    public static ThemePalette Plain { get; } =
        new(
            Background: Color.None,
            Surface: Color.None,
            Selection: Color.None,
            Text: Color.None,
            True: Color.None,
            False: Color.None,
            Unknown: Color.None,
            Skipped: Color.None,
            Error: Color.None,
            Warning: Color.None,
            Info: Color.None,
            Predicate: Color.None,
            Operator: Color.None,
            Variable: Color.None,
            Constant: Color.None,
            Description: Color.None,
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

    /// <summary>Gets the color of a syntax kind in the editor and in the lists.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The color.</returns>
    public Color Of(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.Predicate => this.Predicate,
            SyntaxKind.Operator => this.Operator,
            SyntaxKind.Variable => this.Variable,
            SyntaxKind.Constant => this.Constant,
            _ => this.Description,
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

    /// <summary>
    /// Gets the text style of a syntax kind. With color it is none, because the color tells the kinds apart. Without color the
    /// weight does: an operator is bold, a predicate is italic, a variable is bold and italic, a constant is underlined and a
    /// description is faint. A diagnostic mark wins over these styles where the two overlap.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The style.</returns>
    public TextStyle StyleOf(SyntaxKind kind)
    {
        if (this.HasColor)
        {
            return TextStyle.None;
        }

        return kind switch
        {
            SyntaxKind.Predicate => TextStyle.Italic,
            SyntaxKind.Operator => TextStyle.Bold,
            SyntaxKind.Variable => TextStyle.Bold | TextStyle.Italic,
            SyntaxKind.Constant => TextStyle.Underline,
            _ => TextStyle.Faint,
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
