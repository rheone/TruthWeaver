namespace TruthWeaver.Tool.Tui.Panes;

using Terminal.Gui.ViewBase;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Tui.Theme;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The rule text editor pane. Every edit goes to <see cref="Workbench.Document"/>, which compiles it; every change of the
/// document (undo, redo, format, open) comes back into the editor. The editor colors the spans of the diagnostics.
/// </summary>
internal sealed class RuleTextPane : IWorkbenchPane
{
    /// <inheritdoc />
    public string Title => "Rule";

    /// <inheritdoc />
    public PaneSlot Slot => PaneSlot.Editor;

    /// <inheritdoc />
    public View CreateView(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        RuleEditorView editor = new()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Palette = workbench.Palette,
        };
        workbench.ThemeChanged += (_, _) => editor.Palette = workbench.Palette;
        bool syncing = false;

        // The document to the editor: replace the text only when it differs, so typing keeps the cursor where it is.
        void ShowDocument()
        {
            string text = workbench.Document.Text;
            if (Normalize(editor.Text) != text)
            {
                syncing = true;
                try
                {
                    editor.Text = text;
                }
                finally
                {
                    syncing = false;
                }
            }

            editor.Marks = EditorMarks.Compute(text, workbench.Document.Diagnostics);
        }

        // The editor to the document. The text view may report its lines with the platform line ending; the document
        // keeps line feeds, so diagnostic spans and saved files are the same on every platform.
        editor.ContentChanged += (_, _) =>
        {
            if (!syncing)
            {
                workbench.EditText(Normalize(editor.Text));
            }
        };

        workbench.DocumentChanged += (_, _) => ShowDocument();
        workbench.SpanRequested += (_, span) =>
        {
            editor.MoveTo(span.Start);
            editor.SetFocus();
        };

        // The Insert menu: the editor puts the text at the cursor, in place of the selection.
        workbench.InsertRequested += (_, text) =>
        {
            editor.InsertAtCaret(text);
            editor.SetFocus();
        };

        ShowDocument();
        return editor;
    }

    private static string Normalize(string text)
    {
        return text.ReplaceLineEndings("\n");
    }
}
