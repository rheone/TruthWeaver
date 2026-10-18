namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// Smoke tests of the Terminal.Gui layer with no terminal: the window is built, laid out and driven with keys as the
/// terminal would send them.
/// </summary>
[Collection(TerminalCollection.Name)]
public sealed class WorkbenchWindowTests
{
    /// <summary>Typing a rule compiles it, and the canonical pane shows its tree.</summary>
    [Fact]
    public void Type_ValidRule_UpdatesTheDocumentAndTheCanonicalPane_Test()
    {
        using WorkbenchHarness harness = new();

        harness.Type("a and b");

        Assert.Equal("a and b", harness.Workbench.Document.Text);
        Assert.True(harness.Workbench.Document.IsRuleCurrent);
        Assert.Equal("a AND b", CanonicalPaneText(harness));
    }

    /// <summary>A syntax error keeps the last valid tree in the canonical pane, marks it as such, and lists the error.</summary>
    [Fact]
    public void Type_SyntaxError_KeepsTheLastTreeAndListsTheError_Test()
    {
        using WorkbenchHarness harness = new("a AND b");
        MoveToEnd(harness);

        harness.Type(" AND");

        Assert.StartsWith(CanonicalTextPane.StaleNotice, CanonicalPaneText(harness), StringComparison.Ordinal);
        Assert.Contains("a AND b", CanonicalPaneText(harness), StringComparison.Ordinal);
        Assert.StartsWith("error ", DiagnosticLines(harness)[0], StringComparison.Ordinal);
        Assert.Contains("showing the last valid tree", harness.Window.StatusText, StringComparison.Ordinal);
    }

    /// <summary>Ctrl+Z undoes through the document history, not the editor's own, and the editor shows the result.</summary>
    [Fact]
    public void CtrlZ_AfterTyping_UndoesThroughTheDocument_Test()
    {
        using WorkbenchHarness harness = new("a");
        MoveToEnd(harness);
        harness.Type("b");

        harness.Press(Key.Z.WithCtrl);

        Assert.Equal("a", harness.Workbench.Document.Text);
        Assert.Equal("a", EditorText(harness));
        Assert.True(harness.Workbench.Document.CanRedo);
    }

    /// <summary>Ctrl+Y redoes the step that Ctrl+Z undid.</summary>
    [Fact]
    public void CtrlY_AfterUndo_RedoesTheEdit_Test()
    {
        using WorkbenchHarness harness = new("a");
        MoveToEnd(harness);
        harness.Type("b");
        harness.Press(Key.Z.WithCtrl);

        harness.Press(Key.Y.WithCtrl);

        Assert.Equal("ab", harness.Workbench.Document.Text);
        Assert.Equal("ab", EditorText(harness));
    }

    /// <summary>F8 formats the editor text in the canonical form; Shift+F8 in the pretty form.</summary>
    [Fact]
    public void F8_MessyRule_FormatsTheEditorText_Test()
    {
        using WorkbenchHarness harness = new("a   and   (b or c)");

        harness.Press(Key.F8);

        Assert.Equal("a AND (b OR c)", EditorText(harness));
        Assert.Contains("Formatted as canonical", harness.Window.StatusText, StringComparison.Ordinal);
    }

    /// <summary>F1 shows the key help overlay, which lists every key; Esc closes it.</summary>
    [Fact]
    public void F1_ThenEsc_ShowsAndClosesTheKeyHelp_Test()
    {
        using WorkbenchHarness harness = new();

        harness.Press(Key.F1);
        bool shown = harness.Window.IsHelpVisible;
        harness.Press(Key.Esc);

        Assert.True(shown);
        Assert.False(harness.Window.IsHelpVisible);
    }

    /// <summary>F11 maximises the focused region and hides the others; a second F11 shows them again.</summary>
    [Fact]
    public void F11_EditorFocused_MaximisesAndRestoresTheEditor_Test()
    {
        using WorkbenchHarness harness = new("a");

        harness.Press(Key.F11);
        bool sideHidden = !harness.Window.IsRegionVisible(PaneSlot.Side);
        bool bottomHidden = !harness.Window.IsRegionVisible(PaneSlot.Bottom);
        harness.Press(Key.F11);

        Assert.True(sideHidden);
        Assert.True(bottomHidden);
        Assert.True(harness.Window.IsRegionVisible(PaneSlot.Side));
        Assert.True(harness.Window.IsRegionVisible(PaneSlot.Editor));
    }

    /// <summary>F6 moves the focus to the next region, and Shift+F6 back.</summary>
    [Fact]
    public void F6_FromTheEditor_FocusesTheSideRegion_Test()
    {
        using WorkbenchHarness harness = new("a");

        harness.Press(Key.F6);
        PaneSlot? next = harness.Window.FocusedSlot();
        harness.Press(Key.F6.WithShift);

        Assert.Equal(PaneSlot.Side, next);
        Assert.Equal(PaneSlot.Editor, harness.Window.FocusedSlot());
    }

    /// <summary>Alt+Right widens the editor region by one step, and the side region follows it.</summary>
    [Fact]
    public void AltRight_DefaultLayout_WidensTheEditorRegion_Test()
    {
        using WorkbenchHarness harness = new("a");
        int before = harness.Window.ActivePaneViews[PaneSlot.Editor].SuperView!.Frame.Width;

        harness.Press(Key.CursorRight.WithAlt);

        Assert.Equal(60, harness.Workbench.Layout.EditorWidthPercent);
        Assert.True(harness.Window.ActivePaneViews[PaneSlot.Editor].SuperView!.Frame.Width > before);
    }

    /// <summary>A registered pane joins its region, and Ctrl+PageDown switches the region between its panes.</summary>
    [Fact]
    public void CtrlPageDown_RegionWithTwoPanes_SwitchesToTheRegisteredPane_Test()
    {
        // The three shell panes only, so panes that later tickets register do not change which pane comes next.
        IWorkbenchPane[] panes = [new RuleTextPane(), new CanonicalTextPane(), new DiagnosticsPane(), new NotePane()];
        using WorkbenchHarness harness = new("a", panes);
        harness.Window.FocusRegion(PaneSlot.Side);

        harness.Press(Key.PageDown.WithCtrl);

        Assert.Equal(NotePane.PaneTitle, harness.Window.ActivePaneTitle(PaneSlot.Side));
        Assert.Equal(PaneSlot.Side, harness.Window.FocusedSlot());
    }

    /// <summary>A request for a span, such as Enter on a diagnostic, puts the editor cursor at the start of the span.</summary>
    [Fact]
    public void RequestSpan_SecondLine_MovesTheEditorCursor_Test()
    {
        using WorkbenchHarness harness = new("a AND\nb AND c");

        harness.Workbench.RequestSpan(new TruthWeaver.Diagnostics.SourceSpan(8, 3));

        Assert.Equal(new System.Drawing.Point(2, 1), EditorCursor(harness));
    }

    /// <summary>Quitting with unsaved changes asks first, and a no keeps the workbench open.</summary>
    [Fact]
    public void ConfirmClose_UnsavedChanges_AsksTheAuthor_Test()
    {
        using WorkbenchHarness harness = new("a");
        harness.Type("b");
        harness.Prompts.Discard = false;

        bool close = harness.Window.ConfirmClose();

        Assert.False(close);
        Assert.Equal(1, harness.Prompts.DiscardQuestions);
    }

    /// <summary>Opening a file through Ctrl+O replaces the document, and every pane follows the new document.</summary>
    [Fact]
    public void CtrlO_OtherFile_ShowsTheNewDocumentInEveryPane_Test()
    {
        string path = Path.Combine(Path.GetTempPath(), $"truthweaver-tui-{Guid.NewGuid():N}.rule");
        File.WriteAllText(path, "x OR y");
        try
        {
            using WorkbenchHarness harness = new("a");
            harness.Prompts.OpenPath = path;

            harness.Press(Key.O.WithCtrl);

            Assert.Equal("x OR y", EditorText(harness));
            Assert.Equal("x OR y", CanonicalPaneText(harness));
            Assert.Equal(DiagnosticsPane.NoDiagnostics, DiagnosticLines(harness)[0]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static System.Drawing.Point EditorCursor(WorkbenchHarness harness)
    {
        return Assert.IsType<RuleEditorView>(harness.Window.ActivePaneViews[PaneSlot.Editor]).InsertionPoint;
    }

    private static string EditorText(WorkbenchHarness harness)
    {
        return Assert.IsType<RuleEditorView>(harness.Window.ActivePaneViews[PaneSlot.Editor]).Text.ReplaceLineEndings("\n");
    }

    private static void MoveToEnd(WorkbenchHarness harness)
    {
        harness.Press(Key.End.WithCtrl);
    }

    private static string CanonicalPaneText(WorkbenchHarness harness)
    {
        harness.Window.FocusRegion(PaneSlot.Side);
        string text = harness.Window.ActivePaneViews[PaneSlot.Side].Text;
        harness.Window.FocusRegion(PaneSlot.Editor);
        return text;
    }

    private static List<string> DiagnosticLines(WorkbenchHarness harness)
    {
        return [.. Assert.IsType<ListView>(harness.Window.ActivePaneViews[PaneSlot.Bottom]).Source!.ToList().Cast<string>()];
    }

    /// <summary>A pane that a later change could register: a fixed note in the side region.</summary>
    private sealed class NotePane : IWorkbenchPane
    {
        public const string PaneTitle = "Note";

        public string Title => PaneTitle;

        public PaneSlot Slot => PaneSlot.Side;

        public View CreateView(Workbench workbench)
        {
            return new Label
            {
                Text = "A registered pane.",
                CanFocus = true,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };
        }
    }
}
