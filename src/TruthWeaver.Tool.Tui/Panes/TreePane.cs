namespace TruthWeaver.Tool.Tui.Panes;

using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The Tree pane, the structural editor: the nodes of the rule, one per line and indented by depth, with the selected node
/// and its literals above them. Keys inside the pane edit the selected node (see <see cref="Handle"/>); every edit is
/// checked by the compiler, changes the text of the editor, and is one undo step.
/// </summary>
/// <param name="services">The dialogs and the clipboard, or <see langword="null"/> for <see cref="WorkbenchServices.Current"/>.</param>
internal sealed class TreePane(WorkbenchServices? services) : IWorkbenchPane
{
    /// <summary>Initializes a new instance of the <see cref="TreePane"/> class with the program's services.</summary>
    public TreePane()
        : this(null) { }

    /// <inheritdoc />
    public string Title => "Tree";

    /// <inheritdoc />
    public PaneSlot Slot => PaneSlot.Side;

    /// <summary>
    /// Runs the edit of a key in the pane: <c>o</c> operator, <c>w</c> wrap in NOT, <c>n</c> negate, <c>i</c> insert,
    /// Delete, <c>[</c> and <c>]</c> swap, <c>p</c> palette, <c>l</c> literal, <c>y</c> and <c>Y</c> copy, <c>v</c> paste.
    /// </summary>
    /// <param name="controller">The controller.</param>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> when the key is a key of the pane.</returns>
    public static bool Handle(StructureController controller, Key key)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(key);

        if (key.IsCtrl || key.IsAlt)
        {
            return false;
        }

        if (key == Key.Delete)
        {
            _ = controller.Delete();
            return true;
        }

        Action? action = key.AsRune.ToString() switch
        {
            "o" => () => controller.ReplaceOperator(),
            "w" => () => controller.WrapInNot(),
            "n" => () => controller.Negate(),
            "i" => () => controller.InsertOperand(),
            "[" => () => controller.Swap(-1),
            "]" => () => controller.Swap(1),
            "p" => () => controller.ReplaceFromPalette(),
            "l" => () => controller.ChangeLiteral(),
            "y" => () => controller.CopyNode(),
            "Y" => () => controller.CopyRule(),
            "v" => () => controller.Paste(),
            _ => null,
        };
        action?.Invoke();
        return action is not null;
    }

    /// <inheritdoc />
    public View CreateView(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        // Reading Assignment before the subscriptions below keeps the outcome fresh in their handlers (see Workbench.Outcome).
        _ = workbench.Assignment;
        StructureController controller = StructureController.For(workbench, services);
        TreePaneView view = new(controller);

        // A key that is not a key of the pane goes on to the window and its commands.
        view.List.KeyDown += (_, key) =>
        {
            if (Handle(controller, key))
            {
                key.Handled = true;
            }
        };
        workbench.DocumentChanged += (_, _) => view.Show();
        workbench.SelectedNodeChanged += (_, _) => view.Show();

        // The badge of a term follows the assignment.
        workbench.AssignmentChanged += (_, _) => view.Show();

        // A case that is added, changed or removed changes the marks and the summary.
        workbench.RuleTestsChanged += (_, _) => view.Show();
        view.Show();
        return view;
    }
}
