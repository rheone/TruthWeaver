namespace TruthWeaver.Tool.Tui.Workbench;

using Terminal.Gui.Input;

/// <summary>
/// A command of the workbench: its key, the menu that holds it and what it does. The menu bar shows each command in the menu
/// of its group, with its key, and the key reference lists the commands by group.
/// </summary>
/// <param name="Key">The key, or <see cref="Key.Empty"/> for a command that only the menu runs.</param>
/// <param name="Group">The menu that holds the command. Every command names one.</param>
/// <param name="Title">The short name of the command in its menu, such as <c>Save as</c>.</param>
/// <param name="Description">What the command does, for the key reference.</param>
/// <param name="Execute">The action.</param>
internal sealed record WorkbenchCommand(
    Key Key,
    CommandGroup Group,
    string Title,
    string Description,
    Action<WorkbenchWindow> Execute
)
{
    /// <summary>
    /// Gets the word for the hint line at the bottom of the window, such as <c>Save</c>, or <see langword="null"/> when the
    /// hint line does not show the command. Only the most common commands set it.
    /// </summary>
    public string? Hint { get; init; }
}
