namespace TruthWeaver.Tool.Tui.Views;

using Terminal.Gui.Drawing;
using Terminal.Gui.Editor;
using Terminal.Gui.Editor.Document;
using Terminal.Gui.Editor.Rendering;
using Terminal.Gui.Input;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Tui.Theme;
using Attribute = Terminal.Gui.Drawing.Attribute;
using Point = System.Drawing.Point;

/// <summary>
/// The rule text editor: a Terminal.Gui editor view that colors the characters a diagnostic marks (errors red, warnings
/// yellow, information cyan, all underlined; the colors follow the theme, and with no color the severities differ by
/// style). It also colors each syntax kind (predicate, operator, variable, constant and comment) in its own color; where a
/// diagnostic mark and a syntax kind overlap, the diagnostic mark wins. Undo and redo are not the editor's own: the workbench binds those keys to
/// the document history, so one history covers typing, formatting and conversion.
/// </summary>
internal sealed class RuleEditorView : Editor
{
    private EditorMarks marks = EditorMarks.None;
    private SyntaxMarks syntax = SyntaxMarks.None;
    private ThemePalette palette = ThemePalette.Dark;

    /// <summary>Initializes a new instance of the <see cref="RuleEditorView"/> class.</summary>
    public RuleEditorView()
    {
        // The document keeps the history. The editor's own Ctrl+Z and Ctrl+Y would undo behind its back, so its undo
        // and redo commands report "not handled" and the keys go on to the workbench window.
        this.AddCommand(Command.Undo, () => false);
        this.AddCommand(Command.Redo, () => false);
        this.GutterOptions = GutterOptions.None;
        this.LineTransformers.Add(new MarkTransformer(this));
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

    /// <summary>Gets or sets the syntax kinds to color, computed from the current text after the compile.</summary>
    public SyntaxMarks Syntax
    {
        get => this.syntax;
        set
        {
            this.syntax = value;
            this.SetNeedsDraw();
        }
    }

    /// <summary>Gets or sets the colors of the marks. The workbench sets it from the theme.</summary>
    public ThemePalette Palette
    {
        get => this.palette;
        set
        {
            this.palette = value;
            this.SetNeedsDraw();
        }
    }

    /// <summary>Gets the cursor as a 0-based column (X) and line (Y).</summary>
    public Point InsertionPoint
    {
        get
        {
            TextLocation location = this.Document!.GetLocation(this.CaretOffset);
            return new Point(location.Column - 1, location.Line - 1);
        }
    }

    /// <summary>Moves the cursor to an offset of the text and scrolls it into view.</summary>
    /// <param name="offset">The offset.</param>
    public void MoveTo(int offset)
    {
        this.CaretOffset = Math.Clamp(offset, 0, this.Document!.TextLength);
    }

    /// <summary>
    /// Puts text at the cursor in place of the selection, and leaves the cursor after the text. The text is one change of the
    /// document, so the editor reports one content change.
    /// </summary>
    /// <param name="text">The text.</param>
    public void InsertAtCaret(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        int start = this.CaretOffset;
        int length = 0;
        if (this.HasSelection)
        {
            start = Math.Min(this.SelectionStart, this.SelectionEnd);
            length = Math.Abs(this.SelectionEnd - this.SelectionStart);
            this.ClearSelection();
        }

        this.Document!.Replace(start, length, text);
        this.MoveTo(start + text.Length);
    }

    /// <summary>Colors the marked characters of a visual line: the editor calls it for each line before it draws.</summary>
    private sealed class MarkTransformer(RuleEditorView owner) : IVisualLineTransformer
    {
        /// <inheritdoc />
        public void Transform(CellVisualLine line)
        {
            EditorMarks marks = owner.marks;
            SyntaxMarks syntax = owner.syntax;
            int row = line.DocumentLine.LineNumber - 1;
            int lineStart = line.DocumentLine.Offset;
            List<CellVisualLineElement> result = [];
            bool changed = false;
            foreach (CellVisualLineElement element in line.Elements)
            {
                // Only a run of plain text is split per character. A run with wide characters has more cells than
                // characters, so it takes the most severe mark of its characters as a whole.
                if (element is TextRunElement run)
                {
                    changed |= ColorRun(run, marks, syntax, owner.palette, row, lineStart, result);
                }
                else
                {
                    result.Add(element);
                }
            }

            if (changed)
            {
                line.ReplaceElements(result);
            }
        }

        private static bool ColorRun(
            TextRunElement run,
            EditorMarks marks,
            SyntaxMarks syntax,
            ThemePalette palette,
            int row,
            int lineStart,
            List<CellVisualLineElement> result
        )
        {
            // Each character has a diagnostic severity, a syntax kind, both or neither. A severity wins over a kind.
            DiagnosticSeverity?[] severities = new DiagnosticSeverity?[run.Text.Length];
            SyntaxKind?[] kinds = new SyntaxKind?[run.Text.Length];
            bool any = false;
            for (int i = 0; i < severities.Length; i++)
            {
                severities[i] = marks.At(row, run.DocumentOffset - lineStart + i);
                kinds[i] = syntax.At(run.DocumentOffset + i);
                any |= severities[i] is not null || kinds[i] is not null;
            }

            if (!any)
            {
                result.Add(run);
                return false;
            }

            bool simple = run.Text.Length == run.VisualLength && run.Text.Length == run.DocumentLength;
            if (!simple)
            {
                // A run with wide characters has more cells than characters, so it takes the most severe mark of its
                // characters as a whole, or else the first syntax kind.
                Attribute whole = severities.Any(s => s is not null)
                    ? Marked(run.Attribute, severities.Where(s => s is not null).Max()!.Value, palette)
                    : Kinded(run.Attribute, kinds.First(k => k is not null)!.Value, palette);
                result.Add(new TextRunElement(run.DocumentOffset, run.DocumentLength, run.VisualColumn, run.Text, whole));
                return true;
            }

            // Split the run where the mark changes, so each piece has one color.
            int start = 0;
            while (start < severities.Length)
            {
                int end = start + 1;
                while (end < severities.Length && severities[end] == severities[start] && kinds[end] == kinds[start])
                {
                    end++;
                }

                Attribute attribute = run.Attribute;
                if (severities[start] is { } severity)
                {
                    attribute = Marked(run.Attribute, severity, palette);
                }
                else if (kinds[start] is { } kind)
                {
                    attribute = Kinded(run.Attribute, kind, palette);
                }

                result.Add(
                    new TextRunElement(
                        run.DocumentOffset + start,
                        end - start,
                        run.VisualColumn + start,
                        run.Text[start..end],
                        attribute
                    )
                );
                start = end;
            }

            return true;
        }

        private static Attribute Kinded(Attribute normal, SyntaxKind kind, ThemePalette palette)
        {
            return new Attribute(palette.Of(kind), palette.HasColor ? normal.Background : Color.None, palette.StyleOf(kind));
        }

        private static Attribute Marked(Attribute normal, DiagnosticSeverity severity, ThemePalette palette)
        {
            return new Attribute(
                palette.Of(severity),
                palette.HasColor ? normal.Background : Color.None,
                palette.StyleOf(severity)
            );
        }
    }
}
