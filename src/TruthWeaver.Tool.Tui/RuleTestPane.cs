namespace TruthWeaver.Tool.Tui;

using Terminal.Gui.ViewBase;

/// <summary>
/// The Tests pane: the rule test file and its cases. Ctrl+F5 saves the current assignment as a case, Ctrl+F7 runs the cases of the file
/// on the open rule and lists each as pass or fail with the expected and the actual values. Enter on a case loads its
/// assignment into the Assign pane, which is how a failing case is debugged; Delete removes a case from the file.
/// </summary>
internal sealed class RuleTestPane : IWorkbenchPane
{
    /// <inheritdoc />
    public string Title => "Tests";

    /// <inheritdoc />
    public PaneSlot Slot => PaneSlot.Bottom;

    /// <inheritdoc />
    public View CreateView(Workbench workbench)
    {
        return new RuleTestView(workbench);
    }
}
