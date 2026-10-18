namespace TruthWeaver.Tool.Tui;

using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

/// <summary>
/// The view of the Variables pane: the editor of the variables document and a note under it with the problems of the text,
/// or a hint. An edit goes to <see cref="Workbench.Assignment"/>; a loaded file or case comes back into the editor.
/// </summary>
internal sealed class VariablesView : View
{
    private readonly Workbench workbench;
    private readonly VariablesEditorView editor;
    private readonly Label note;
    private bool syncing;

    /// <summary>Initializes a new instance of the <see cref="VariablesView"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    public VariablesView(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        this.workbench = workbench;
        this.Width = Dim.Fill();
        this.Height = Dim.Fill();
        this.CanFocus = true;

        this.editor = new VariablesEditorView
        {
            X = 0,
            Y = 0,
            Text = workbench.Assignment.VariablesText,
        };
        this.note = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Text = AssignmentText.VariablesNote(workbench.Assignment),
        };
        this.Add(this.editor, this.note);

        // The editor to the assignment: every edit applies at once and the decision follows.
        this.editor.ContentsChanged += (_, _) =>
        {
            if (!this.syncing)
            {
                _ = workbench.Assignment.SetVariables(Normalize(this.editor.Text));
                workbench.NotifyAssignmentChanged();
            }
        };

        // The assignment to the editor: a loaded file or a loaded case replaces the text.
        workbench.AssignmentChanged += (_, _) => this.ShowAssignment();
    }

    /// <summary>Gets the editor, for tests that type into it.</summary>
    public View Editor => this.editor;

    /// <summary>Gets the text under the editor.</summary>
    public string NoteText => this.note.Text;

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        // The base class disposes the subviews; disposing a view twice does nothing, so the fields are disposed after it.
        base.Dispose(disposing);
        if (disposing)
        {
            this.editor.Dispose();
            this.note.Dispose();
        }
    }

    private static string Normalize(string text)
    {
        return text.ReplaceLineEndings("\n");
    }

    private void ShowAssignment()
    {
        this.note.Text = AssignmentText.VariablesNote(this.workbench.Assignment);
        if (Normalize(this.editor.Text) == this.workbench.Assignment.VariablesText)
        {
            return;
        }

        this.syncing = true;
        try
        {
            this.editor.Text = this.workbench.Assignment.VariablesText;
        }
        finally
        {
            this.syncing = false;
        }
    }
}
