namespace TruthWeaver.Tool.Tui.Tests.Panes;

using Terminal.Gui.Input;
using TruthWeaver.Abstractions;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Theme;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// Smoke tests of the Assign and Variables panes with no terminal: keys flip the terms, and the decision, the trace and the
/// policy follow.
/// </summary>
[Collection(TerminalCollection.Name)]
public sealed class AssignmentPaneTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "truthweaver-tui-tests", Guid.NewGuid().ToString("N"));

    public AssignmentPaneTests()
    {
        _ = Directory.CreateDirectory(this.directory);
    }

    /// <summary>Every term starts Unknown, so the decision is Unknown and is not satisfied.</summary>
    [Fact]
    public void Create_NewRule_ListsEveryTermAsUnknown_Test()
    {
        using WorkbenchHarness harness = Create("isActive AND isAdmin");
        AssignmentView view = AssignView(harness);

        Assert.Equal(["[U] isActive", "[U] isAdmin"], view.TermLines);
        Assert.Equal("Result: Unknown | IsSatisfied: False | Faults: 0", view.ResultText);
        Assert.Equal(["Unknown  AND", "  Unknown  isActive", "  Unknown  isAdmin"], view.TraceLines);
    }

    /// <summary>Space moves the selected term through True, False and Unknown, and the decision follows at once.</summary>
    [Fact]
    public void Space_OnATerm_CyclesItsValueAndUpdatesTheDecision_Test()
    {
        using WorkbenchHarness harness = Create("isActive AND isAdmin");
        AssignmentView view = AssignView(harness);
        harness.Window.FocusRegion(PaneSlot.Bottom);

        harness.Press(Key.Space);
        string first = view.TermLines[0];
        harness.Press(Key.CursorDown, Key.Space);
        string bothTrue = view.ResultText;
        harness.Press(Key.Space);

        Assert.Equal("[T] isActive", first);
        Assert.Equal("Result: True | IsSatisfied: True | Faults: 0", bothTrue);
        Assert.Equal(["[T] isActive", "[F] isAdmin"], view.TermLines);
        Assert.Equal("Result: False | IsSatisfied: False | Faults: 0", view.ResultText);
    }

    /// <summary>T, F and U set the selected term directly, so a rule becomes satisfied when every term is True.</summary>
    [Fact]
    public void TFAndU_OnATerm_SetTheValue_Test()
    {
        using WorkbenchHarness harness = Create("isActive AND isAdmin");
        AssignmentView view = AssignView(harness);
        harness.Window.FocusRegion(PaneSlot.Bottom);

        harness.Type("t");
        harness.Press(Key.CursorDown);
        harness.Type("t");
        string satisfied = view.ResultText;
        harness.Type("u");

        Assert.Equal("Result: True | IsSatisfied: True | Faults: 0", satisfied);
        Assert.Equal(["[T] isActive", "[U] isAdmin"], view.TermLines);
    }

    /// <summary>An Unknown operand shows how AND, OR and NOT respond, in the reason of the node.</summary>
    [Fact]
    public void Show_UnknownLeaf_ExplainsAndOrAndNot_Test()
    {
        using WorkbenchHarness harness = Create("a AND NOT b");
        AssignmentView view = AssignView(harness);

        string reason = view.ReasonText;
        TraceLine root = harness.Workbench.Outcome.Trace!;

        Assert.Contains("no operand is False", reason, StringComparison.Ordinal);
        Assert.Equal("NOT is Unknown: NOT of Unknown stays Unknown.", root.Children[1].Reason);
    }

    /// <summary>The trace colors the four states of a node (True, False, Unknown, skipped) in four distinct colors.</summary>
    [Fact]
    public void Of_EachValue_GivesADistinctColor_Test()
    {
        Terminal.Gui.Drawing.Color[] colors =
        [
            ThemePalette.Dark.Of(TruthValue.True),
            ThemePalette.Dark.Of(TruthValue.False),
            ThemePalette.Dark.Of(TruthValue.Unknown),
            ThemePalette.Dark.Of((TruthValue?)null),
        ];

        Assert.Equal(4, colors.Distinct().Count());
    }

    /// <summary>Ctrl+F3 moves the policy, and the Collapse and Project answers follow it.</summary>
    [Fact]
    public void CtrlF3_Policy_ChangesTheCollapseAndProjectAnswers_Test()
    {
        using WorkbenchHarness harness = Create("isActive");
        AssignmentView view = AssignView(harness);
        string failClosed = view.PolicyText;

        harness.Press(Key.F3.WithCtrl);
        string failOpen = view.PolicyText;
        harness.Press(Key.F3.WithCtrl);
        string error = view.PolicyText;

        Assert.Contains("fail-closed", failClosed, StringComparison.Ordinal);
        Assert.Contains("Collapse: False | Project: False", failClosed, StringComparison.Ordinal);
        Assert.Contains("fail-open", failOpen, StringComparison.Ordinal);
        Assert.Contains("Collapse: True | Project: True", failOpen, StringComparison.Ordinal);
        Assert.Contains("Collapse: RejectedUnresolved | Project: n/a", error, StringComparison.Ordinal);
    }

    /// <summary>Ctrl+F2 gives every term the value Unknown again.</summary>
    [Fact]
    public void CtrlF2_AfterAssigning_ClearsEveryValue_Test()
    {
        using WorkbenchHarness harness = Create("isActive");
        AssignmentView view = AssignView(harness);
        harness.Window.FocusRegion(PaneSlot.Bottom);
        harness.Type("t");

        harness.Press(Key.F2.WithCtrl);

        Assert.Equal(["[U] isActive"], view.TermLines);
    }

    /// <summary>Editing the rule keeps the values of the terms that stay, and the list follows the new rule.</summary>
    [Fact]
    public void DocumentChanged_RuleEdited_KeepsTheValuesAndUpdatesTheTerms_Test()
    {
        using WorkbenchHarness harness = Create("isActive");
        AssignmentView view = AssignView(harness);
        harness.Window.FocusRegion(PaneSlot.Bottom);
        harness.Type("t");

        harness.Workbench.Document.SetText("isActive OR isAdmin");

        Assert.Equal(["[T] isActive", "[U] isAdmin"], view.TermLines);
        Assert.Equal("Result: True | IsSatisfied: True | Faults: 0", view.ResultText);
    }

    /// <summary>A missing variable is Unknown with a fault, and the term waits for data.</summary>
    [Fact]
    public void Show_MissingVariable_ShowsTheFaultAndAWaitingTerm_Test()
    {
        using WorkbenchHarness harness = Create("inRegion(region: from(\"user\", \"$.region\"))");
        AssignmentView view = AssignView(harness);

        Assert.Equal("Result: Unknown | IsSatisfied: False | Faults: 1", view.ResultText);
        Assert.EndsWith("(no data yet)", view.TermLines[0], StringComparison.Ordinal);
    }

    /// <summary>Typing the variables resolves the reference, so the term can take a value and the fault goes.</summary>
    [Fact]
    public void Variables_Typed_ResolveTheReference_Test()
    {
        using WorkbenchHarness harness = Create("inRegion(region: from(\"user\", \"$.region\"))");
        AssignmentView view = AssignView(harness);
        VariablesView variables = VariablesViewOf(harness);

        variables.Editor.Text = "{\"user\": {\"region\": \"EU\"}}";
        harness.Window.FocusRegion(PaneSlot.Bottom);
        harness.Type("t");

        Assert.Equal("{\"user\": {\"region\": \"EU\"}}", harness.Workbench.Assignment.VariablesText);
        Assert.Equal(["[T] inRegion(region: from(\"user\", \"$.region\"))"], view.TermLines);
        Assert.Equal("Result: True | IsSatisfied: True | Faults: 0", view.ResultText);
    }

    /// <summary>A variables text with an error keeps the last good data and tells the author under the editor.</summary>
    [Fact]
    public void Variables_Malformed_ShowsTheProblemUnderTheEditor_Test()
    {
        using WorkbenchHarness harness = Create("isActive");
        VariablesView variables = VariablesViewOf(harness);

        variables.Editor.Text = "{\"user\": ";

        Assert.Contains("not well-formed", variables.NoteText, StringComparison.Ordinal);
        Assert.Contains("last good data", variables.NoteText, StringComparison.Ordinal);
    }

    /// <summary>Ctrl+F4 loads a YAML variables file into the editor and the assignment.</summary>
    [Fact]
    public void CtrlF4_YamlFile_LoadsTheVariables_Test()
    {
        string path = Path.Combine(this.directory, "data.yaml");
        File.WriteAllText(path, "user:\n  region: EU\n");
        using WorkbenchHarness harness = Create("inRegion(region: from(\"user\", \"$.region\"))");
        harness.Prompts.OpenPath = path;

        harness.Press(Key.F4.WithCtrl);

        Assert.Equal("user:\n  region: EU\n", harness.Workbench.Assignment.VariablesText);
        Assert.Equal("user:\n  region: EU\n", VariablesViewOf(harness).Editor.Text.ReplaceLineEndings("\n"));
        Assert.Empty(harness.Workbench.Outcome.Faults);
        Assert.Contains("Variables loaded", harness.Workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A rule that never compiled shows that there is no rule, and has no terms.</summary>
    [Fact]
    public void Show_NoRule_SaysSo_Test()
    {
        using WorkbenchHarness harness = Create("isActive AND (");
        AssignmentView view = AssignView(harness);

        Assert.Equal(AssignmentText.NoRule, view.ResultText);
        Assert.Empty(view.TermLines);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(this.directory, recursive: true);
    }

    private static WorkbenchHarness Create(string text)
    {
        return new WorkbenchHarness(
            text,
            [new RuleTextPane(), new AssignmentPane(), new VariablesPane(), new CanonicalTextPane()]
        );
    }

    private static AssignmentView AssignView(WorkbenchHarness harness)
    {
        return Assert.IsType<AssignmentView>(harness.Window.ActivePaneViews[PaneSlot.Bottom]);
    }

    // The Variables pane is the second pane of the bottom region: the window shows one at a time, so switch to it and back.
    private static VariablesView VariablesViewOf(WorkbenchHarness harness)
    {
        harness.Window.FocusRegion(PaneSlot.Bottom);
        harness.Window.SwitchPane(1);
        VariablesView view = Assert.IsType<VariablesView>(harness.Window.ActivePaneViews[PaneSlot.Bottom]);
        harness.Window.SwitchPane(-1);
        return view;
    }
}
