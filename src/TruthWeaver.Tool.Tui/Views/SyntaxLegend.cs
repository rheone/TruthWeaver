namespace TruthWeaver.Tool.Tui.Views;

using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Tui.Theme;
using Attribute = Terminal.Gui.Drawing.Attribute;

/// <summary>
/// One line that names each syntax kind in its own color (predicate, operator, variable, constant, description). The names
/// are text, so the legend still tells the kinds apart when there is no color: the style of each name is the style that the
/// kind has under <c>NO_COLOR</c>.
/// </summary>
internal sealed class SyntaxLegend : View
{
    private readonly Label[] labels;

    /// <summary>Initializes a new instance of the <see cref="SyntaxLegend"/> class.</summary>
    /// <param name="palette">The colors of the theme.</param>
    public SyntaxLegend(ThemePalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);

        this.Width = Dim.Fill();
        this.Height = 1;
        this.CanFocus = false;

        List<Label> created = [];
        Label? previous = null;
        foreach (SyntaxKind kind in SyntaxKinds.All)
        {
            Label label = new()
            {
                Text = SyntaxKinds.NameOf(kind),
                X = previous is null ? 0 : Pos.Right(previous) + 2,
                Y = 0,
            };
            label.SetScheme(SchemeOf(kind, palette));
            this.Add(label);
            created.Add(label);
            previous = label;
        }

        this.labels = [.. created];
    }

    /// <summary>
    /// Gets the scheme that draws text of a kind: its color on the background of the dialogs and the menus, and on the
    /// background of the selection when the text has the focus. With no color it has the style of the kind only.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <param name="palette">The colors of the theme.</param>
    /// <returns>The scheme.</returns>
    public static Scheme SchemeOf(SyntaxKind kind, ThemePalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);

        Attribute normal = new(palette.Of(kind), palette.HasColor ? palette.Surface : Color.None, palette.StyleOf(kind));
        Attribute focus = new(
            palette.Of(kind),
            palette.HasColor ? palette.Selection : Color.None,
            palette.StyleOf(kind) | (palette.HasColor ? TextStyle.None : TextStyle.Reverse)
        );
        return new Scheme
        {
            Normal = normal,
            HotNormal = normal,
            Focus = focus,
            HotFocus = focus,
            Active = focus,
            HotActive = focus,
        };
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            foreach (Label label in this.labels)
            {
                label.Dispose();
            }
        }
    }
}
