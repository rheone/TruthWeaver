namespace TruthWeaver.Tool.Tui;

using Terminal.Gui.ViewBase;

/// <summary>
/// The assignment pane: a list of the terms of the rule with a value each, the decision (result, <c>IsSatisfied</c>,
/// faults and the <c>Collapse</c> and <c>Project</c> answers) and the trace tree with each node's value in color and a
/// plain reason line. Space on a term moves it through True, False and Unknown; T, F and U set it. Every change
/// evaluates the rule again.
/// </summary>
internal sealed class AssignmentPane : IWorkbenchPane
{
    /// <inheritdoc />
    public string Title => "Assign";

    /// <inheritdoc />
    public PaneSlot Slot => PaneSlot.Bottom;

    /// <inheritdoc />
    public View CreateView(Workbench workbench)
    {
        return new AssignmentView(workbench);
    }
}
