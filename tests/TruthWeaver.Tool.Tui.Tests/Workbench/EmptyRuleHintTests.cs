namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using Terminal.Gui.Input;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The diagnostics pane shows a short hint for an empty rule in place of the TRE0001 error.</summary>
[Collection(TerminalCollection.Name)]
public sealed class EmptyRuleHintTests
{
    /// <summary>An empty or whitespace-only rule shows the hint and no TRE0001.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("  \n\t ")]
    public void LinesOf_EmptyOrBlankRule_ShowsTheHintAndNoError_Test(string text)
    {
        RuleDocument document = RuleDocument.Create(text);

        IReadOnlyList<string> lines = DiagnosticsPane.LinesOf(document.Text, document.Diagnostics);

        Assert.Equal(DiagnosticsPane.EmptyRuleHint(), lines);
        Assert.DoesNotContain(lines, l => l.Contains("TRE0001", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, l => l.StartsWith("error", StringComparison.Ordinal));
    }

    /// <summary>A rule with text keeps its real diagnostics.</summary>
    [Fact]
    public void LinesOf_RuleWithAnError_ShowsTheDiagnostic_Test()
    {
        RuleDocument document = RuleDocument.Create("a AND");

        IReadOnlyList<string> lines = DiagnosticsPane.LinesOf(document.Text, document.Diagnostics);

        Assert.Matches(@"^error TRE\d{4} ", lines[0]);
        Assert.DoesNotContain(DiagnosticsPane.EmptyRuleHint()[0], lines);
    }

    /// <summary>The hint names the keys of the Samples command and of the menu bar, as the command list binds them.</summary>
    [Fact]
    public void EmptyRuleHint_Keys_MatchTheKeyBindings_Test()
    {
        Key samples = WorkbenchCommands.Default.Single(c => c.Title == "Samples...").Key;

        string hint = string.Join('\n', DiagnosticsPane.EmptyRuleHint());

        Assert.Contains(samples.ToString(), hint, StringComparison.Ordinal);
        Assert.Contains(WorkbenchMenus.MenuBarKey.ToString(), hint, StringComparison.Ordinal);
        Assert.NotEqual(Key.Empty, samples);
    }

    /// <summary>In the window, an empty editor shows the hint and typing a rule brings the diagnostics back.</summary>
    [Fact]
    public void Pane_EmptyThenTypedRule_ShowsTheHintThenTheRealDiagnostics_Test()
    {
        using WorkbenchHarness harness = new();
        List<string> empty = Lines(harness);

        harness.Type("a AND");
        List<string> typed = Lines(harness);

        Assert.Equal(DiagnosticsPane.EmptyRuleHint(), empty);
        Assert.StartsWith("error ", typed[0], StringComparison.Ordinal);
    }

    private static List<string> Lines(WorkbenchHarness harness)
    {
        return [.. Assert.IsType<ListView>(harness.Window.ActivePaneViews[PaneSlot.Bottom]).Source!.ToList().Cast<string>()];
    }
}
