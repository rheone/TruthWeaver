namespace TruthWeaver.Tool.Tui.Tests;

using Terminal.Gui.Input;
using TruthWeaver.Abstractions;
using TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>
/// Smoke tests of the Tests pane with no terminal: the keys save a case and run the file, and Enter on a case loads it into
/// the Assign pane.
/// </summary>
[Collection(TerminalCollection.Name)]
public sealed class RuleTestPaneTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "truthweaver-tui-tests", Guid.NewGuid().ToString("N"));

    public RuleTestPaneTests()
    {
        _ = Directory.CreateDirectory(this.directory);
    }

    /// <summary>Ctrl+F5 saves the current assignment as a case, and the list shows it.</summary>
    [Fact]
    public void CtrlF5_WithAPathAndAName_SavesTheCaseAndListsIt_Test()
    {
        using WorkbenchHarness harness = Create("isActive AND isAdmin");
        RuleTestView view = TestView(harness);
        string path = Path.Combine(this.directory, "access.test.yaml");
        view.FileField.Text = path;
        view.CaseNameField.Text = "nobody is in";
        harness.Workbench.Assignment.Set("isActive", TruthValue.True);
        harness.Workbench.NotifyAssignmentChanged();

        harness.Press(Key.F5.WithCtrl);

        Assert.True(File.Exists(path));
        Assert.Equal(["----  nobody is in  (expects Unknown, 0 fault(s))"], view.CaseLines);
        Assert.Contains("Saved the case", harness.Window.StatusText, StringComparison.Ordinal);
    }

    /// <summary>Ctrl+F7 runs the cases on the open rule and lists each as pass or fail, with the reason under the list.</summary>
    [Fact]
    public void CtrlF7_RuleChanged_ListsTheFailureWithItsReason_Test()
    {
        using WorkbenchHarness harness = Create("isActive");
        RuleTestView view = TestView(harness);
        view.FileField.Text = Path.Combine(this.directory, "access.test.yaml");
        view.CaseNameField.Text = "active";
        harness.Workbench.Assignment.Set("isActive", TruthValue.True);
        harness.Press(Key.F5.WithCtrl);
        harness.Press(Key.F7.WithCtrl);
        string[] passing = [.. view.CaseLines];

        harness.Workbench.Document.SetText("NOT isActive");
        harness.Press(Key.F7.WithCtrl);

        Assert.Equal(["PASS  active  (True, 0 fault(s))"], passing);
        Assert.Equal(["FAIL  active  expected True, 0 fault(s); got False, 0 fault(s)"], view.CaseLines);
        Assert.StartsWith("Expected the result True, but the result is False.", view.DetailText, StringComparison.Ordinal);
    }

    /// <summary>Enter on a failing case loads its assignment into the Assign pane, so the decision shows it.</summary>
    [Fact]
    public void Enter_OnAFailingCase_LoadsItIntoTheAssignPane_Test()
    {
        using WorkbenchHarness harness = Create("isActive AND isAdmin");
        RuleTestView view = TestView(harness);
        view.FileField.Text = Path.Combine(this.directory, "access.test.yaml");
        view.CaseNameField.Text = "both";
        harness.Workbench.Assignment.Set("isActive", TruthValue.True);
        harness.Workbench.Assignment.Set("isAdmin", TruthValue.True);
        harness.Press(Key.F5.WithCtrl);
        harness.Workbench.Document.SetText("isActive AND NOT isAdmin");
        harness.Workbench.Assignment.ClearValues();
        harness.Press(Key.F7.WithCtrl);
        harness.Window.FocusRegion(PaneSlot.Bottom);
        view.Cases.SetFocus();

        harness.Press(Key.Enter);

        Assert.Equal(TruthValue.True, harness.Workbench.Assignment.ValueOf("isActive"));
        Assert.Equal(TruthValue.True, harness.Workbench.Assignment.ValueOf("isAdmin"));
        Assert.Equal(
            "Result: False | IsSatisfied: False | Faults: 0",
            harness.Workbench.Outcome is { } ? AssignmentText.ResultLine(harness.Workbench.Outcome) : string.Empty
        );
        Assert.Equal("both", harness.Workbench.RuleTests.CaseName);
    }

    /// <summary>Delete on a case removes it from the file.</summary>
    [Fact]
    public void Delete_OnACase_RemovesItFromTheFile_Test()
    {
        using WorkbenchHarness harness = Create("isActive");
        RuleTestView view = TestView(harness);
        view.FileField.Text = Path.Combine(this.directory, "access.test.yaml");
        view.CaseNameField.Text = "a";
        harness.Press(Key.F5.WithCtrl);
        view.CaseNameField.Text = "b";
        harness.Press(Key.F5.WithCtrl);
        view.Cases.SetFocus();

        harness.Press(Key.Delete);

        Assert.Equal(["----  b  (expects Unknown, 0 fault(s))"], view.CaseLines);
    }

    /// <summary>Enter in the file field reads the cases of an existing file.</summary>
    [Fact]
    public void Enter_InTheFileField_ReadsTheCasesOfTheFile_Test()
    {
        string path = Path.Combine(this.directory, "access.test.yaml");
        File.WriteAllText(path, "rule: |\n  isActive\ncases:\n  - name: a\n    expect:\n      result: Unknown\n");
        using WorkbenchHarness harness = Create("isActive");
        RuleTestView view = TestView(harness);
        view.FileField.Text = path;
        view.FileField.SetFocus();

        harness.Press(Key.Enter);

        Assert.Equal(["----  a  (expects Unknown)"], view.CaseLines);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(this.directory, recursive: true);
    }

    private static WorkbenchHarness Create(string text)
    {
        return new WorkbenchHarness(text, [new RuleTextPane(), new AssignmentPane(), new RuleTestPane()]);
    }

    // The Tests pane is the second pane of the bottom region.
    private static RuleTestView TestView(WorkbenchHarness harness)
    {
        harness.Window.FocusRegion(PaneSlot.Bottom);
        harness.Window.SwitchPane(1);
        return Assert.IsType<RuleTestView>(harness.Window.ActivePaneViews[PaneSlot.Bottom]);
    }
}
