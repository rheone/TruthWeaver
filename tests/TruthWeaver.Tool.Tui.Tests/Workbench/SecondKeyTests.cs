namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using Terminal.Gui.Input;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Tui.Commands;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Theme;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The fallback key of a command (ticket 06): a terminal that swallows the primary key (Alt+arrows, Ctrl+PageUp/PageDown,
/// a Ctrl+F-key or Shift+F11/F12) still reaches the command through its <see cref="WorkbenchCommand.SecondKey"/>. Both
/// keys run the same action, and both show in the menu and the key reference.
/// </summary>
[Collection(TerminalCollection.Name)]
public sealed class SecondKeyTests
{
    /// <summary>Alt+G, the second key of Switch theme (Ctrl+F1), switches the theme the same way the primary key does.</summary>
    [Fact]
    public void Default_AltGSecondKeyOfSwitchTheme_SwitchesTheThemeLikeCtrlF1_Test()
    {
        using WorkbenchHarness harness = new("a");
        ThemeVariant before = harness.Workbench.Theme;

        harness.Press(Key.G.WithAlt);

        Assert.NotEqual(before, harness.Workbench.Theme);
    }

    /// <summary>Alt+W, the second key of Next pane (Ctrl+PageDown), switches the pane the same way the primary key does.</summary>
    [Fact]
    public void Default_AltWSecondKeyOfNextPane_SwitchesToTheRegisteredPaneLikeCtrlPageDown_Test()
    {
        // Two Side panes, so switching the pane has somewhere to go.
        IWorkbenchPane[] panes = [new RuleTextPane(), new CanonicalTextPane(), new ViewPane()];
        using WorkbenchHarness harness = new("a", panes);
        harness.Window.FocusRegion(PaneSlot.Side);

        harness.Press(Key.W.WithAlt);

        Assert.Equal("View", harness.Window.ActivePaneTitle(PaneSlot.Side));
        Assert.Equal(PaneSlot.Side, harness.Window.FocusedSlot());
    }

    /// <summary>Alt+Q, the second key of Previous pane (Ctrl+PageUp), switches back to the pane before, like the primary key.</summary>
    [Fact]
    public void Default_AltQSecondKeyOfPreviousPane_SwitchesToThePaneBeforeLikeCtrlPageUp_Test()
    {
        IWorkbenchPane[] panes = [new RuleTextPane(), new CanonicalTextPane(), new ViewPane()];
        using WorkbenchHarness harness = new("a", panes);
        harness.Window.FocusRegion(PaneSlot.Side);
        harness.Press(Key.W.WithAlt);
        string? after = harness.Window.ActivePaneTitle(PaneSlot.Side);

        harness.Press(Key.Q.WithAlt);

        Assert.NotEqual(after, harness.Window.ActivePaneTitle(PaneSlot.Side));
        Assert.Equal(PaneSlot.Side, harness.Window.FocusedSlot());
    }

    /// <summary>Alt+C, the second key of Editor wider (Alt+Right), widens the editor region the same way the primary key does.</summary>
    [Fact]
    public void Default_AltCSecondKeyOfEditorWider_WidensTheEditorRegionLikeAltRight_Test()
    {
        using WorkbenchHarness harness = new("a");
        int before = harness.Window.ActivePaneViews[PaneSlot.Editor].SuperView!.Frame.Width;

        harness.Press(Key.C.WithAlt);

        Assert.Equal(60, harness.Workbench.Layout.EditorWidthPercent);
        Assert.True(harness.Window.ActivePaneViews[PaneSlot.Editor].SuperView!.Frame.Width > before);
    }

    /// <summary>Alt+Z, the second key of Editor narrower (Alt+Left), narrows the editor region the same way the primary key does.</summary>
    [Fact]
    public void Default_AltZSecondKeyOfEditorNarrower_NarrowsTheEditorRegionLikeAltLeft_Test()
    {
        using WorkbenchHarness harness = new("a");
        int before = harness.Window.ActivePaneViews[PaneSlot.Editor].SuperView!.Frame.Width;

        harness.Press(Key.Z.WithAlt);

        Assert.Equal(50, harness.Workbench.Layout.EditorWidthPercent);
        Assert.True(harness.Window.ActivePaneViews[PaneSlot.Editor].SuperView!.Frame.Width < before);
    }

    /// <summary>Alt+M, the second key of Inspect (Ctrl+F8), opens the inspect box the same way the primary key does.</summary>
    [Fact]
    public void Default_AltMSecondKeyOfInspect_OpensTheInspectBoxLikeCtrlF8_Test()
    {
        using WorkbenchHarness harness = new("isActive");

        harness.Press(CommandKeys.InspectSecond);

        Assert.NotEmpty(harness.Window.InspectText);
    }

    /// <summary>
    /// Every second key of every default command, pressed with the editor focused, reaches its command: the window runs that
    /// command and no other. A key that a terminal turns into another key, or that the editor takes first, fails here and
    /// names the key and the command. The test replaces the action of each command with a recorder, so it proves that the
    /// key reached the command and not what the command does; the tests above cover the effect of the pane and divider
    /// keys. Only a terminal driver can prove that a real terminal sends the key at all, and this test cannot.
    /// </summary>
    [Fact]
    public void RunCommand_EverySecondKeyPressedInTheFocusedEditor_RunsItsCommand_Test()
    {
        List<string> failures = [];
        foreach (WorkbenchCommand command in WorkbenchCommands.Default.Where(c => c.SecondKey is not null))
        {
            Key second = CommandKeys.SecondOf(command);

            string[] ran = RanByKeyInTheEditor(second, extraEditorBinding: false);

            if (!ran.SequenceEqual([command.Title]))
            {
                failures.Add($"{command.SecondKey} ({command.Title}) ran [{string.Join(", ", ran)}]");
            }
        }

        Assert.Empty(failures);
    }

    /// <summary>
    /// A second key that the focused editor binds is swallowed before the window sees it, and the recorder test reports that
    /// the command did not run. This shows that the every-second-key test fails on a clash: it adds a binding for Alt+G to
    /// the editor, as a temporary clash would.
    /// </summary>
    [Fact]
    public void RunCommand_SecondKeyBoundByTheFocusedEditor_DoesNotRunItsCommand_Test()
    {
        string[] ran = RanByKeyInTheEditor(Key.G.WithAlt, extraEditorBinding: true);

        Assert.Empty(ran);
    }

    /// <summary>
    /// Ctrl+F12 with the editor focused runs Save into the document: after the author agrees to the warning, the rule goes
    /// into the document file.
    /// </summary>
    [Fact]
    public void Default_CtrlF12InTheEditor_SavesTheRuleIntoTheDocument_Test()
    {
        string folder = Directory.CreateTempSubdirectory("tw-ctrlf12-").FullName;
        WorkbenchServices previous = WorkbenchServices.Current;
        try
        {
            string file = Path.Combine(folder, "policies.json");
            File.WriteAllText(file, "{\n  \"rules\": { \"review\": \"isActive AND isReviewer\" }\n}\n");
            ScriptedServices services = new();
            services.Dialogs.Agree = true;
            WorkbenchServices.Current = services.Services;
            using WorkbenchHarness harness = new();
            Assert.True(EmbeddedRuleController.For(harness.Workbench).OpenAt(file, "$.rules.review"));
            harness.Workbench.Document.SetText("isActive AND isOwner");
            harness.Window.FocusRegion(PaneSlot.Editor);

            harness.Press(Key.F12.WithCtrl);

            Assert.Contains("isOwner", File.ReadAllText(file), StringComparison.Ordinal);
            Assert.False(harness.Workbench.Document.IsDirty);
            Assert.Single(services.Dialogs.Confirmations);
        }
        finally
        {
            WorkbenchServices.Current = previous;
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    /// The key reference shows both keys of a command that has a second one, joined by "or", on the same line as its
    /// description.
    /// </summary>
    [Fact]
    public void HelpText_CommandWithASecondKey_ShowsBothKeysOnOneLine_Test()
    {
        string help = WorkbenchWindow.HelpText(WorkbenchCommands.Default);
        string[] lines = help.Split('\n');

        string themeLine = lines.Single(line =>
            line.EndsWith("Switch between the light and the dark theme", StringComparison.Ordinal)
        );
        Assert.StartsWith("  Ctrl+F1 or Alt+G", themeLine, StringComparison.Ordinal);
    }

    /// <summary>The menu entry of a command with a second key shows that key, so the menu names both ways to run it.</summary>
    [Fact]
    public void MenuBar_SwitchTheme_ShowsTheSecondKey_Test()
    {
        using WorkbenchHarness harness = new();

        MenuItem item = harness.Window.MenuBar.GetMenuItemsWith(i => i.Title == "Switch theme").Single();

        Assert.Equal("or Alt+G", item.HelpText);
        Assert.Equal(Key.F1.WithCtrl, item.Key);
    }

    /// <summary>No two keys of the default commands, primary or second, are the same key.</summary>
    [Fact]
    public void Default_SecondKeys_AreDistinctFromEveryOtherKey_Test()
    {
        Key[] primary = [.. WorkbenchCommands.Default.Select(c => c.Key).Where(k => k != Key.Empty)];
        Key[] second = [.. WorkbenchCommands.Default.Where(c => c.SecondKey is not null).Select(c => c.SecondKey!)];

        Assert.NotEmpty(second);
        Assert.All(second, key => Assert.DoesNotContain(key, primary));
        Assert.Equal(second.Length, second.Distinct().Count());
    }

    // Presses a key with the editor focused, in a window whose commands only record their titles, and returns the titles of
    // the commands that ran. With extraEditorBinding the editor also binds the key, as a clash would.
    private static string[] RanByKeyInTheEditor(Key key, bool extraEditorBinding)
    {
        List<string> ran = [];
        WorkbenchCommand[] recorders = [.. WorkbenchCommands.Default.Select(c => c with { Execute = _ => ran.Add(c.Title) })];
        using WorkbenchHarness harness = new("isActive", commands: recorders);
        if (extraEditorBinding)
        {
            (harness.Window.MostFocused ?? throw new InvalidOperationException("No view has the focus.")).KeyBindings.Add(
                key,
                Command.Copy
            );
        }

        harness.Press(key);

        return [.. ran];
    }
}
