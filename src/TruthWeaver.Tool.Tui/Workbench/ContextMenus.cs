namespace TruthWeaver.Tool.Tui.Workbench;

using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Views;

/// <summary>
/// The context menus: the menu that a right click opens in the editor, the tree, the term list and the diagnostics pane.
/// Each menu lists the common actions of its pane with their shortcuts. A shortcut is either the key of a command of the
/// window or a key of the pane itself; <see cref="Run"/> sends the key to the place that binds it, so no entry has its own
/// action that could drift from the key.
/// </summary>
internal static class ContextMenus
{
    /// <summary>Gets the entries of the editor menu.</summary>
    public static IReadOnlyList<ContextMenuEntry> EditorEntries { get; } =
    [
        Command("Undo"),
        Command("Redo"),
        Local("Cut", Key.X.WithCtrl),
        Local("Copy", Key.C.WithCtrl),
        Local("Paste", Key.V.WithCtrl),
        Local("Select all", Key.A.WithCtrl),
        Command("Format: canonical"),
        Command("Format: pretty"),
        Command("Save"),
    ];

    /// <summary>Gets the entries of the tree menu.</summary>
    public static IReadOnlyList<ContextMenuEntry> TreeEntries { get; } =
    [
        Local("Replace operator", new Key('o')),
        Local("Wrap in NOT", new Key('w')),
        Local("Negate", new Key('n')),
        Local("Insert operand", new Key('i')),
        Local("Delete node", Key.Delete),
        Local("Copy node", new Key('y')),
        Local("Copy rule", new Key('Y')),
        Local("Paste", new Key('v')),
    ];

    /// <summary>Gets the entries of the term list menu.</summary>
    public static IReadOnlyList<ContextMenuEntry> TermEntries { get; } =
    [
        Local("Set True", new Key('T')),
        Local("Set False", new Key('F')),
        Local("Set Unknown", new Key('U')),
        Local("Next value", Key.Space),
        Command("All terms Unknown"),
        Command("Load variables..."),
    ];

    /// <summary>Gets the entries of the diagnostics menu.</summary>
    public static IReadOnlyList<ContextMenuEntry> DiagnosticsEntries { get; } =
    [Local("Go to location", Key.Enter), Command("Validate"), Command("Format: canonical")];

    /// <summary>Gets the kind of context menu for the view under the pointer.</summary>
    /// <param name="view">The deepest view under the pointer.</param>
    /// <returns>The view that owns the menu and the entries, or <see langword="null"/> when the view has no context menu.</returns>
    public static (View Owner, IReadOnlyList<ContextMenuEntry> Entries)? For(View? view)
    {
        for (View? current = view; current is not null; current = current.SuperView)
        {
            switch (current)
            {
                case RuleEditorView:
                    return (current, EditorEntries);
                case TreePaneView tree when IsInside(view, tree.List):
                    return (tree.List, TreeEntries);
                case AssignmentView assignment when IsInside(view, assignment.Terms):
                    return (assignment.Terms, TermEntries);
                case ListView { Id: DiagnosticsPane.ViewId }:
                    return (current, DiagnosticsEntries);
            }
        }

        return null;
    }

    /// <summary>Builds the menu items for a set of entries. Each item shows the shortcut of its entry and runs it.</summary>
    /// <param name="window">The workbench window.</param>
    /// <param name="owner">The view that binds the pane keys.</param>
    /// <param name="entries">The entries.</param>
    /// <returns>The items.</returns>
    public static MenuItem[] ItemsOf(WorkbenchWindow window, View owner, IReadOnlyList<ContextMenuEntry> entries)
    {
        return
        [
            .. entries.Select(entry => new MenuItem(entry.Title, string.Empty, () => Run(window, owner, entry), entry.Key)),
        ];
    }

    /// <summary>Runs an entry: sends its key to the command of the window or to the pane that binds it.</summary>
    /// <param name="window">The workbench window.</param>
    /// <param name="owner">The view that binds the pane keys.</param>
    /// <param name="entry">The entry.</param>
    public static void Run(WorkbenchWindow window, View owner, ContextMenuEntry entry)
    {
        if (entry.IsWindowCommand)
        {
            _ = window.RunCommand(entry.Key);
        }
        else
        {
            _ = owner.NewKeyDownEvent(entry.Key);
        }
    }

    // The window commands by title; WorkbenchCommands.Default is the source of the key, so a changed key changes the menu.
    private static ContextMenuEntry Command(string title)
    {
        WorkbenchCommand command = WorkbenchCommands.Default.Single(c => c.Title == title);
        return new ContextMenuEntry(title, command.Key, IsWindowCommand: true);
    }

    private static ContextMenuEntry Local(string title, Key key)
    {
        return new ContextMenuEntry(title, key, IsWindowCommand: false);
    }

    private static bool IsInside(View? view, View container)
    {
        for (View? current = view; current is not null; current = current.SuperView)
        {
            if (ReferenceEquals(current, container))
            {
                return true;
            }
        }

        return false;
    }
}
