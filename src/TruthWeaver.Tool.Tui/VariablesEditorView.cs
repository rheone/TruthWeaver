namespace TruthWeaver.Tool.Tui;

using Terminal.Gui.Editor;
using Terminal.Gui.ViewBase;

/// <summary>
/// The editor of the variables document (JSON or YAML) in the Variables pane. Unlike the rule editor, it keeps the editor's
/// own undo and redo, because the variables have no history in the document.
/// </summary>
internal sealed class VariablesEditorView : Editor
{
    /// <summary>Initializes a new instance of the <see cref="VariablesEditorView"/> class.</summary>
    public VariablesEditorView()
    {
        this.Width = Dim.Fill();
        this.Height = Dim.Fill(1);
        this.GutterOptions = GutterOptions.None;
    }
}
