namespace TruthWeaver.Tool.Tui;

using System.Collections.ObjectModel;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

/// <summary>
/// The validation panel: the result of every validation level, the diagnostics under their level, and the statistics of the
/// rule. It is the second pane of the bottom region (Ctrl+PageDown shows it). It validates when it is shown and after each
/// change of the rule while it is shown, and a key runs it again. Enter on a diagnostic moves the editor cursor to its span,
/// and <c>s</c> turns strict mode on or off.
/// </summary>
internal sealed class ValidationPane : IWorkbenchPane
{
    /// <inheritdoc />
    public string Title => "Validation";

    /// <inheritdoc />
    public PaneSlot Slot => PaneSlot.Bottom;

    /// <inheritdoc />
    public View CreateView(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        ValidationController controller = ValidationController.For(workbench);
        ObservableCollection<string> lines = [];
        ListView list = new() { Width = Dim.Fill(), Height = Dim.Fill() };
        list.SetSource(lines);
        List<ValidationLine> shown = [];

        void Show()
        {
            shown.Clear();
            shown.AddRange(controller.Lines);
            lines.Clear();
            foreach (ValidationLine line in shown)
            {
                lines.Add(line.Text);
            }
        }

        // The panel works only while it shows, so a hidden panel costs nothing on each keystroke.
        void Refresh()
        {
            if (list.Visible)
            {
                controller.Run(announce: false);
            }
        }

        list.KeyDown += (_, key) =>
        {
            if (key == Key.Enter && list.Value is int index && index < shown.Count)
            {
                key.Handled = controller.Select(shown[index]);
            }
            else if (!key.IsCtrl && !key.IsAlt && key.AsRune.ToString() == "r")
            {
                controller.Run();
                key.Handled = true;
            }
            else if (!key.IsCtrl && !key.IsAlt && key.AsRune.ToString() == "s")
            {
                controller.ToggleStrict();
                key.Handled = true;
            }
        };
        controller.Changed += (_, _) => Show();
        workbench.DocumentChanged += (_, _) => Refresh();
        list.VisibleChanged += (_, _) => Refresh();
        Show();
        return list;
    }
}
