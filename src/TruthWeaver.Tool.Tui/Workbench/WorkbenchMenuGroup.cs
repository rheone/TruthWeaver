namespace TruthWeaver.Tool.Tui.Workbench;

/// <summary>One menu of the menu bar: its group and the commands and choices it lists, in order.</summary>
/// <param name="Group">The group.</param>
/// <param name="Commands">The commands of the group.</param>
/// <param name="Choices">The choices of the group; the menu lists them after the commands.</param>
internal sealed record WorkbenchMenuGroup(
    CommandGroup Group,
    IReadOnlyList<WorkbenchCommand> Commands,
    IReadOnlyList<WorkbenchChoice> Choices
);
