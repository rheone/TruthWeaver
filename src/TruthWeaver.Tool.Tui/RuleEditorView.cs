namespace TruthWeaver.Tool.Tui;

using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using TruthWeaver.Diagnostics;
using Attribute = Terminal.Gui.Drawing.Attribute;
using Point = System.Drawing.Point;

// CS0618: Terminal.Gui 2.5 marks TextView obsolete in favour of EditorView from the separate tui-cs/Editor package. That
// package is a new dependency the owner has not approved; TextView ships in the approved Terminal.Gui 2.5.0 and works.
// The suppression covers only the base-type reference. Each other file that uses TextView (VariablesEditorView,
// ViewPaneView) has its own narrow suppression. .scratch/tui-workbench/issues/09-move-to-terminal-gui-editor-package.md records the follow-up.
#pragma warning disable CS0618
/// <summary>
/// The rule text editor: a Terminal.Gui text view that colors the characters a diagnostic marks (errors red, warnings
/// yellow, information cyan, all underlined). Undo and redo are not the text view's own: the workbench binds those keys to
/// the document history, so one history covers typing, formatting and conversion.
/// </summary>
internal sealed class RuleEditorView : TextView
#pragma warning restore CS0618
{
    private EditorMarks marks = EditorMarks.None;

    /// <summary>Initializes a new instance of the <see cref="RuleEditorView"/> class.</summary>
    public RuleEditorView()
    {
        // The document keeps the history. The text view's own Ctrl+Z and Ctrl+Y would undo behind its back, so its undo
        // and redo commands report "not handled" and the keys go on to the workbench window.
        this.AddCommand(Command.Undo, () => false);
        this.AddCommand(Command.Redo, () => false);
    }

    /// <summary>Gets or sets the marks to color, computed from the current text.</summary>
    public EditorMarks Marks
    {
        get => this.marks;
        set
        {
            this.marks = value;
            this.SetNeedsDraw();
        }
    }

    /// <summary>Moves the cursor to an offset of the text and scrolls it into view.</summary>
    /// <param name="offset">The offset.</param>
    public void MoveTo(int offset)
    {
        (int row, int column) = this.marks.PositionOf(offset);
        this.InsertionPoint = new Point(column, row);
    }

    /// <inheritdoc />
    protected override void OnDrawNormalColor(List<Cell> line, int idxCol, int idxRow)
    {
        if (this.marks.At(idxRow, idxCol) is { } severity)
        {
            Attribute normal = this.GetAttributeForRole(VisualRole.Normal);
            this.SetAttribute(new Attribute(ColorOf(severity), normal.Background, TextStyle.Underline));
            return;
        }

        base.OnDrawNormalColor(line, idxCol, idxRow);
    }

    private static Color ColorOf(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => new Color(ColorName16.BrightRed),
            DiagnosticSeverity.Warning => new Color(ColorName16.Yellow),
            _ => new Color(ColorName16.Cyan),
        };
    }
}
