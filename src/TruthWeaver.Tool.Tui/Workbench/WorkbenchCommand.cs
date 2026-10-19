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

    /// <summary>
    /// Gets the fallback key, built from Ctrl or Alt and a letter, or <see langword="null"/> for a command that has only
    /// one key. A terminal that swallows the primary key (Alt+arrows, Ctrl+PageUp/PageDown, a Ctrl+F-key or Shift+F11/F12)
    /// still reaches the command through this key. Both keys run the command, and both show in the menu and the key
    /// reference.
    /// </summary>
    public Key? SecondKey { get; init; }

    /// <summary>Gets whether a key is the key or the <see cref="SecondKey"/> of this command.</summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> when the key runs this command.</returns>
    public bool Matches(Key key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return (this.Key != Key.Empty && this.Key == key) || (this.SecondKey is { } second && second == key);
    }
}
