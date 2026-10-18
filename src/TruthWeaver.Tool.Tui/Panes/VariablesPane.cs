namespace TruthWeaver.Tool.Tui.Panes;

using Terminal.Gui.ViewBase;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The Variables pane: an editor for the data of the <c>from("source", "query")</c> references. The text is one JSON or
/// YAML object that maps each source name to its data. Every edit applies at once and the decision follows; a text with an
/// error keeps the last good data and shows the problem under the editor. Ctrl+F4 loads a file into it.
/// </summary>
internal sealed class VariablesPane : IWorkbenchPane
{
    /// <inheritdoc />
    public string Title => "Variables";

    /// <inheritdoc />
    public PaneSlot Slot => PaneSlot.Bottom;

    /// <inheritdoc />
    public View CreateView(Workbench workbench)
    {
        return new VariablesView(workbench);
    }
}
