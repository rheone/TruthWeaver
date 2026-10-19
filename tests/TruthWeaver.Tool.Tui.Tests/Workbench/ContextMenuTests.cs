namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using System.Drawing;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// Tests of the context menus: a right click in the editor, the tree, the term list and the diagnostics pane opens the menu
/// of that pane, and the shortcuts the menus show are the keys that work.
/// </summary>
[Collection(TerminalCollection.Name)]
public sealed class ContextMenuTests
{
    /// <summary>The views of the four panes map to their own menus.</summary>
    [Fact]
    public void For_ViewInEachPane_ReturnsTheMenuOfThatPane_Test()
    {
        using WorkbenchHarness harness = new("a AND b");
        View editor = harness.Window.ActivePaneViews[PaneSlot.Editor];
        TreePaneView tree = ShowPane<TreePaneView>(harness.Window, "Tree");
        AssignmentView assign = ShowPane<AssignmentView>(harness.Window, "Assign");
        View diagnostics = ShowPane<ListView>(harness.Window, "Diagnostics");

        Assert.Same(ContextMenus.EditorEntries, ContextMenus.For(editor)!.Value.Entries);
        Assert.Same(ContextMenus.TreeEntries, ContextMenus.For(tree.List)!.Value.Entries);
        Assert.Same(ContextMenus.TermEntries, ContextMenus.For(assign.Terms)!.Value.Entries);
        Assert.Same(ContextMenus.DiagnosticsEntries, ContextMenus.For(diagnostics)!.Value.Entries);
        Assert.Null(ContextMenus.For(null));
    }

    /// <summary>
    /// A shortcut of an entry that a command of the window runs is the key of that command; the menu cannot show a key that
    /// the window does not bind.
    /// </summary>
    [Fact]
    public void Entries_RunByTheWindow_ShowTheKeyOfTheirCommand_Test()
    {
        ContextMenuEntry[] windowEntries =
        [
            .. new[]
            {
                ContextMenus.EditorEntries,
                ContextMenus.TreeEntries,
                ContextMenus.TermEntries,
                ContextMenus.DiagnosticsEntries,
            }
                .SelectMany(entries => entries)
                .Where(e => e.IsWindowCommand),
        ];

        Assert.NotEmpty(windowEntries);
        Assert.All(
            windowEntries,
            e => Assert.Single(WorkbenchCommands.Default, c => c.Title == e.Title && c.Key == e.Key && c.Key != Key.Empty)
        );
    }

    /// <summary>The clipboard and select-all shortcuts of the editor menu are bound in the editor.</summary>
    [Fact]
    public void EditorEntries_PaneKeys_AreBoundInTheEditor_Test()
    {
        using WorkbenchHarness harness = new("a AND b");
        RuleEditorView editor = Assert.IsType<RuleEditorView>(harness.Window.ActivePaneViews[PaneSlot.Editor]);
        Dictionary<string, Command> expected = new()
        {
            ["Cut"] = Command.Cut,
            ["Copy"] = Command.Copy,
            ["Paste"] = Command.Paste,
            ["Select all"] = Command.SelectAll,
        };

        ContextMenuEntry[] local = [.. ContextMenus.EditorEntries.Where(e => !e.IsWindowCommand)];

        Assert.Equal(expected.Keys.Order(), local.Select(e => e.Title).Order());
        Assert.All(
            local,
            e =>
            {
                Assert.True(editor.KeyBindings.TryGet(e.Key, out KeyBinding binding), e.Title);
                Assert.Contains(expected[e.Title], binding.Commands);
            }
        );
    }

    /// <summary>Each shortcut of the tree menu is a key that the Tree pane handles.</summary>
    [Fact]
    public void TreeEntries_PaneKeys_AreHandledByTheTreePane_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);
        StructureController controller = StructureController.For(harness.Workbench, services.Services);

        Assert.All(ContextMenus.TreeEntries, e => Assert.True(TreePane.Handle(controller, e.Key), e.Title));
    }

    /// <summary>A shortcut of the term menu sets the value of the selected term, as the menu entry does.</summary>
    [Fact]
    public void TermEntries_PaneKeys_SetTheValueOfTheSelectedTerm_Test()
    {
        using WorkbenchHarness harness = new("a AND b");
        AssignmentView assign = ShowPane<AssignmentView>(harness.Window, "Assign");
        harness.Window.FocusRegion(PaneSlot.Bottom);

        ContextMenus.Run(harness.Window, assign.Terms, ContextMenus.TermEntries.Single(e => e.Title == "Set True"));
        harness.Press(Key.CursorDown);
        ContextMenus.Run(harness.Window, assign.Terms, ContextMenus.TermEntries.Single(e => e.Title == "Set False"));

        Assert.Equal(["[T] a", "[F] b"], assign.TermLines);
    }

    /// <summary>The Go to location entry of the diagnostics menu asks the editor to show the span of the diagnostic.</summary>
    [Fact]
    public void DiagnosticsEntries_GoToLocation_RequestsTheSpanOfTheDiagnostic_Test()
    {
        using WorkbenchHarness harness = new("a AND");
        ListView list = ShowPane<ListView>(harness.Window, "Diagnostics");
        harness.Window.FocusRegion(PaneSlot.Bottom);
        list.Value = 0;
        int requests = 0;
        harness.Workbench.SpanRequested += (_, _) => requests++;

        ContextMenus.Run(harness.Window, list, ContextMenus.DiagnosticsEntries.Single(e => e.Title == "Go to location"));

        Assert.Equal(1, requests);
    }

    /// <summary>The items of a menu show the title and the shortcut of their entries.</summary>
    [Fact]
    public void ItemsOf_Entries_ShowTheirShortcuts_Test()
    {
        using WorkbenchHarness harness = new("a AND b");
        View editor = harness.Window.ActivePaneViews[PaneSlot.Editor];

        MenuItem[] items = ContextMenus.ItemsOf(harness.Window, editor, ContextMenus.EditorEntries);

        Assert.Equal(ContextMenus.EditorEntries.Select(e => (e.Title, e.Key)), items.Select(i => (i.Title, i.Key)));
    }

    /// <summary>A right click in each of the four panes opens a context menu, and Esc closes it.</summary>
    [Fact]
    public void RightClick_InEachPane_OpensTheMenuOfThePane_Test()
    {
        using ContextSession session = new("a AND b");
        Func<View>[] targets =
        [
            () => session.Window.ActivePaneViews[PaneSlot.Editor],
            () => ShowPane<TreePaneView>(session.Window, "Tree").List,
            () => ShowPane<AssignmentView>(session.Window, "Assign").Terms,
            () => ShowPane<ListView>(session.Window, "Diagnostics"),
        ];

        foreach (Func<View> find in targets)
        {
            View target = find();
            session.Layout();
            session.RightClick(target.FrameToScreen().Location);
            Assert.True(session.Window.IsContextMenuOpen, target.GetType().Name);
            session.Press(Key.Esc);
            Assert.False(session.Window.IsContextMenuOpen, target.GetType().Name);
        }
    }

    /// <summary>A right click on a line of the Tree pane selects that node before the menu acts on it.</summary>
    [Fact]
    public void RightClick_OnATreeNode_SelectsTheNode_Test()
    {
        using ContextSession session = new("a AND b");
        TreePaneView tree = ShowPane<TreePaneView>(session.Window, "Tree");
        session.Layout();
        Point at = tree.List.FrameToScreen().Location;

        session.RightClick(new Point(at.X, at.Y + 2));

        Assert.StartsWith("$.operands[1]", tree.HeaderText, StringComparison.Ordinal);
        Assert.True(session.Window.IsContextMenuOpen);
    }

    /// <summary>A right click outside the four panes opens no menu.</summary>
    [Fact]
    public void RightClick_OnTheStatusLine_OpensNoMenu_Test()
    {
        using ContextSession session = new("a AND b");

        session.RightClick(new Point(5, 28));

        Assert.False(session.Window.IsContextMenuOpen);
    }

    private static T ShowPane<T>(WorkbenchWindow window, string title)
        where T : View
    {
        Assert.True(window.ShowPane(title));
        PaneSlot slot = new[] { PaneSlot.Editor, PaneSlot.Side, PaneSlot.Bottom }.Single(s =>
            window.ActivePaneTitle(s) == title
        );
        return Assert.IsType<T>(window.ActivePaneViews[slot]);
    }

    /// <summary>A headless run of the application with the workbench window, for the mouse and keys through Terminal.Gui.</summary>
    private sealed class ContextSession : IDisposable
    {
        private readonly IApplication app;
        private readonly SessionToken? token;

        public ContextSession(string text)
        {
            this.app = Application.Create();
            this.app.Init(DriverRegistry.Names.ANSI);
            this.app.Driver!.SetScreenSize(100, 30);
            this.Window = new WorkbenchWindow(
                new Workbench(RuleDocument.Create(text), ToolSettings.Default, new ScriptedPrompts())
            );
            this.token = this.app.Begin(this.Window);
            this.Window.FocusRegion(PaneSlot.Editor);
            this.app.LayoutAndDraw(true);
        }

        public WorkbenchWindow Window { get; }

        public void Layout()
        {
            this.app.LayoutAndDraw(true);
        }

        public void Press(Key key)
        {
            _ = this.app.Keyboard.RaiseKeyDownEvent(key);
            this.app.LayoutAndDraw(true);
        }

        // A right click one column into the view, as the terminal reports it: a press, a release and a click.
        public void RightClick(Point location)
        {
            Point at = new(location.X + 1, location.Y);
            foreach (
                MouseFlags flags in new[]
                {
                    MouseFlags.RightButtonPressed,
                    MouseFlags.RightButtonReleased,
                    MouseFlags.RightButtonClicked,
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
