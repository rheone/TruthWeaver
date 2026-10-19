namespace TruthWeaver.Tool.Tui.Commands;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The keys that step through an evaluation in the Tree pane. Esc leaves the step mode.</summary>
internal static class StepCommands
{
    /// <summary>Gets the key of the Next step command.</summary>
    public static Key NextKey => Key.F12.WithShift;

    /// <summary>Gets the key of the Previous step command.</summary>
    public static Key PreviousKey => Key.F11.WithShift;

    /// <summary>Gets the commands. <see cref="WorkbenchCommands.Default"/> includes them.</summary>
    public static IReadOnlyList<WorkbenchCommand> All { get; } =
    [
        new(
            NextKey,
            CommandGroup.Analyze,
            "Next evaluation step",
            "Step through the evaluation: highlight the next node and show the value it returned (Esc leaves step mode)",
            w => EvaluationStepController.For(w.Workbench).Next()
        ),
        new(
            PreviousKey,
            CommandGroup.Analyze,
            "Previous evaluation step",
            "Step back through the evaluation to the previous node (Esc leaves step mode)",
            w => EvaluationStepController.For(w.Workbench).Previous()
        ),
    ];
}
