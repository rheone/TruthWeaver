namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// A command key of the workbench must reach the window wherever the focus is. A focused view that binds the same key takes
/// it first, so the command never runs. This test is the list of keys to avoid. It covers the primary key of every command
/// and, for a command that sets one, the <see cref="WorkbenchCommand.SecondKey"/> too: the fallback a terminal that
/// swallows the primary key (Alt+arrows, Ctrl+PageUp/PageDown, a Ctrl+F-key or Shift+F11/F12) is meant to reach.
/// </summary>
[Collection(TerminalCollection.Name)]
public sealed class CommandKeyClashTests
{
    // The keys the window owns on purpose: the document keeps the history, so the editor reports its own undo and redo as
    // not handled and the keys go on to the window.
    private static readonly Key[] OwnedByTheWindow = [Key.Z.WithCtrl, Key.Y.WithCtrl];

    /// <summary>
    /// No command key (primary or second) is also a key binding of the editor, a list or a text field, except the keys the
    /// window owns on purpose (Ctrl+Z and Ctrl+Y). A new command on such a key fails here.
    /// </summary>
    [Fact]
    public void Default_CommandKeys_AreNotBoundByTheEditorAListOrATextField_Test()
    {
        using RuleEditorView editor = new();
        using ListView list = new();
        using TextField field = new();
        View[] views = [editor, list, field];
        Key[] commandKeys = [.. AllKeysOf(WorkbenchCommands.Default)];

        string[] clashes =
        [
            .. from view in views
            from binding in view.KeyBindings.GetBindings()
            where commandKeys.Contains(binding.Key) && !OwnedByTheWindow.Contains(binding.Key)
            select $"{binding.Key} is bound by {view.GetType().Name}",
        ];

        Assert.Empty(clashes);
    }

    /// <summary>
    /// No two commands share a key: every primary and second key, taken together, is distinct, and none of them is the key
    /// that opens the menu bar.
    /// </summary>
    [Fact]
    public void Default_CommandKeys_AreAllDistinctAndNoneIsTheMenuBarKey_Test()
    {
        Key[] keys = [.. AllKeysOf(WorkbenchCommands.Default)];

        Assert.Equal(keys.Length, keys.Distinct().Count());
        Assert.DoesNotContain(WorkbenchMenus.MenuBarKey, keys);
    }

    // Every key a command binds: its Key (unless it has none) and its SecondKey (when it has one).
    private static IEnumerable<Key> AllKeysOf(IEnumerable<WorkbenchCommand> commands)
    {
        foreach (WorkbenchCommand command in commands)
        {
            if (command.Key != Key.Empty)
            {
                yield return command.Key;
            }

            if (command.SecondKey is { } second)
            {
                yield return second;
            }
        }
    }
}
