namespace TruthWeaver.Tool.Tui.Commands;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The keys of the rule tests: save the assignment as a case and run the file. Enter and Delete on a case are handled by the Tests pane.</summary>
internal static class RuleTestCommands
{
    /// <summary>Gets the commands, for <see cref="WorkbenchCommands.Default"/>.</summary>
    public static IReadOnlyList<WorkbenchCommand> Default { get; } =
    [
        new(
            Key.F5.WithCtrl,
            CommandGroup.Analyze,
            "Save the values as a test case",
            "Save the assignment as a case of the rule test file",
            w => w.Workbench.SaveCase()
        ),
        new(
            Key.F7.WithCtrl,
            CommandGroup.Analyze,
            "Run the rule tests",
            "Run the cases of the rule test file on the open rule",
            w => _ = w.Workbench.RunRuleTestsAsync()
        ),
    ];
}
