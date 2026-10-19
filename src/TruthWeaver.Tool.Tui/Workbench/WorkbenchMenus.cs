namespace TruthWeaver.Tool.Tui.Workbench;

using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Tui.Commands;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Theme;
using TruthWeaver.Tool.Tui.Views;

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
    /// <summary>The longest path an Open recent entry shows in full; a longer path keeps its end.</summary>
    public const int MaxShownPath = 60;

    /// <summary>The title of the submenu that lists the recent files.</summary>
    public const string RecentTitle = "Open recent";

    /// <summary>The title of the submenu that lists the sample rules.</summary>
    public const string SamplesTitle = "Samples";

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

    /// <summary>
    /// Gets the entries of the Open recent submenu for the current recent files: the title of each and whether the file is
    /// missing. A missing file is marked, and choosing it takes it out of the list.
    /// </summary>
    /// <param name="workbench">The workbench.</param>
    /// <returns>The entries, newest first.</returns>
    public static IReadOnlyList<RecentEntry> RecentEntries(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        return
        [
            .. workbench.RecentFiles.Select(
                (path, index) =>
                {
                    bool missing = !File.Exists(path);
                    string shown = path.Length > MaxShownPath ? "..." + path[^(MaxShownPath - 3)..] : path;
                    return new RecentEntry(
                        path,
                        $"{index + 1}. {shown}{(missing ? " (missing, choose to remove)" : string.Empty)}",
                        missing
                    );
                }
            ),
        ];
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

        // The recent files follow the Open entry of the File menu.
        if (menu.Group == CommandGroup.File)
        {
            int open = entries.FindIndex(e => e is MenuItem { Title: "Open..." });
            entries.Insert(open + 1, CreateSubmenu(RecentTitle, RecentItems(window, [])));
            entries.Insert(open + 2, CreateSubmenu(SamplesTitle, SampleItems(window)));
        }

        // The operators and the constants are submenus of fixed entries; they follow the Predicate entry. Each entry of the
        // Insert menu has the color of its syntax kind; the title of the entry (or of its submenu) names the kind as well.
        List<(MenuItem Item, SyntaxKind Kind)> kinded = [];
        if (menu.Group == CommandGroup.Insert)
        {
            MenuItem operators = CreateSubmenu(InsertCommands.OperatorsTitle, OperatorItems(window, kinded));
            MenuItem constants = CreateSubmenu(
                InsertCommands.ConstantsTitle,
                [.. InsertItems.Constants.Select(i => InsertEntry(window, i, SyntaxKind.Constant, kinded))]
            );
            entries.Insert(1, operators);
            entries.Insert(2, constants);
            kinded.Add((operators, SyntaxKind.Operator));
            kinded.Add((constants, SyntaxKind.Constant));
            foreach (MenuItem entry in entries.OfType<MenuItem>())
            {
                SyntaxKind? kind = entry.Title switch
                {
                    "Predicate..." or "Use definition..." => SyntaxKind.Predicate,
                    "Variable..." => SyntaxKind.Variable,
                    _ => null,
                };
                if (kind is { } known)
                {
                    kinded.Add((entry, known));
                }
            }
        }

        MenuItem? recent = entries.OfType<MenuItem>().FirstOrDefault(e => e.Title == RecentTitle);

        MenuBarItem item = new(TitleOf(menu.Group), entries);
        if (kinded.Count > 0)
        {
            ColorKinds(kinded, window.Workbench.Palette);
            window.Workbench.ThemeChanged += (_, _) => ColorKinds(kinded, window.Workbench.Palette);
        }

        // The marks of the choices follow the state, so they are set each time the menu opens.
        item.PopoverMenuOpenChanged += (_, args) =>
        {
            if (args.NewValue)
            {
                MarkCurrent(window.Workbench, choiceItems);
                if (recent is not null)
                {
                    // The list changes with every open, so the submenu is built again each time the menu opens.
                    Menu? old = recent.SubMenu;
                    recent.SubMenu = new Menu(RecentItems(window, RecentEntries(window.Workbench)));
                    old?.Dispose();
                }
            }
        };
        item.Initialized += (_, _) => KeepOnlyAltHotKeys(item);
        return item;
    }

    // The entries of the Open recent submenu. With no recent file the submenu shows one disabled line.
    private static MenuItem[] RecentItems(WorkbenchWindow window, IReadOnlyList<RecentEntry> entries)
    {
        if (entries.Count == 0)
        {
            return [new MenuItem("(no recent files)", string.Empty, () => { }, Key.Empty) { Enabled = false }];
        }

        return
        [
            .. entries.Select(entry => new MenuItem(
                entry.Title,
                string.Empty,
                () => window.Workbench.OpenRecent(entry.Path),
                Key.Empty
            )),
        ];
    }

    // The entries of the Samples submenu: one for each sample, with what it shows beside its name.
    private static MenuItem[] SampleItems(WorkbenchWindow window)
    {
        return
        [
            .. SampleRules.All.Select(sample => new MenuItem(
                sample.Name,
                sample.Description,
                () => window.Workbench.OpenSample(sample),
                Key.Empty
            )),
        ];
    }

    // The Operators submenu: one submenu for each group (connectives, cardinality, inspection, other).
    private static MenuItem[] OperatorItems(WorkbenchWindow window, List<(MenuItem Item, SyntaxKind Kind)> kinded)
    {
        return
        [
            .. InsertItems.Operators.Select(g =>
            {
                MenuItem group = CreateSubmenu(
                    g.Title,
                    [.. g.Items.Select(i => InsertEntry(window, i, SyntaxKind.Operator, kinded))]
                );
                kinded.Add((group, SyntaxKind.Operator));
                return group;
            }),
        ];
    }

    // An entry of the Insert menu: the name in the color of its kind, with its one-line description beside it.
    private static MenuItem InsertEntry(
        WorkbenchWindow window,
        InsertItem item,
        SyntaxKind kind,
        List<(MenuItem Item, SyntaxKind Kind)> kinded
    )
    {
        MenuItem entry = new(item.Name, item.Description, () => window.Workbench.InsertText(item.Text, item.Name), Key.Empty);
        kinded.Add((entry, kind));
        return entry;
    }

    // Colors the name of each entry in the color of its kind, and its description in the color of descriptions.
    private static void ColorKinds(List<(MenuItem Item, SyntaxKind Kind)> kinded, ThemePalette palette)
    {
        foreach ((MenuItem item, SyntaxKind kind) in kinded)
        {
            item.CommandView?.SetScheme(SyntaxLegend.SchemeOf(kind, palette));
            if (!string.IsNullOrEmpty(item.HelpText))
            {
                item.HelpView?.SetScheme(SyntaxLegend.SchemeOf(SyntaxKind.Description, palette));
            }
        }
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
