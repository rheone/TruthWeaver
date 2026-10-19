namespace TruthWeaver.Tool.Tui.Commands;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The key that describes the part of the rule at the cursor or the selected node. The mouse does the same on hover (after a
/// short rest on a part), with the same text; where the terminal sends no mouse moves, this key still shows everything.
/// </summary>
internal static class InspectCommands
{
    /// <summary>Gets the key of the Inspect command.</summary>
    public static Key InspectKey => Key.F8.WithCtrl;

    /// <summary>Gets the fallback key of the Inspect command, for a terminal that sends no Ctrl+F-keys.</summary>
    public static Key InspectSecondKey => Key.M.WithAlt;

    /// <summary>Gets the commands. <see cref="WorkbenchCommands.Default"/> includes them.</summary>
    public static IReadOnlyList<WorkbenchCommand> All { get; } =
    [
        new(
            InspectKey,
            CommandGroup.Analyze,
            "Inspect",
            "Describe the part of the rule at the cursor or the selected node: what it means and what it does now (Esc closes)",
            w => w.Inspect()
        )
        {
            Hint = "Inspect",
            SecondKey = InspectSecondKey,
        },
    ];
}
