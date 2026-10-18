namespace TruthWeaver.Tool.Tui;

using System.Collections.ObjectModel;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

/// <summary>
/// The Rewrite pane: the result of a rewrite before it is accepted (the rule and its tree before and after, the size, the
/// steps of <c>Simplify</c> and the diagnostics), and the comparison with the last saved or accepted version. Keys in the
/// pane: <c>r</c> chooses a rewrite, <c>a</c> accepts it, <c>x</c> discards it and <c>c</c> compares.
/// </summary>
/// <param name="services">The dialogs, or <see langword="null"/> for <see cref="WorkbenchServices.Current"/>.</param>
internal sealed class RewritePane(WorkbenchServices? services) : IWorkbenchPane
{
    /// <summary>Initializes a new instance of the <see cref="RewritePane"/> class with the program's services.</summary>
    public RewritePane()
        : this(null) { }

    /// <inheritdoc />
    public string Title => "Rewrite";

    /// <inheritdoc />
    public PaneSlot Slot => PaneSlot.Side;

    /// <summary>Runs the action of a key in the pane.</summary>
    /// <param name="controller">The controller.</param>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> when the key is a key of the pane.</returns>
    public static bool Handle(RewriteController controller, Key key)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(key);

        if (key.IsCtrl || key.IsAlt)
        {
            return false;
        }

        Action? action = key.AsRune.ToString() switch
        {
            "r" => () => controller.Choose(),
            "a" => () => controller.Accept(),
            "x" => controller.Discard,
            "c" => () => controller.Compare(),
            _ => null,
        };
        action?.Invoke();
        return action is not null;
    }

    /// <inheritdoc />
    public View CreateView(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        RewriteController controller = RewriteController.For(workbench, services);
        ObservableCollection<string> lines = [];
        ListView list = new()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            KeystrokeNavigator = null,
        };
        list.SetSource(lines);

        void Show()
        {
            lines.Clear();
            foreach (string line in controller.Lines)
            {
                lines.Add(line);
            }
        }

        // A key that is not a key of the pane goes on to the window and its commands.
        list.KeyDown += (_, key) =>
        {
            if (Handle(controller, key))
            {
                key.Handled = true;
            }
        };
        controller.Changed += (_, _) => Show();
        Show();
        return list;
    }
}
