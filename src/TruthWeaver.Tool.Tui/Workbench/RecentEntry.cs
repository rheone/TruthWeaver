namespace TruthWeaver.Tool.Tui.Workbench;

/// <summary>One line of the Open recent submenu.</summary>
/// <param name="Path">The full path of the file.</param>
/// <param name="Title">The text of the menu entry, with its number.</param>
/// <param name="Missing"><see langword="true"/> when the file no longer exists; choosing the entry then removes it from the list.</param>
internal sealed record RecentEntry(string Path, string Title, bool Missing);
