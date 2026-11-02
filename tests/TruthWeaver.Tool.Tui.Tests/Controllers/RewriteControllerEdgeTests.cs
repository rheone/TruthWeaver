namespace TruthWeaver.Tool.Tui.Tests.Controllers;

using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Rewriting;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The no-op, no-preview and broken-rule cases of the rewrite controller.</summary>
public sealed class RewriteControllerEdgeTests
{
    /// <summary>A rewrite of a rule it cannot improve says it does not change the rule.</summary>
    [Fact]
    public void Run_RuleAlreadySimple_SaysTheRuleIsNotChanged_Test()
    {
        (RewriteController controller, Workbench workbench) = Create("a");

        bool ran = controller.Run(RuleRewriteKind.Simplify);

        Assert.True(ran);
        Assert.Contains("does not change the rule", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A rule with a syntax error is not rewritten: no preview is made.</summary>
    [Fact]
    public void Run_RuleThatDoesNotCompile_MakesNoPreview_Test()
    {
        (RewriteController controller, _) = Create("a AND");

        bool ran = controller.Run(RuleRewriteKind.Simplify);

        Assert.False(ran);
        Assert.Null(controller.Preview);
    }

    /// <summary>Accepting with no preview does nothing and says why.</summary>
    [Fact]
    public void Accept_NoPreview_SaysThereIsNothingToAccept_Test()
    {
        (RewriteController controller, Workbench workbench) = Create("a AND (a OR b)");

        bool accepted = controller.Accept();

        Assert.False(accepted);
        Assert.StartsWith("Nothing to accept", workbench.Status, StringComparison.Ordinal);
        Assert.Equal("a AND (a OR b)", workbench.Document.Text);
    }

    /// <summary>Discarding with no preview says there is nothing to discard.</summary>
    [Fact]
    public void Discard_NoPreview_SaysThereIsNothingToDiscard_Test()
    {
        (RewriteController controller, Workbench workbench) = Create("a");

        controller.Discard();

        Assert.Equal("Nothing to discard.", workbench.Status);
    }

    /// <summary>A rule that does not compile cannot be compared, and no comparison is made.</summary>
    [Fact]
    public void Compare_RuleThatDoesNotCompile_MakesNoComparison_Test()
    {
        (RewriteController controller, _) = Create("a AND");

        bool compared = controller.Compare();

        Assert.False(compared);
        Assert.Null(controller.Comparison);
    }

    /// <summary>Before any rewrite or comparison the pane shows the idle text.</summary>
    [Fact]
    public void Lines_NothingDoneYet_IsTheIdleText_Test()
    {
        (RewriteController controller, _) = Create("a");

        Assert.Equal(RewriteController.Idle, Assert.Single(controller.Lines));
    }

    /// <summary>The assignment text of a counterexample is sorted by name, and empty when there is none.</summary>
    [Fact]
    public void Assignment_Values_AreSortedByNameAndNullIsEmpty_Test()
    {
        Dictionary<string, TruthWeaver.Abstractions.TruthValue> values = new()
        {
            ["b"] = TruthWeaver.Abstractions.TruthValue.False,
            ["a"] = TruthWeaver.Abstractions.TruthValue.True,
        };

        Assert.Equal("a = True, b = False", RewriteController.Assignment(values));
        Assert.Equal(string.Empty, RewriteController.Assignment(null));
    }

    private static (RewriteController Controller, Workbench Workbench) Create(string text)
    {
        ScriptedServices services = new();
        Workbench workbench = new(RuleDocument.Create(text), ToolSettings.Default, new ScriptedPrompts());
        return (new RewriteController(workbench, services.Services), workbench);
    }
}
