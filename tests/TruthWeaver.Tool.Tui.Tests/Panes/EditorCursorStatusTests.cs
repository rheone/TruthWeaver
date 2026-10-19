namespace TruthWeaver.Tool.Tui.Tests.Panes;

using TruthWeaver.Abstractions;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The status line shows the value of the term under the editor cursor, as it does for the selected tree node.</summary>
[Collection(TerminalCollection.Name)]
public sealed class EditorCursorStatusTests
{
    private const string FromTerm = "inRegion(region: from(\"user\", \"$.region\"))";

    /// <summary>The cursor on a term with a value in the assignment shows the value as a word.</summary>
    [Fact]
    public void StatusOf_CursorOnATerm_ShowsItsValueAsAWord_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);
        harness.Workbench.Assignment.Set("b", TruthValue.False);
        harness.Workbench.NotifyAssignmentChanged();

        Editor(harness).MoveTo(6);

        Assert.Contains("b = False", WorkbenchWindow.StatusOf(harness.Workbench), StringComparison.Ordinal);
    }

    /// <summary>A term that the assignment has no value for shows the word Unknown.</summary>
    [Fact]
    public void StatusOf_CursorOnATermWithNoAssignment_ShowsUnknown_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);

        Editor(harness).MoveTo(0);

        Assert.Contains("a = Unknown", WorkbenchWindow.StatusOf(harness.Workbench), StringComparison.Ordinal);
    }

    /// <summary>The cursor on the variable of a term (a from source) shows the value of the whole term, or the reason it has none.</summary>
    [Fact]
    public void StatusOf_CursorOnTheVariableOfATerm_ShowsTheValueOfTheTerm_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new(FromTerm, [new RuleTextPane(), new TreePane(services.Services)]);

        Editor(harness).MoveTo(FromTerm.IndexOf("from", StringComparison.Ordinal) + 2);

        Assert.Contains($"{FromTerm} = Unknown: ", WorkbenchWindow.StatusOf(harness.Workbench), StringComparison.Ordinal);
    }

    /// <summary>The cursor on an operator or on a constant adds no value.</summary>
    [Fact]
    public void StatusOf_CursorOnAnOperator_ShowsNoValue_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);

        Editor(harness).MoveTo(3);

        Assert.DoesNotContain(" = ", WorkbenchWindow.StatusOf(harness.Workbench), StringComparison.Ordinal);
    }

    /// <summary>While the text has errors, the status line shows no value for the term.</summary>
    [Fact]
    public void StatusOf_TextHasErrors_ShowsNoValue_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND (b", [new RuleTextPane(), new TreePane(services.Services)]);

        Editor(harness).MoveTo(0);

        Assert.DoesNotContain(" = ", WorkbenchWindow.StatusOf(harness.Workbench), StringComparison.Ordinal);
    }

    /// <summary>While a keystroke waits for its check, the status line shows no value, and it comes back when the check ends.</summary>
    [Fact]
    public void StatusOf_RuleIsBeingChecked_ShowsNoValueUntilTheCheckEnds_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);
        Editor(harness).MoveTo(0);
        harness.Workbench.DiagnosticsDelay = TimeSpan.FromHours(1);

        harness.Workbench.EditText("a AND b OR c");
        string checking = WorkbenchWindow.StatusOf(harness.Workbench);
        harness.Workbench.FlushPendingText();
        Editor(harness).MoveTo(1);

        Assert.Contains("checking...", checking, StringComparison.Ordinal);
        Assert.DoesNotContain(" = ", checking, StringComparison.Ordinal);
        Assert.Contains("a = Unknown", WorkbenchWindow.StatusOf(harness.Workbench), StringComparison.Ordinal);
    }

    /// <summary>The selected node of the tree takes over from the cursor when it is the one that moved last.</summary>
    [Fact]
    public void StatusOf_TreeSelectionMovesAfterTheCursor_ShowsTheSelectedNode_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);
        Editor(harness).MoveTo(0);

        harness.Workbench.SelectNode("$.operands[1]");

        string status = WorkbenchWindow.StatusOf(harness.Workbench);
        Assert.Contains("b = Unknown", status, StringComparison.Ordinal);
        Assert.DoesNotContain("a = Unknown", status, StringComparison.Ordinal);
    }

    private static RuleEditorView Editor(WorkbenchHarness harness)
    {
        return Assert.IsType<RuleEditorView>(harness.Window.ActivePaneViews[PaneSlot.Editor]);
    }
}
