namespace TruthWeaver.Tool.Tui.Commands;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The key that loads a second rule to compare with the open rule. The other keys are keys of the Compare pane.</summary>
internal static class CompareCommands
{
    /// <summary>Gets the commands, for <see cref="WorkbenchCommands.Default"/>.</summary>
    public static IReadOnlyList<WorkbenchCommand> Default { get; } =
    [
        new(
            Key.F9.WithCtrl,
            CommandGroup.Analyze,
            "Compare with a second rule...",
            "Load a second rule (file or clipboard, read-only) and compare it with the open rule",
            w =>
            {
                if (CompareController.For(w.Workbench).Open())
                {
                    w.ShowPane("Compare");
                }
            }
        )
        {
            SecondKey = Key.B.WithAlt,
        },
    ];
}
