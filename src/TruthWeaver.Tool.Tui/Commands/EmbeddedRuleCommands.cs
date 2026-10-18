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
            "Open a rule inside a JSON or YAML document (by path or by browsing)",
            w => EmbeddedRuleController.For(w.Workbench).Open()
        ),
        new(
            Key.F2.WithShift,
            "Switch to another rule of the open document",
            w => EmbeddedRuleController.For(w.Workbench).Switch()
        ),
        new(
            Key.F3,
            "Save the rule into its document by writing the whole document again (warns first)",
            w => EmbeddedRuleController.For(w.Workbench).SaveRoundTrip()
        ),
    ];
}
