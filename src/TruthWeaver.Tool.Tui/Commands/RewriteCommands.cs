namespace TruthWeaver.Tool.Tui.Commands;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The keys of the rewrites: choose a rewrite, and compare with the last saved or accepted version. Accept and discard are keys of the Rewrite pane.</summary>
internal static class RewriteCommands
{
    /// <summary>Gets the commands, for <see cref="WorkbenchCommands.Default"/>.</summary>
    public static IReadOnlyList<WorkbenchCommand> Default { get; } =
    [
        new(
            Key.R.WithCtrl,
            "Rewrite the rule (Simplify, normal forms, ...) and show the result in the Rewrite pane",
            w =>
            {
                if (RewriteController.For(w.Workbench).Choose())
                {
                    w.ShowPane("Rewrite");
                }
            }
        ),
        new(
            Key.G.WithCtrl,
            "Compare the rule with the last saved or accepted version",
            w =>
            {
                if (RewriteController.For(w.Workbench).Compare())
                {
                    w.ShowPane("Rewrite");
                }
            }
        ),
    ];
}
