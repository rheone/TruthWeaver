namespace TruthWeaver.Tool.Tui;

using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// CS0618: Terminal.Gui 2.5 marks TextView obsolete in favour of EditorView from the separate tui-cs/Editor package, a
// dependency the owner has not approved. TextView ships in the approved Terminal.Gui 2.5.0 and works. As in
// RuleEditorView, the suppression covers only the base-type reference;
// .scratch/tui-workbench/issues/09-move-to-terminal-gui-editor-package.md records the follow-up.
#pragma warning disable CS0618
/// <summary>
/// The editor of the variables document (JSON or YAML) in the Variables pane. Unlike the rule editor, it keeps the text
/// view's own undo and redo, because the variables have no history in the document.
/// </summary>
internal sealed class VariablesEditorView : TextView
#pragma warning restore CS0618
{
    /// <summary>Initializes a new instance of the <see cref="VariablesEditorView"/> class.</summary>
    public VariablesEditorView()
    {
        this.Width = Dim.Fill();
        this.Height = Dim.Fill(1);
    }
}
