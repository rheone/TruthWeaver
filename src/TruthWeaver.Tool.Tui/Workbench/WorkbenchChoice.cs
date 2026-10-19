namespace TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// A setting with a small, fixed set of values, such as the theme. Its menu entry opens a submenu with one entry for each
/// value and marks the current one, so the author picks the value in one step and does not cycle through the others.
/// </summary>
/// <param name="Group">The menu that holds the choice.</param>
/// <param name="Title">The name of the submenu, such as <c>Theme</c>.</param>
/// <param name="Options">The values, in the order the submenu lists them.</param>
/// <param name="Current">Gets the index in <paramref name="Options"/> of the current value.</param>
/// <param name="Select">Sets the value with the given index in <paramref name="Options"/>.</param>
internal sealed record WorkbenchChoice(
    CommandGroup Group,
    string Title,
    IReadOnlyList<string> Options,
    Func<Workbench, int> Current,
    Action<WorkbenchWindow, int> Select
);
