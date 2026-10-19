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
    /// <summary>
    /// The keys the window owns on purpose: Ctrl+Z (undo) and Ctrl+Y (redo). The document keeps the history, so the editor
    /// reports its own undo and redo as not handled and the keys go on to the window. A view may bind these keys without a
    /// clash.
    /// </summary>
    private static readonly Key[] WindowOwnedKeys = [Key.Z.WithCtrl, Key.Y.WithCtrl];

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

        string[] clashes = ClashesOf(views, AllKeysOf(WorkbenchCommands.Default));

        Assert.Empty(clashes);
    }

    /// <summary>
    /// The clash check finds a key that a view binds: a command key added as a binding to the editor, a list or a text field
    /// is reported with the key and the view. This shows that the clash test would fail for such a key, which no view binds
    /// today. The key is read from a command. The test also passes a window-owned key, which these views bind themselves, and
    /// shows that it is not reported.
    /// </summary>
    [Fact]
    public void ClashesOf_CommandKeyBoundByTheEditorAListAndATextField_IsReportedForEachView_Test()
    {
        Key key = CommandKeys.SecondOf("Switch theme");
        using RuleEditorView editor = new();
        using ListView list = new();
        using TextField field = new();
        View[] views = [editor, list, field];
        foreach (View view in views)
        {
            view.KeyBindings.Add(key, Command.Accept);
        }

        string[] clashes = ClashesOf(views, [key, WindowOwnedKeys[0]]);

        Assert.Equal([.. views.Select(v => $"{key} is bound by {v.GetType().Name}")], clashes);
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

    /// <summary>
    /// No command key (primary or second) is a key that a terminal sends as another key or that a host takes for itself:
    /// Tab, Enter, Esc and Backspace, the Ctrl letters that are the same bytes (Ctrl+I, Ctrl+J, Ctrl+M, Ctrl+H and Ctrl+[),
    /// Ctrl+L (redraw) and Ctrl+W (closes a tab in some hosts). Such a key never arrives as the key the command names.
    /// </summary>
    [Fact]
    public void Default_CommandKeys_AreNotKeysATerminalSendsAsAnotherKey_Test()
    {
        Key[] ambiguous =
        [
            Key.Tab,
            Key.Enter,
            Key.Esc,
            Key.Backspace,
            Key.I.WithCtrl,
            Key.J.WithCtrl,
            Key.M.WithCtrl,
            Key.H.WithCtrl,
            new Key('[').WithCtrl,
            Key.L.WithCtrl,
            Key.W.WithCtrl,
        ];

        string[] found = [.. AllKeysOf(WorkbenchCommands.Default).Where(ambiguous.Contains).Select(k => k.ToString())];

        Assert.Empty(found);
    }

    // The keys of a command list that a view binds, except the keys the window owns on purpose.
    private static string[] ClashesOf(IEnumerable<View> views, IEnumerable<Key> commandKeys)
    {
        Key[] keys = [.. commandKeys];
        return
        [
            .. from view in views
            from binding in view.KeyBindings.GetBindings()
            where keys.Contains(binding.Key) && !WindowOwnedKeys.Contains(binding.Key)
            select $"{binding.Key} is bound by {view.GetType().Name}",
        ];
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
