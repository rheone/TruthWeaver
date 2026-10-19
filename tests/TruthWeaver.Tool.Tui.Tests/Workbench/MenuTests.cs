namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using System.Drawing;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using TruthWeaver.Abstractions;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Theme;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// Tests of the menu bar: every command is in exactly one menu with its key, and the menus open and run their entries with
/// the keyboard and the mouse in a headless run of the application.
/// </summary>
[Collection(TerminalCollection.Name)]
public sealed class MenuTests
{
    /// <summary>
    /// Every command names a defined group and a title, and the menus hold each command exactly once, in the order of the
    /// groups. A new command that names no group does not compile; one with an undefined group fails here.
    /// </summary>
    [Fact]
    public void GroupsOf_DefaultCommands_PutsEveryCommandInExactlyOneMenu_Test()
    {
        IReadOnlyList<WorkbenchMenuGroup> menus = WorkbenchMenus.GroupsOf(WorkbenchCommands.Default, WorkbenchCommands.Choices);

        WorkbenchCommand[] listed = [.. menus.SelectMany(m => m.Commands)];
        Assert.All(WorkbenchCommands.Default, c => Assert.True(Enum.IsDefined(c.Group), c.Description));
        Assert.All(WorkbenchCommands.Default, c => Assert.False(string.IsNullOrWhiteSpace(c.Title), c.Description));
        Assert.All(WorkbenchCommands.Default, c => Assert.Single(listed, l => ReferenceEquals(l, c)));
        Assert.Equal(WorkbenchCommands.Default.Count, listed.Length);
        Assert.Equal(
            [
                CommandGroup.File,
                CommandGroup.Edit,
                CommandGroup.Insert,
                CommandGroup.Rule,
                CommandGroup.Analyze,
                CommandGroup.Export,
                CommandGroup.View,
                CommandGroup.Help,
            ],
            menus.Select(m => m.Group)
        );
    }

    /// <summary>The menu bar has an entry for each command, with the title and the key of the command.</summary>
    [Fact]
    public void MenuBar_DefaultCommands_ShowsEachCommandWithItsKey_Test()
    {
        using WorkbenchHarness harness = new();

        MenuItem[] items = [.. harness.Window.MenuBar.GetMenuItemsWith(_ => true)];

        Assert.All(WorkbenchCommands.Default, c => Assert.Single(items, i => i.Title == c.Title && i.Key == c.Key));
    }

    /// <summary>
    /// The entries of the Insert menu are drawn in the color of their kind, and a switch of the theme colors them again. The
    /// title names the kind as well ("Operators", "Constants", "Predicate...", "Variable..."), so color is not the only mark.
    /// </summary>
    [Fact]
    public void MenuBar_InsertEntries_AreColoredByKindAndFollowTheTheme_Test()
    {
        using WorkbenchHarness harness = new();
        MenuItem[] items = [.. harness.Window.MenuBar.GetMenuItemsWith(_ => true)];
        MenuItem Find(string title) => items.First(i => i.Title == title);

        Assert.Equal(ThemePalette.Dark.Operator, Find("AND").CommandView!.GetAttributeForRole(VisualRole.Normal).Foreground);
        Assert.Equal(ThemePalette.Dark.Constant, Find("True").CommandView!.GetAttributeForRole(VisualRole.Normal).Foreground);
        Assert.Equal(
            ThemePalette.Dark.Predicate,
            Find("Predicate...").CommandView!.GetAttributeForRole(VisualRole.Normal).Foreground
        );
        Assert.Equal(
            ThemePalette.Dark.Variable,
            Find("Variable...").CommandView!.GetAttributeForRole(VisualRole.Normal).Foreground
        );
        Assert.Equal(ThemePalette.Dark.Description, Find("AND").HelpView.GetAttributeForRole(VisualRole.Normal).Foreground);

        harness.Press(Key.F1.WithCtrl);

        Assert.Equal(ThemePalette.Light.Operator, Find("AND").CommandView!.GetAttributeForRole(VisualRole.Normal).Foreground);
        harness.Press(Key.F1.WithCtrl);
    }

    /// <summary>F1 opens the menu bar, and Esc closes it and gives the focus back to the editor.</summary>
    [Fact]
    public void F1_InTheTerminal_OpensTheMenusAndEscClosesThem_Test()
    {
        using MenuSession session = new("a");

        session.Press(Key.F1);
        bool opened = session.Window.MenuBar.IsOpen();
        session.Press(Key.Esc);

        Assert.True(opened);
        Assert.False(session.Window.MenuBar.IsOpen());
        Assert.Equal(PaneSlot.Editor, session.Window.FocusedSlot());
    }

    /// <summary>Alt+E opens the Edit menu; the arrows move to an entry and Enter runs it, as its key does.</summary>
    [Fact]
    public void AltE_DownToFormatCanonical_FormatsTheRule_Test()
    {
        using MenuSession session = new("a   and b");

        session.Press(Key.E.WithAlt, Key.CursorDown, Key.CursorDown, Key.Enter);

        Assert.Equal("a AND b", session.Workbench.Document.Text);
        Assert.False(session.Window.MenuBar.IsOpen());
    }

    /// <summary>A click on a menu title opens it, and a click on an entry runs the command.</summary>
    [Fact]
    public void Click_EditThenFormatCanonical_FormatsTheRule_Test()
    {
        using MenuSession session = new("a   and b");
        MenuBarItem edit = session.Window.MenuBar.SubViews.OfType<MenuBarItem>().Single(i => i.Title == "_Edit");

        session.Click(edit.FrameToScreen().Location);
        MenuItem format = session.Window.MenuBar.GetMenuItemsWith(i => i.Title == "Format: canonical").Single();
        session.Click(format.FrameToScreen().Location);

        Assert.Equal("a AND b", session.Workbench.Document.Text);
    }

    /// <summary>The policy choice of the Rule menu marks the current policy when the menu opens, and sets the chosen one.</summary>
    [Fact]
    public void PolicyChoice_OpenAndChoose_MarksTheCurrentPolicyAndSetsTheChosenOne_Test()
    {
        using MenuSession session = new("a");
        WorkbenchChoice policy = WorkbenchCommands.Choices.Single(c => c.Title == "Unknown policy");

        session.Press(Key.R.WithAlt);
        string[] marked =
        [
            .. session
                .Window.MenuBar.GetMenuItemsWith(i => i.Title.StartsWith("(*) ", StringComparison.Ordinal))
                .Select(i => i.Title),
        ];
        session.Press(Key.Esc);
        policy.Select(session.Window, 2);

        Assert.Contains(WorkbenchMenus.OptionTitle(Workbench.PolicyText(CollapsePolicy.UnknownAsFalse), true), marked);
        Assert.Equal(CollapsePolicy.UnknownIsError, session.Workbench.Assignment.Policy);
        Assert.Equal(2, policy.Current(session.Workbench));
    }

    /// <summary>A headless run of the application with the workbench window, for keys and clicks through Terminal.Gui.</summary>
    private sealed class MenuSession : IDisposable
    {
        private readonly IApplication app;
        private readonly SessionToken? token;

        public MenuSession(string text)
        {
            this.app = Application.Create();
            this.app.Init(DriverRegistry.Names.ANSI);
            this.app.Driver!.SetScreenSize(100, 30);
            this.Workbench = new(RuleDocument.Create(text), ToolSettings.Default, new ScriptedPrompts());
            this.Window = new WorkbenchWindow(this.Workbench);
            this.token = this.app.Begin(this.Window);
            this.Window.FocusRegion(PaneSlot.Editor);
            this.app.LayoutAndDraw(true);
        }

        public Workbench Workbench { get; }

        public WorkbenchWindow Window { get; }

        public void Press(params Key[] keys)
        {
            foreach (Key key in keys)
            {
                _ = this.app.Keyboard.RaiseKeyDownEvent(key);
                this.app.LayoutAndDraw(true);
            }
        }

        // A click one column into the view, as the terminal reports it: a press, a release and a click.
        public void Click(Point location)
        {
            Point at = new(location.X + 1, location.Y);
            foreach (
                MouseFlags flags in new[]
                {
                    MouseFlags.LeftButtonPressed,
                    MouseFlags.LeftButtonReleased,
                    MouseFlags.LeftButtonClicked,
                }
            )
            {
                this.app.Mouse.RaiseMouseEvent(new Mouse { Flags = flags, ScreenPosition = at });
            }

            this.app.LayoutAndDraw(true);
        }

        public void Dispose()
        {
            if (this.token is not null)
            {
                this.app.End(this.token);
            }

            this.Window.Dispose();
            this.app.Dispose();
        }
    }
}
