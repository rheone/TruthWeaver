namespace TruthWeaver.Tool.Tui.Workbench;

using Terminal.Gui.Input;

/// <summary>
/// One entry of a context menu: the title, the shortcut the menu shows, and where the shortcut is bound. The menu runs the
/// entry by sending the shortcut to the place that binds it, so the shortcut shown is the shortcut that works.
/// </summary>
/// <param name="Title">The title of the entry.</param>
/// <param name="Key">The shortcut.</param>
/// <param name="IsWindowCommand">
/// <see langword="true"/> when a command of the window binds the key (<see cref="WorkbenchCommands.Default"/>);
/// <see langword="false"/> when the pane under the pointer binds it.
/// </param>
internal sealed record ContextMenuEntry(string Title, Key Key, bool IsWindowCommand);
