namespace TruthWeaver.Tool.Tui;

/// <summary>The panes of the workbench window: the place to register a new pane.</summary>
internal static class WorkbenchPanes
{
    /// <summary>
    /// Gets the panes, in order. The order sets the order of the panes inside a region; the first pane of a region shows
    /// when the window opens. Add a new pane here.
    /// </summary>
    public static IReadOnlyList<IWorkbenchPane> Default { get; } =
    [
        new RuleTextPane(),
        new CanonicalTextPane(),
        new DiagnosticsPane(),
        new ViewPane(),
        new ValidationPane(),
        new AssignmentPane(),
        new VariablesPane(),
        new RuleTestPane(),
        new TreePane(),
        new RewritePane(),
    ];
}
