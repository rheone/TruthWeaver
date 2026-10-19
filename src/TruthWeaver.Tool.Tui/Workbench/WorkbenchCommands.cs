namespace TruthWeaver.Tool.Tui.Workbench;

using Terminal.Gui.Input;
using TruthWeaver.Abstractions;
using TruthWeaver.Tool.Tui.Commands;
using TruthWeaver.Tool.Tui.Theme;

/// <summary>
/// The commands and choices of the workbench: the place to register a new one. One entry, with its group, puts it in the
/// menu of that group, in the key reference and, with a key, on that key. The keys avoid those the editor uses for itself
/// (Ctrl+A to Ctrl+F, Ctrl+N, Ctrl+P, Ctrl+V, Ctrl+W, Ctrl+X and the arrows without Alt) and F10, which opens the menu bar.
/// </summary>
internal static class WorkbenchCommands
{
    /// <summary>
    /// Gets the commands. Each menu lists the commands of its group in this order, and the hint line shows the hints in this
    /// order, so Quit comes last.
    /// </summary>
    public static IReadOnlyList<WorkbenchCommand> Default { get; } =
    [
        new(Key.F1, CommandGroup.Help, "Menus", "Open the menu bar (F10 also opens it)", w => w.OpenMenus()) { Hint = "Menus" },
        new(
            Key.F1.WithShift,
            CommandGroup.Help,
            "Key reference",
            "Show or hide the key reference: every key, by menu",
            w => w.ToggleHelp()
        ),
        new(Key.O.WithCtrl, CommandGroup.File, "Open...", "Open a rule file (rule text, JSON or YAML)", w => w.Workbench.Open())
        {
            Hint = "Open",
        },
        new(Key.S.WithCtrl, CommandGroup.File, "Save", "Save", w => w.Workbench.Save()) { Hint = "Save" },
        new(Key.F12, CommandGroup.File, "Save as...", "Save as (the extension selects the format)", w => w.Workbench.SaveAs()),
        new(Key.Z.WithCtrl, CommandGroup.Edit, "Undo", "Undo", w => w.Workbench.Undo()) { Hint = "Undo" },
        new(Key.Y.WithCtrl, CommandGroup.Edit, "Redo", "Redo", w => w.Workbench.Redo()),
        new(Key.F8, CommandGroup.Edit, "Format: canonical", "Format: canonical", w => w.Workbench.Format(pretty: false)),
        new(Key.F8.WithShift, CommandGroup.Edit, "Format: pretty", "Format: pretty", w => w.Workbench.Format(pretty: true)),
        new(Key.F6, CommandGroup.View, "Next region", "Next region", w => w.FocusNextRegion(1)) { Hint = "Region" },
        new(Key.F6.WithShift, CommandGroup.View, "Previous region", "Previous region", w => w.FocusNextRegion(-1)),
        new(Key.PageDown.WithCtrl, CommandGroup.View, "Next pane", "Next pane in this region", w => w.SwitchPane(1)),
        new(Key.PageUp.WithCtrl, CommandGroup.View, "Previous pane", "Previous pane in this region", w => w.SwitchPane(-1)),
        new(Key.F11, CommandGroup.View, "Maximise or restore", "Maximise or restore this region", w => w.ToggleMaximised()),
        new(
            Key.CursorLeft.WithAlt,
            CommandGroup.View,
            "Editor narrower",
            "Resize: editor narrower (vertical divider left)",
            w => w.MoveVerticalDivider(-1)
        ),
        new(
            Key.CursorRight.WithAlt,
            CommandGroup.View,
            "Editor wider",
            "Resize: editor wider (vertical divider right)",
            w => w.MoveVerticalDivider(1)
        ),
        new(
            Key.CursorUp.WithAlt,
            CommandGroup.View,
            "Top regions lower",
            "Resize: top regions lower (horizontal divider up)",
            w => w.MoveHorizontalDivider(-1)
        ),
        new(
            Key.CursorDown.WithAlt,
            CommandGroup.View,
            "Top regions higher",
            "Resize: top regions higher (horizontal divider down)",
            w => w.MoveHorizontalDivider(1)
        ),
        new(
            Key.F1.WithCtrl,
            CommandGroup.View,
            "Switch theme",
            "Switch between the light and the dark theme",
            w => w.Workbench.ToggleTheme()
        ),
        .. SampleCommands.All,
        .. GenerateCommands.All,
        .. InsertCommands.All,
        .. EmbeddedRuleCommands.All,
        .. ImportExportCommands.All,
        .. AssignmentCommands.Default,
        .. StepCommands.All,
        .. InspectCommands.All,
        .. RuleTestCommands.Default,
        .. RewriteCommands.Default,
        .. CompareCommands.Default,
        new(Key.Q.WithCtrl, CommandGroup.File, "Quit", "Quit (asks first when there are unsaved changes)", w => w.Quit())
        {
            Hint = "Quit",
        },
    ];

    /// <summary>Gets the choices. Each menu lists the choices of its group, as submenus, after its commands.</summary>
    public static IReadOnlyList<WorkbenchChoice> Choices { get; } =
    [
        new(
            CommandGroup.Rule,
            "Unknown policy",
            [.. PolicyOrder.Select(Workbench.PolicyText)],
            w => Array.IndexOf(PolicyOrder, w.Assignment.Policy),
            (w, index) => w.Workbench.SetPolicy(PolicyOrder[index])
        ),
        new(
            CommandGroup.View,
            "Theme",
            ["Light", "Dark"],
            w => w.Theme == ThemeVariant.Light ? 0 : 1,
            (w, index) =>
            {
                if (w.Workbench.Theme != (index == 0 ? ThemeVariant.Light : ThemeVariant.Dark))
                {
                    w.Workbench.ToggleTheme();
                }
            }
        ),
    ];

    // The policies in the order of the policy key (Ctrl+F3), so the submenu and the key agree.
    private static CollapsePolicy[] PolicyOrder =>
        [CollapsePolicy.UnknownAsFalse, CollapsePolicy.UnknownAsTrue, CollapsePolicy.UnknownIsError];
}
