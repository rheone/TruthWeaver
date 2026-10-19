namespace TruthWeaver.Tool.Tui.Commands;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The keys that open, switch and save a rule inside a larger JSON or YAML document.</summary>
internal static class EmbeddedRuleCommands
{
    /// <summary>Gets the commands. <see cref="WorkbenchCommands.Default"/> includes them.</summary>
    public static IReadOnlyList<WorkbenchCommand> All { get; } =
    [
        new(
            Key.F2,
            CommandGroup.File,
            "Open a rule in a document...",
            "Open a rule inside a JSON or YAML document (by path or by browsing)",
            w => EmbeddedRuleController.For(w.Workbench).Open()
        ),
        new(
            Key.F2.WithShift,
            CommandGroup.File,
            "Switch rule of the document...",
            "Switch to another rule of the open document",
            w => EmbeddedRuleController.For(w.Workbench).Switch()
        ),
        new(
            Key.F12.WithCtrl,
            CommandGroup.File,
            "Save into the document",
            "Save the rule into its document by writing the whole document again (warns first)",
            w => EmbeddedRuleController.For(w.Workbench).SaveRoundTrip()
        )
        {
            SecondKey = Key.O.WithAlt,
        },
    ];
}
