namespace TruthWeaver.Tool.Tui.Workbench;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Commands;

/// <summary>
/// The keys of the workbench: the place to register a new command. The keys avoid those the editor uses for itself
/// (Ctrl+A to Ctrl+F, Ctrl+N, Ctrl+P, Ctrl+V, Ctrl+W, Ctrl+X and the arrows without Alt).
/// </summary>
internal static class WorkbenchCommands
{
    /// <summary>Gets the commands, in the order the key help overlay lists them.</summary>
    public static IReadOnlyList<WorkbenchCommand> Default { get; } =
    [
        new(Key.F1, "Show or hide this key help", w => w.ToggleHelp()),
        new(Key.O.WithCtrl, "Open a rule file (rule text, JSON or YAML)", w => w.Workbench.Open()),
        new(Key.S.WithCtrl, "Save", w => w.Workbench.Save()),
        new(Key.F12, "Save as (the extension selects the format)", w => w.Workbench.SaveAs()),
        new(Key.Z.WithCtrl, "Undo", w => w.Workbench.Undo()),
        new(Key.Y.WithCtrl, "Redo", w => w.Workbench.Redo()),
        new(Key.F8, "Format: canonical", w => w.Workbench.Format(pretty: false)),
        new(Key.F8.WithShift, "Format: pretty", w => w.Workbench.Format(pretty: true)),
        new(Key.F6, "Next region", w => w.FocusNextRegion(1)),
        new(Key.F6.WithShift, "Previous region", w => w.FocusNextRegion(-1)),
        new(Key.PageDown.WithCtrl, "Next pane in this region", w => w.SwitchPane(1)),
        new(Key.PageUp.WithCtrl, "Previous pane in this region", w => w.SwitchPane(-1)),
        new(Key.F11, "Maximise or restore this region", w => w.ToggleMaximised()),
        new(Key.CursorLeft.WithAlt, "Resize: editor narrower (vertical divider left)", w => w.MoveVerticalDivider(-1)),
        new(Key.CursorRight.WithAlt, "Resize: editor wider (vertical divider right)", w => w.MoveVerticalDivider(1)),
        new(Key.CursorUp.WithAlt, "Resize: top regions lower (horizontal divider up)", w => w.MoveHorizontalDivider(-1)),
        new(Key.CursorDown.WithAlt, "Resize: top regions higher (horizontal divider down)", w => w.MoveHorizontalDivider(1)),
        new(Key.F1.WithCtrl, "Switch between the light and the dark theme", w => w.Workbench.ToggleTheme()),
        new(Key.Q.WithCtrl, "Quit (asks first when there are unsaved changes)", w => w.Quit()),
        .. EmbeddedRuleCommands.All,
        .. ImportExportCommands.All,
        .. AssignmentCommands.Default,
        .. RuleTestCommands.Default,
        .. RewriteCommands.Default,
        .. CompareCommands.Default,
    ];
}
