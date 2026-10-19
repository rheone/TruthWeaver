namespace TruthWeaver.Tool.Tui.Workbench;

using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

/// <summary>
/// Builds the menu bar of the workbench from the commands and choices: one menu for each <see cref="CommandGroup"/> that has
/// an entry, in the order of the enum. A menu entry runs the same action as the key, and shows the key on its right.
/// </summary>
/// <remarks>
/// The keys of the commands stay with the window (<see cref="WorkbenchWindow.RunCommand"/>): Terminal.Gui runs the key of a
/// menu entry only while its menu is open, so a key never runs twice.
/// </remarks>
internal static class WorkbenchMenus
{
    /// <summary>The key that opens the menu bar. F9, the Terminal.Gui default, imports a rule.</summary>
    public static readonly Key MenuBarKey = Key.F10;

    /// <summary>Gets the menus for a set of commands and choices: the groups that have an entry, in the order of the enum.</summary>
    /// <param name="commands">The commands.</param>
    /// <param name="choices">The choices.</param>
    /// <returns>The menus.</returns>
    public static IReadOnlyList<WorkbenchMenuGroup> GroupsOf(
        IEnumerable<WorkbenchCommand> commands,
        IEnumerable<WorkbenchChoice> choices
    )
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(choices);

        WorkbenchCommand[] commandList = [.. commands];
        WorkbenchChoice[] choiceList = [.. choices];
        return
        [
            .. Enum.GetValues<CommandGroup>()
                .Select(group => new WorkbenchMenuGroup(
                    group,
                    [.. commandList.Where(c => c.Group == group)],
                    [.. choiceList.Where(c => c.Group == group)]
                ))
                .Where(menu => menu.Commands.Count > 0 || menu.Choices.Count > 0),
        ];
    }

    /// <summary>Gets the title of the menu of a group, with its hot key marked by <c>_</c> (Alt and the letter opens it).</summary>
    /// <param name="group">The group.</param>
    /// <returns>The title, such as <c>_File</c>.</returns>
    public static string TitleOf(CommandGroup group)
    {
        // Edit takes E, so Export takes x.
        return group switch
        {
            CommandGroup.File => "_File",
            CommandGroup.Edit => "_Edit",
            CommandGroup.Insert => "_Insert",
            CommandGroup.Rule => "_Rule",
            CommandGroup.Analyze => "_Analyze",
            CommandGroup.Export => "E_xport",
            CommandGroup.View => "_View",
            _ => "_Help",
        };
    }

    /// <summary>Gets the text of one value in the submenu of a choice: <c>(*)</c> marks the current value.</summary>
    /// <param name="option">The value.</param>
    /// <param name="isCurrent"><see langword="true"/> for the current value.</param>
    /// <returns>The text.</returns>
    public static string OptionTitle(string option, bool isCurrent)
    {
        return (isCurrent ? "(*) " : "( ) ") + option;
    }

    /// <summary>Creates the menu bar of a window.</summary>
    /// <param name="window">The window that the entries act on.</param>
    /// <param name="commands">The commands.</param>
    /// <param name="choices">The choices.</param>
    /// <returns>The menu bar, on the first row of the window.</returns>
    public static MenuBar Create(
        WorkbenchWindow window,
        IEnumerable<WorkbenchCommand> commands,
        IEnumerable<WorkbenchChoice> choices
    )
    {
        ArgumentNullException.ThrowIfNull(window);

        MenuBar bar = new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Key = MenuBarKey,
        };
        foreach (WorkbenchMenuGroup menu in GroupsOf(commands, choices))
        {
            bar.Add(CreateMenu(window, menu));
        }

        return bar;
    }

    // One menu of the bar. The menu bar owns it, and the menu owns its entries and submenus.
    private static MenuBarItem CreateMenu(WorkbenchWindow window, WorkbenchMenuGroup menu)
    {
        List<View> entries =
        [
            .. menu.Commands.Select(command => new MenuItem(
                command.Title,
                string.Empty,
                () => command.Execute(window),
                command.Key
            )),
        ];
        List<(WorkbenchChoice Choice, MenuItem[] Options)> choiceItems = [];
        foreach (WorkbenchChoice choice in menu.Choices)
        {
            MenuItem[] options =
            [
                .. choice.Options.Select(
                    (option, index) =>
                        new MenuItem(OptionTitle(option, false), string.Empty, () => choice.Select(window, index), Key.Empty)
                ),
            ];
            choiceItems.Add((choice, options));
            entries.Add(CreateSubmenu(choice.Title, options));
        }

        MenuBarItem item = new(TitleOf(menu.Group), entries);

        // The marks of the choices follow the state, so they are set each time the menu opens.
        item.PopoverMenuOpenChanged += (_, args) =>
        {
            if (args.NewValue)
            {
                MarkCurrent(window.Workbench, choiceItems);
            }
        };
        item.Initialized += (_, _) => KeepOnlyAltHotKeys(item);
        return item;
    }

    // An entry that opens a submenu; the entry owns the submenu.
    private static MenuItem CreateSubmenu(string title, MenuItem[] options)
    {
        return new MenuItem { Title = title, SubMenu = new Menu(options) };
    }

    private static void MarkCurrent(Workbench workbench, List<(WorkbenchChoice Choice, MenuItem[] Options)> choiceItems)
    {
        foreach ((WorkbenchChoice choice, MenuItem[] options) in choiceItems)
        {
            int current = choice.Current(workbench);
            for (int i = 0; i < options.Length; i++)
            {
                options[i].Title = OptionTitle(choice.Options[i], i == current);
            }
        }
    }

    // Terminal.Gui binds the hot key of a menu title with and without Alt. Without Alt, a letter that a pane does not take,
    // such as 'f' in the Tree pane, would open a menu. Only Alt and the letter opens a menu here.
    private static void KeepOnlyAltHotKeys(View item)
    {
        foreach (Key key in item.HotKeyBindings.GetBindings().Select(b => b.Key).ToList())
        {
            if (!key.IsAlt)
            {
                item.HotKeyBindings.Remove(key);
            }
        }
    }
}
