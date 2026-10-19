namespace TruthWeaver.Tool.Tui.Commands;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The key of the samples: a list of the sample rules. The File menu also has a Samples submenu with the same entries.</summary>
internal static class SampleCommands
{
    /// <summary>The text of the accept button of the samples list: it names the action.</summary>
    public const string UseButton = "_Use";

    /// <summary>Gets the key of the command that lists the samples.</summary>
    public static Key SamplesKey => Key.F9.WithShift;

    /// <summary>Gets the commands. <see cref="WorkbenchCommands.Default"/> includes them.</summary>
    public static IReadOnlyList<WorkbenchCommand> All { get; } =
    [
        new(
            SamplesKey,
            CommandGroup.File,
            "Samples...",
            "Choose a sample rule from a list: each shows a part of the logic and replaces the rule (Ctrl+Z undoes it)",
            w => Choose(w.Workbench, WorkbenchServices.Current)
        ),
    ];

    /// <summary>Gets the line of a sample in the list: its name and what it shows.</summary>
    /// <param name="sample">The sample.</param>
    /// <returns>The line.</returns>
    public static string LineOf(SampleRule sample)
    {
        ArgumentNullException.ThrowIfNull(sample);

        return $"{sample.Name} - {sample.Description}";
    }

    /// <summary>Asks which sample to use and puts it in place of the rule. Unsaved changes need the author's consent first.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <param name="services">The dialogs.</param>
    /// <returns><see langword="true"/> when a sample replaced the rule.</returns>
    public static bool Choose(Workbench workbench, WorkbenchServices services)
    {
        ArgumentNullException.ThrowIfNull(workbench);
        ArgumentNullException.ThrowIfNull(services);

        int? choice = services.Dialogs.Choose("Sample rules", [.. SampleRules.All.Select(LineOf)], UseButton);
        return choice is { } index
            && index >= 0
            && index < SampleRules.All.Count
            && workbench.OpenSample(SampleRules.All[index]);
    }
}
