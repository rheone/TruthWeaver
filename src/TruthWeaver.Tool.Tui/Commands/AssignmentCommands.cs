namespace TruthWeaver.Tool.Tui.Commands;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The keys of the assignment: clear the values, change the policy and load variables. Space, T, F and U on a term are handled by the Assign pane.</summary>
internal static class AssignmentCommands
{
    /// <summary>Gets the commands, for <see cref="WorkbenchCommands.Default"/>.</summary>
    public static IReadOnlyList<WorkbenchCommand> Default { get; } =
    [
        new(
            Key.F2.WithCtrl,
            CommandGroup.Rule,
            "All terms Unknown",
            "Give every term the value Unknown",
            w => w.Workbench.ClearAssignment()
        )
        {
            SecondKey = Key.U.WithAlt,
        },
        new(
            Key.F3.WithCtrl,
            CommandGroup.Rule,
            "Next Unknown policy",
            "Next policy: Unknown counts as False, as True, or is an error",
            w => w.Workbench.CyclePolicy()
        )
        {
            SecondKey = Key.P.WithAlt,
        },
        new(
            Key.F4.WithCtrl,
            CommandGroup.Rule,
            "Load variables...",
            "Load variables for from(...) references (JSON or YAML file)",
            w => w.Workbench.LoadVariables()
        )
        {
            SecondKey = Key.D.WithAlt,
        },
    ];
}
