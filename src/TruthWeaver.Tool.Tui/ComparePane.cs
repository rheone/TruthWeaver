namespace TruthWeaver.Tool.Tui;

using System.Collections.ObjectModel;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

/// <summary>
/// The Compare pane: a second rule, read-only, beside the open rule, with how it relates to the open rule and which pairs of
/// values the two rules can give (from <see cref="CompareController"/>). Keys in the pane: <c>o</c> loads a second rule from
/// a file or the clipboard, Enter on a line with an example loads that assignment into the Assign pane and <c>x</c> closes the
/// second rule.
/// </summary>
/// <param name="services">The dialogs and the clipboard, or <see langword="null"/> for <see cref="WorkbenchServices.Current"/>.</param>
internal sealed class ComparePane(WorkbenchServices? services) : IWorkbenchPane
{
    /// <summary>Initializes a new instance of the <see cref="ComparePane"/> class with the program's services.</summary>
    public ComparePane()
        : this(null) { }

    /// <inheritdoc />
    public string Title => "Compare";

    /// <inheritdoc />
    public PaneSlot Slot => PaneSlot.Side;

    /// <inheritdoc />
    public View CreateView(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        CompareController controller = CompareController.For(workbench, services);
        ObservableCollection<string> lines = [];
        ListView list = new()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            KeystrokeNavigator = null,
        };
        list.SetSource(lines);
        List<CompareLine> shown = [];

        void Show()
        {
            shown.Clear();
            shown.AddRange(controller.Lines);
            lines.Clear();
            foreach (CompareLine line in shown)
            {
                lines.Add(line.Text);
            }
        }

        // A key that is not a key of the pane goes on to the window and its commands.
        list.KeyDown += (_, key) =>
        {
            if (key == Key.Enter && list.Value is int index && index < shown.Count)
            {
                controller.LoadExample(shown[index]);
                key.Handled = true;
            }
            else if (!key.IsCtrl && !key.IsAlt && key.AsRune.ToString() == "o")
            {
                controller.Open();
                key.Handled = true;
            }
            else if (!key.IsCtrl && !key.IsAlt && key.AsRune.ToString() == "x")
            {
                controller.Close();
                key.Handled = true;
            }
        };
        controller.Changed += (_, _) => Show();
        Show();
        return list;
    }
}
