namespace TruthWeaver.Tool.Tui.Commands;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The keys of import, conversion, export and validation.</summary>
internal static class ImportExportCommands
{
    /// <summary>Gets the commands. <see cref="WorkbenchCommands.Default"/> includes them.</summary>
    public static IReadOnlyList<WorkbenchCommand> All { get; } =
    [
        new(
            Key.F4,
            CommandGroup.Rule,
            "Convert...",
            "Convert the rule: choose canonical rule text, formatted (pretty) rule text, JSON or YAML from a list",
            w => ImportController.For(w.Workbench).Convert()
        ),
        new(
            Key.F4.WithShift,
            CommandGroup.Rule,
            "Convert to the next format",
            "Convert the rule to the next format: rule text, JSON, YAML",
            w => ImportController.For(w.Workbench).CycleFormat(1)
        ),
        new(
            Key.F5,
            CommandGroup.Export,
            "Export...",
            "Export dialog: any artifact to the clipboard or a file, or all to a folder",
            w => ExportController.For(w.Workbench).ShowMenu()
        )
        {
            Hint = "Export",
        },
        new(
            Key.F7,
            CommandGroup.Analyze,
            "Validate",
            "Validate every level and update the validation panel",
            w => ValidationController.For(w.Workbench).Run()
        )
        {
            Hint = "Validate",
        },
        new(
            Key.F7.WithShift,
            CommandGroup.Analyze,
            "Strict validation on or off",
            "Turn strict validation (warnings fail) on or off",
            w => ValidationController.For(w.Workbench).ToggleStrict()
        ),
        new(
            Key.F9,
            CommandGroup.File,
            "Import...",
            "Import a rule from the clipboard or a file, in a named or detected format",
            w => ImportController.For(w.Workbench).Import()
        ),
    ];
}
