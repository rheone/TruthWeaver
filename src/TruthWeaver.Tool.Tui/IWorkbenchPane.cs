namespace TruthWeaver.Tool.Tui;

using Terminal.Gui.ViewBase;

/// <summary>
/// One pane of the workbench: a view of the shared <see cref="Workbench"/> state in one region of the window. To add a
/// pane, implement this interface and add an instance to <see cref="WorkbenchPanes.Default"/>. A region with several
/// panes shows one at a time; Ctrl+PageDown and Ctrl+PageUp switch between them.
/// </summary>
internal interface IWorkbenchPane
{
    /// <summary>Gets the title of the pane, shown on the border of its region.</summary>
    public string Title { get; }

    /// <summary>Gets the region the pane shows in.</summary>
    public PaneSlot Slot { get; }

    /// <summary>
    /// Creates the view of the pane. The window calls this once and fills the region with the view. The view follows the
    /// workbench through <see cref="Workbench.DocumentChanged"/> and the other workbench events, and changes the document
    /// only through the workbench and <see cref="Workbench.Document"/>.
    /// </summary>
    /// <param name="workbench">The shared state.</param>
    /// <returns>The view, sized to fill its region.</returns>
    public View CreateView(Workbench workbench);
}
