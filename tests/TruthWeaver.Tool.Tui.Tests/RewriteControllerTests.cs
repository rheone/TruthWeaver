namespace TruthWeaver.Tool.Tui.Tests;

using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core;
using TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>The rewrites and the comparison of the workbench with no terminal.</summary>
public sealed class RewriteControllerTests
{
    /// <summary>A rewrite shows before and after with the size and the steps, and leaves the document as it is.</summary>
    [Fact]
    public void Run_Simplify_ShowsBeforeAndAfterAndKeepsTheDocument_Test()
    {
        (RewriteController controller, _, Workbench workbench) = Create("a AND (a OR b)");

        bool ran = controller.Run(RuleRewriteKind.Simplify);

        Assert.True(ran);
        Assert.Equal("a AND (a OR b)", workbench.Document.Text);
        Assert.Equal("a", controller.Preview!.AfterText);
        Assert.Contains("Diagnostics: none", controller.Lines);
        Assert.Contains(controller.Lines, l => l.StartsWith("Size: ", StringComparison.Ordinal));
        Assert.Contains("Steps:", controller.Lines);
        Assert.Contains("Tree after:", controller.Lines);
    }

    /// <summary>A rewrite that gives no rule shows its diagnostic in the pane and on the status line.</summary>
    [Fact]
    public void Run_ResultOverTheNodeCap_ShowsTheDiagnostic_Test()
    {
        (RewriteController controller, _, Workbench workbench) = Create(
            "(a AND b) OR (c AND d) OR (e AND f)",
            new ToolSettings { MaxRewriteNodeCount = 4 }
        );

        controller.Run(RuleRewriteKind.ConjunctiveNormalForm);

        Assert.Contains(controller.Lines, l => l.Contains(DiagnosticCodes.RewriteTooLarge, StringComparison.Ordinal));
        Assert.Contains("After: no rule", controller.Lines);
        Assert.StartsWith("ToCnf gave no rule: TRE0016", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>Accept replaces the rule as one undo step, and undo gives back the rule before the rewrite.</summary>
    [Fact]
    public void Accept_Preview_ReplacesTheRuleAsOneUndoStep_Test()
    {
        (RewriteController controller, _, Workbench workbench) = Create("NOT (a AND b)");
        controller.Run(RuleRewriteKind.NegationNormalForm);

        bool accepted = controller.Accept();
        string after = workbench.Document.Text;
        workbench.Undo();

        Assert.True(accepted);
        Assert.Equal("NOT a OR NOT b", after);
        Assert.Null(controller.Preview);
        Assert.Equal("NOT (a AND b)", workbench.Document.Text);
    }

    /// <summary>Accept keeps the note of a node the rewrite leaves alone, says when it drops one, and one undo restores the text.</summary>
    [Fact]
    public void Accept_RuleWithNotes_KeepsWhatItCanAndSaysWhatItDrops_Test()
    {
        (RewriteController controller, _, Workbench workbench) = Create(
            """{"op":"and","operands":[{"predicate":"a","note":"keep"},{"op":"not","operands":[{"op":"and","operands":[{"predicate":"b"},{"predicate":"c"}]}],"note":"drop"}]}""",
            format: RuleFormat.Json
        );
        string earlier = workbench.Document.Text;
        controller.Run(RuleRewriteKind.NegationNormalForm);

        bool accepted = controller.Accept();
        string after = workbench.Document.Text;
        string status = workbench.Status;
        workbench.Undo();

        Assert.True(accepted);
        Assert.Contains("keep", after, StringComparison.Ordinal);
        Assert.DoesNotContain("drop", after, StringComparison.Ordinal);
        Assert.Contains("1 note was dropped", status, StringComparison.Ordinal);
        Assert.Equal(earlier, workbench.Document.Text);
    }

    /// <summary>A preview of a rule that changed since the rewrite ran is not accepted.</summary>
    [Fact]
    public void Accept_RuleChangedAfterTheRewrite_IsRefused_Test()
    {
        (RewriteController controller, _, Workbench workbench) = Create("NOT (a AND b)");
        controller.Run(RuleRewriteKind.NegationNormalForm);
        workbench.Document.SetText("NOT (a AND c)");

        bool accepted = controller.Accept();

        Assert.False(accepted);
        Assert.Equal("NOT (a AND c)", workbench.Document.Text);
        Assert.StartsWith("Not accepted: the rule changed", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>Discard drops the preview and leaves the document as it is.</summary>
    [Fact]
    public void Discard_Preview_DropsItAndKeepsTheDocument_Test()
    {
        (RewriteController controller, _, Workbench workbench) = Create("NOT (a AND b)");
        controller.Run(RuleRewriteKind.NegationNormalForm);

        controller.Discard();

        Assert.Null(controller.Preview);
        Assert.Equal("NOT (a AND b)", workbench.Document.Text);
        Assert.False(workbench.Document.CanUndo);
    }

    /// <summary>The comparison with the opened version says that a narrower rule is not equivalent and is tighter.</summary>
    [Fact]
    public void Compare_EditedRule_ReportsEquivalenceAndRelation_Test()
    {
        (RewriteController controller, _, Workbench workbench) = Create("a OR b");
        workbench.Document.SetText("a");

        bool compared = controller.Compare();

        Assert.True(compared);
        Assert.Equal(RuleEquivalenceOutcome.NotEquivalent, controller.Comparison!.Equivalence.Outcome);
        Assert.Equal(RuleRelation.Tighter, controller.Comparison.Relation.Relation);
        Assert.Contains(controller.Lines, l => l.StartsWith("Equivalent: no.", StringComparison.Ordinal));
    }

    /// <summary>After an accepted rewrite, the comparison is with the accepted rule.</summary>
    [Fact]
    public void Compare_AfterAnAcceptedRewrite_ComparesWithTheAcceptedRule_Test()
    {
        (RewriteController controller, _, Workbench workbench) = Create("NOT (a AND b)");
        controller.Run(RuleRewriteKind.NegationNormalForm);
        controller.Accept();

        controller.Compare();

        Assert.Equal(RuleEquivalenceOutcome.Equivalent, controller.Comparison!.Equivalence.Outcome);
        Assert.Equal(workbench.Document.Rule, controller.Baseline);
    }

    /// <summary>The comparison respects the analysis cap of the settings and says that it is undecided.</summary>
    [Fact]
    public void Compare_MoreTermsThanMaxAnalysisTerms_IsUndecided_Test()
    {
        (RewriteController controller, _, Workbench workbench) = Create("a OR b", new ToolSettings { MaxAnalysisTerms = 1 });
        workbench.Document.SetText("a OR c");

        controller.Compare();

        Assert.Equal(RuleEquivalenceOutcome.Undecided, controller.Comparison!.Equivalence.Outcome);
        Assert.Contains(controller.Lines, l => l.StartsWith("Equivalent: undecided.", StringComparison.Ordinal));
    }

    /// <summary>With no saved or accepted version, the comparison says there is nothing to compare with.</summary>
    [Fact]
    public void Compare_NoSavedVersion_SaysSo_Test()
    {
        (RewriteController controller, _, Workbench workbench) = Create("a AND (");
        workbench.Document.SetText("a");

        bool compared = controller.Compare();

        Assert.False(compared);
        Assert.StartsWith("Nothing to compare with", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>The rewrite menu lists every rewrite by its library name and runs the one chosen.</summary>
    [Fact]
    public void Choose_SecondEntry_RunsCanonicalize_Test()
    {
        (RewriteController controller, ScriptedServices services, _) = Create("b AND a");
        services.Dialogs.Choices.Enqueue(1);

        controller.Choose();

        Assert.Equal(
            ["Simplify", "Canonicalize", "ToNnf", "ToCnf", "ToDnf", "CompressToDerived", "ExpandToPrimitives", "OptimizeOrder"],
            services.Dialogs.LastItems
        );
        Assert.Equal(RuleRewriteKind.Canonicalize, controller.Preview!.Kind);
    }

    private static (RewriteController Controller, ScriptedServices Services, Workbench Workbench) Create(
        string text,
        ToolSettings? settings = null,
        RuleFormat format = RuleFormat.Text
    )
    {
        ScriptedServices services = new();
        ToolSettings resolved = settings ?? ToolSettings.Default;
        Workbench workbench = new(RuleDocument.Create(text, format, resolved), resolved, new ScriptedPrompts());
        return (new RewriteController(workbench, services.Services), services, workbench);
    }
}
