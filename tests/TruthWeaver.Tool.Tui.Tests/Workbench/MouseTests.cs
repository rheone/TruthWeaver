namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using System.Drawing;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.Testing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// Tests of the mouse in the workbench window: a click focuses a pane and selects a tree node, the wheel scrolls, and a drag
/// of a divider resizes the regions.
/// </summary>
[Collection(TerminalCollection.Name)]
public sealed class MouseTests
{
    /// <summary>A click in the Tree pane gives the focus to its region.</summary>
    [Fact]
    public void Click_InAnotherRegion_FocusesThatRegion_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);
        TreePaneView tree = Assert.IsType<TreePaneView>(harness.Window.ActivePaneViews[PaneSlot.Side]);
        Assert.Equal(PaneSlot.Editor, harness.Window.FocusedSlot());

        harness.Click(tree.List, 1, 0);

        Assert.Equal(PaneSlot.Side, harness.Window.FocusedSlot());
    }

    /// <summary>A click on a line of the Tree pane selects that node, so the edit keys act on it.</summary>
    [Fact]
    public void Click_OnATreeNode_SelectsTheNode_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);
        TreePaneView tree = Assert.IsType<TreePaneView>(harness.Window.ActivePaneViews[PaneSlot.Side]);

        harness.Click(tree.List, 2, 2);

        Assert.StartsWith("$.operands[1]", tree.HeaderText, StringComparison.Ordinal);
        harness.Press(new Key('w'));
        Assert.Equal("a AND NOT b", harness.Workbench.Document.Text);
    }

    /// <summary>A click on a term of the Assign pane selects it, so a value key acts on that term, not the one it replaces.</summary>
    [Fact]
    public void Click_OnATermOfTheAssignPane_SelectsTheTerm_Test()
    {
        using WorkbenchHarness harness = new("isActive AND isAdmin", [new RuleTextPane(), new AssignmentPane()]);
        AssignmentView assign = Assert.IsType<AssignmentView>(harness.Window.ActivePaneViews[PaneSlot.Bottom]);

        harness.Click(assign.Terms, 0, 1);
        harness.Press(new Key('t'));

        Assert.Equal(["[U] isActive", "[T] isAdmin"], assign.TermLines);
    }

    /// <summary>
    /// A document edit that drops a diagnostic the author had selected moves the highlight back onto a row that exists,
    /// rather than leaving it on an index the new, shorter list no longer has.
    /// </summary>
    [Fact]
    public void Click_ThenFewerDiagnostics_KeepsTheSelectionInRange_Test()
    {
        using WorkbenchHarness harness = new("a AND\n(b OR\n)c", [new RuleTextPane(), new DiagnosticsPane()]);
        ListView list = Assert.IsType<ListView>(harness.Window.ActivePaneViews[PaneSlot.Bottom]);
        harness.Click(list, 0, 2);
        int before = Assert.IsType<int>(list.Value);

        harness.Workbench.Document.SetText("a AND");

        Assert.True(list.Value is int row && row >= 0 && row < list.Source!.Count, "selection must stay in range");
        Assert.True(before >= 0);
    }

    /// <summary>The wheel scrolls the list of the Tree pane without a key.</summary>
    [Fact]
    public void Wheel_OverALongTree_ScrollsTheList_Test()
    {
        ScriptedServices services = new();
        string rule = string.Join(" AND ", Enumerable.Range(0, 60).Select(i => $"t{i}"));
        using WorkbenchHarness harness = new(rule, [new RuleTextPane(), new TreePane(services.Services)]);
        TreePaneView tree = Assert.IsType<TreePaneView>(harness.Window.ActivePaneViews[PaneSlot.Side]);
        int before = tree.List.Viewport.Y;

        harness.Wheel(tree.List, down: true, steps: 3);

        Assert.True(tree.List.Viewport.Y > before);
    }

    /// <summary>The key help says that the mouse works and that every action keeps its key.</summary>
    [Fact]
    public void HelpText_DefaultCommands_SaysTheMouseWorks_Test()
    {
        string help = WorkbenchWindow.HelpText(WorkbenchCommands.Default);

        Assert.Contains("Mouse:", help, StringComparison.Ordinal);
        Assert.Contains("drag a divider to resize", help, StringComparison.Ordinal);
        Assert.Contains("Every action has a key", help, StringComparison.Ordinal);
    }

    /// <summary>The key help lists the keys that resize the editor width and the top height, and the keys that move the focus.</summary>
    [Fact]
    public void HelpText_DefaultCommands_ListsTheResizeAndFocusKeys_Test()
    {
        string help = WorkbenchWindow.HelpText(WorkbenchCommands.Default);

        Assert.Contains("Resize: editor narrower", help, StringComparison.Ordinal);
        Assert.Contains("Resize: editor wider", help, StringComparison.Ordinal);
        Assert.Contains("Resize: top regions lower", help, StringComparison.Ordinal);
        Assert.Contains("Resize: top regions higher", help, StringComparison.Ordinal);
        Assert.Contains("Next region", help, StringComparison.Ordinal);
        Assert.Contains("Previous region", help, StringComparison.Ordinal);
    }

    /// <summary>
    /// A drag of the vertical divider, sent through the Terminal.Gui mouse pipeline as a press, moves with the button held and
    /// a release, makes the editor region wider and writes the new width to the preferences file.
    /// </summary>
    [Fact]
    public void Drag_OnTheVerticalDivider_WidensTheEditorAndSavesTheLayout_Test()
    {
        using DragSession session = new();
        Rectangle before = session.EditorFrame();
        Point start = new(before.Right - 1, before.Y + 3);

        session.Drag(start, start with { X = start.X + 10 }, withMoves: true);

        Rectangle after = session.EditorFrame();
        Assert.True(after.Width >= before.Width + 9, $"{before.Width} -> {after.Width}");
        Assert.True(session.Workbench.Layout.EditorWidthPercent > new PaneLayout().EditorWidthPercent);
        Assert.Equal(
            session.Workbench.Layout.EditorWidthPercent,
            WorkbenchPreferences.Load(session.PreferencesPath).EditorWidthPercent
        );
    }

    /// <summary>
    /// A drag on the top border of the bottom region moves the horizontal divider up and saves the height, without a jump
    /// from the grab on the second border line.
    /// </summary>
    [Fact]
    public void Drag_OnTheHorizontalDivider_LowersTheTopRegionsAndSavesTheLayout_Test()
    {
        using DragSession session = new();
        Rectangle before = session.EditorFrame();
        Point start = new(before.Right + 5, before.Bottom);

        session.Drag(start, start with { Y = start.Y - 4 }, withMoves: true);

        Rectangle after = session.EditorFrame();
        Assert.InRange(after.Height, before.Height - 5, before.Height - 3);
        Assert.Equal(
            session.Workbench.Layout.TopHeightPercent,
            WorkbenchPreferences.Load(session.PreferencesPath).TopHeightPercent
        );
        Assert.True(session.Workbench.Layout.TopHeightPercent < new PaneLayout().TopHeightPercent);
    }

    /// <summary>A terminal that reports no moves while the button is held still resizes: the release position moves the divider.</summary>
    [Fact]
    public void Drag_WithOnlyPressAndRelease_MovesTheDividerToTheRelease_Test()
    {
        using DragSession session = new();
        Rectangle before = session.EditorFrame();
        Point start = new(before.Right, before.Y + 3);

        session.Drag(start, start with { X = start.X - 15 }, withMoves: false);

        Assert.True(session.EditorFrame().Width < before.Width - 10);
        Assert.True(session.Workbench.Layout.EditorWidthPercent < new PaneLayout().EditorWidthPercent);
    }

    /// <summary>A press inside a pane is no drag: the sizes stay.</summary>
    [Fact]
    public void Drag_InsideAPane_LeavesTheSizes_Test()
    {
        using DragSession session = new();
        Rectangle before = session.EditorFrame();
        Point start = new(before.X + 5, before.Y + 3);

        session.Drag(start, start with { X = start.X + 10 }, withMoves: true);

        Assert.Equal(before, session.EditorFrame());
        Assert.Equal(new PaneLayout().EditorWidthPercent, session.Workbench.Layout.EditorWidthPercent);
    }

    /// <summary>A running workbench on the headless ANSI driver, with a preferences file in a temporary folder.</summary>
    private sealed class DragSession : IDisposable
    {
        private readonly string directory;
        private readonly IApplication app;
        private readonly WorkbenchWindow window;
        private readonly SessionToken? token;

        public DragSession()
        {
            this.directory = Path.Combine(Path.GetTempPath(), "truthweaver-tui-tests", Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(this.directory);
            this.PreferencesPath = Path.Combine(this.directory, "workbench.json");
            this.app = Application.Create();
            this.app.Init(DriverRegistry.Names.ANSI);
            this.Workbench = new(RuleDocument.Create("a AND b"), ToolSettings.Default, new ScriptedPrompts())
            {
                Preferences = WorkbenchPreferences.Load(this.PreferencesPath),
            };
            this.window = new WorkbenchWindow(this.Workbench);
            this.token = this.app.Begin(this.window);

            // The first input that the application processes sets the screen size of the headless driver; a move of the
            // pointer over the status line settles it, so the frames that a test reads before a drag stay valid.
            this.app.InjectMouse(new Mouse { ScreenPosition = new Point(0, 0), Flags = MouseFlags.PositionReport });
            this.app.LayoutAndDraw(true);
        }

        public Workbench Workbench { get; }

        public string PreferencesPath { get; }

        public Rectangle EditorFrame()
        {
            return this.window.ActivePaneViews[PaneSlot.Editor].SuperView!.FrameToScreen();
        }

        // A press at the start, moves with the button held one cell at a time (when the terminal reports them), and a release.
        public void Drag(Point start, Point end, bool withMoves)
        {
            this.app.InjectMouse(new Mouse { ScreenPosition = start, Flags = MouseFlags.LeftButtonPressed });
            if (withMoves)
            {
                int steps = Math.Max(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));
                for (int i = 1; i <= steps; i++)
                {
                    Point at = new(start.X + ((end.X - start.X) * i / steps), start.Y + ((end.Y - start.Y) * i / steps));
                    this.app.InjectMouse(
                        new Mouse { ScreenPosition = at, Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport }
                    );
                    this.app.LayoutAndDraw(true);
                }
            }

            this.app.InjectMouse(new Mouse { ScreenPosition = end, Flags = MouseFlags.LeftButtonReleased });
            this.app.LayoutAndDraw(true);
        }

        public void Dispose()
        {
            if (this.token is not null)
            {
                this.app.End(this.token);
            }

            this.window.Dispose();
            this.app.Dispose();
            Directory.Delete(this.directory, recursive: true);
        }
    }
}
