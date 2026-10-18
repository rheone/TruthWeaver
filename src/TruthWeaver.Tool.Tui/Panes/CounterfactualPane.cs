namespace TruthWeaver.Tool.Tui.Panes;

using System.Collections.ObjectModel;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The What would make this True panel: the smallest changes to the assigned term values that give the rule the target
/// value, from <see cref="CounterfactualController"/>. Enter on a change applies it to the Assign pane, <c>t</c> moves the
/// target (True, False, Unknown) and <c>r</c> searches again. It searches while it shows, after each change of the rule or
/// the values, on a background task.
/// </summary>
internal sealed class CounterfactualPane : IWorkbenchPane
{
    /// <inheritdoc />
    public string Title => "Make True";

    /// <inheritdoc />
    public PaneSlot Slot => PaneSlot.Bottom;

    /// <inheritdoc />
    public View CreateView(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        // Read the assignment before subscribing to the document, so the outcome is never stale in the handler.
        _ = workbench.Assignment;
        CounterfactualController controller = CounterfactualController.For(workbench);
        ObservableCollection<string> lines = [];
        ListView list = new() { Width = Dim.Fill(), Height = Dim.Fill() };
        list.SetSource(lines);
        List<CounterfactualLine> shown = [];

        void Show()
        {
            shown.Clear();
            shown.AddRange(controller.Lines);
            lines.Clear();
            foreach (CounterfactualLine line in shown)
            {
                lines.Add(line.Text);
            }
        }

        // The panel works only while it shows, so a hidden panel costs nothing on each change.
        void Refresh()
        {
            if (list.Visible)
            {
                controller.Refresh();
            }
        }

        list.KeyDown += (_, key) =>
        {
            if (key == Key.Enter && list.Value is int index && index < shown.Count && shown[index].Set is { } set)
            {
                controller.Apply(set);
                key.Handled = true;
            }
            else if (!key.IsCtrl && !key.IsAlt && key.AsRune.ToString() == "t")
            {
                controller.CycleTarget();
                key.Handled = true;
            }
            else if (!key.IsCtrl && !key.IsAlt && key.AsRune.ToString() == "r")
            {
                controller.Refresh();
                key.Handled = true;
            }
        };
        controller.Changed += (_, _) => Show();
        workbench.DocumentChanged += (_, _) => Refresh();
        workbench.AssignmentChanged += (_, _) => Refresh();
        list.VisibleChanged += (_, _) => Refresh();
        Show();
        return list;
    }
}
