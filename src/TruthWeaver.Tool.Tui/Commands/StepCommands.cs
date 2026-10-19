namespace TruthWeaver.Tool.Tui.Commands;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The keys that step through an evaluation in the Tree pane. Esc leaves the step mode.</summary>
internal static class StepCommands
{
    /// <summary>Gets the commands. <see cref="WorkbenchCommands.Default"/> includes them.</summary>
    public static IReadOnlyList<WorkbenchCommand> All { get; } =
    [
        new(
            Key.F12.WithShift,
            CommandGroup.Analyze,
            "Next evaluation step",
            "Step through the evaluation: highlight the next node and show the value it returned (Esc leaves step mode)",
            w => EvaluationStepController.For(w.Workbench).Next()
        )
        {
            SecondKey = Key.J.WithAlt,
        },
        new(
            Key.F11.WithShift,
            CommandGroup.Analyze,
            "Previous evaluation step",
            "Step back through the evaluation to the previous node (Esc leaves step mode)",
            w => EvaluationStepController.For(w.Workbench).Previous()
        )
        {
            SecondKey = Key.K.WithAlt,
        },
    ];
}
