namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Yaml;

/// <summary>The rewrites of the playground and the comparison with an earlier version of a rule.</summary>
public sealed class RewritePlaygroundTests
{
    /// <summary>Simplify shows the rule before and after and the list of its steps.</summary>
    [Fact]
    public void Preview_Simplify_ShowsBeforeAfterAndSteps_Test()
    {
        RewritePreview preview = RewritePlayground.Preview(Compile("a AND (a OR b)"), RuleRewriteKind.Simplify);

        Assert.True(preview.Changed);
        Assert.Equal("a AND (a OR b)", preview.BeforeText);
        Assert.Equal("a", preview.AfterText);
        Assert.NotEmpty(preview.Steps);
        Assert.Contains("AND", preview.BeforeTree, StringComparison.Ordinal);
        Assert.NotNull(preview.AfterTree);
    }

    /// <summary>The negation normal form pushes NOT to the terms.</summary>
    [Fact]
    public void Preview_NegationNormalForm_PushesNotToTheTerms_Test()
    {
        RewritePreview preview = RewritePlayground.Preview(Compile("NOT (a AND b)"), RuleRewriteKind.NegationNormalForm);

        Assert.Equal(Compile("NOT a OR NOT b").PrintJson(includeNotes: false), preview.After!.PrintJson(includeNotes: false));
    }

    /// <summary>Every rewrite gives a rule that is equivalent to the rule before it.</summary>
    /// <param name="kind">The rewrite.</param>
    [Theory]
    [InlineData(RuleRewriteKind.Simplify)]
    [InlineData(RuleRewriteKind.Canonicalize)]
    [InlineData(RuleRewriteKind.NegationNormalForm)]
    [InlineData(RuleRewriteKind.ConjunctiveNormalForm)]
    [InlineData(RuleRewriteKind.DisjunctiveNormalForm)]
    [InlineData(RuleRewriteKind.Compress)]
    [InlineData(RuleRewriteKind.ExpandToPrimitives)]
    [InlineData(RuleRewriteKind.OptimizeOrder)]
    public void Preview_EachRewrite_GivesAnEquivalentRule_Test(RuleRewriteKind kind)
    {
        CompiledRule<StubAssignment> rule = Compile("(a XOR b) OR NOT (c AND d)");

        RewritePreview preview = RewritePlayground.Preview(rule, kind);

        Assert.NotNull(preview.After);
        Assert.Equal(RuleEquivalenceOutcome.Equivalent, RewritePlayground.Compare(rule, preview.After).Equivalence.Outcome);
    }

    /// <summary>A normal form over the node cap of the settings gives no rule and the TRE0016 error.</summary>
    [Fact]
    public void Preview_ResultOverTheNodeCap_GivesNoRuleAndTheDiagnostic_Test()
    {
        ToolSettings settings = new() { MaxRewriteNodeCount = 4 };

        RewritePreview preview = RewritePlayground.Preview(
            Compile("(a AND b) OR (c AND d) OR (e AND f)"),
            RuleRewriteKind.ConjunctiveNormalForm,
            settings
        );

        Assert.Null(preview.After);
        Assert.False(preview.Changed);
        Assert.Contains(preview.Diagnostics, d => d.Code == DiagnosticCodes.RewriteTooLarge);
    }

    /// <summary>A rule satisfied by fewer assignments than the earlier one is not equivalent and is tighter.</summary>
    [Fact]
    public void Compare_NarrowerRule_IsNotEquivalentAndTighter_Test()
    {
        RuleComparison comparison = RewritePlayground.Compare(Compile("a OR b"), Compile("a"));

        Assert.Equal(RuleEquivalenceOutcome.NotEquivalent, comparison.Equivalence.Outcome);
        Assert.Equal(RuleRelation.Tighter, comparison.Relation.Relation);
    }

    /// <summary>The comparison stops at the analysis cap of the settings and says it is undecided.</summary>
    [Fact]
    public void Compare_MoreTermsThanTheCap_IsUndecided_Test()
    {
        ToolSettings settings = new() { MaxAnalysisTerms = 1 };

        RuleComparison comparison = RewritePlayground.Compare(Compile("a OR b"), Compile("a"), settings);

        Assert.Equal(RuleEquivalenceOutcome.Undecided, comparison.Equivalence.Outcome);
        Assert.Equal(RuleRelation.Undecided, comparison.Relation.Relation);
    }

    private static CompiledRule<StubAssignment> Compile(string text)
    {
        CompilationResult<StubAssignment> result = RuleConverter.CreateCompiler().CompileAs(text, RuleFormat.Text);
        Assert.NotNull(result.Rule);
        return result.Rule;
    }
}
