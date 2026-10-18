namespace TruthWeaver.Tool.Tui.Tests;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>Smoke tests of the Rewrite pane inside the workbench window.</summary>
[Collection(TerminalCollection.Name)]
public sealed class RewritePaneTests
{
    /// <summary>The rewrite key shows the Rewrite pane with the result, and the accept key there puts it into the editor.</summary>
    [Fact]
    public void RewriteKey_ThenAcceptInThePane_ReplacesTheRule_Test()
    {
        ScriptedServices services = new();
        services.Dialogs.Choices.Enqueue(2);
        using WorkbenchHarness harness = new(
            "NOT (a AND b)",
            [new RuleTextPane(), new CanonicalTextPane(), new RewritePane(services.Services)]
        );

        harness.Press(Key.R.WithCtrl);
        string? shown = harness.Window.ActivePaneTitle(PaneSlot.Side);
        harness.Press(new Key('a'));

        Assert.Equal("Rewrite", shown);
        Assert.Equal("NOT a OR NOT b", harness.Workbench.Document.Text);
    }

    /// <summary>The compare key works from the editor and shows the comparison in the Rewrite pane.</summary>
    [Fact]
    public void CompareKey_FromTheEditor_ShowsTheComparison_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new(
            "a OR b",
            [new RuleTextPane(), new CanonicalTextPane(), new RewritePane(services.Services)]
        );
        harness.Workbench.Document.SetText("a");

        harness.Press(Key.G.WithCtrl);

        Assert.Equal("Rewrite", harness.Window.ActivePaneTitle(PaneSlot.Side));
        Assert.NotNull(RewriteController.For(harness.Workbench).Comparison);
    }

    /// <summary>The second-rule key asks for a source, loads the clipboard and shows the Compare pane.</summary>
    [Fact]
    public void SecondRuleKey_FromTheEditor_ShowsTheComparePane_Test()
    {
        ScriptedServices services = new();
        services.Dialogs.Choices.Enqueue(0);
        services.Clipboard.Text = "a";
        using WorkbenchHarness harness = new(
            "a OR b",
            [new RuleTextPane(), new CanonicalTextPane(), new ComparePane(services.Services)]
        );

        harness.Press(Key.F9.WithCtrl);

        Assert.Equal("Compare", harness.Window.ActivePaneTitle(PaneSlot.Side));
        Assert.Contains(
            CompareController.For(harness.Workbench).Lines,
            l => l.Text.StartsWith("The second rule is tighter", StringComparison.Ordinal)
        );
    }
}
