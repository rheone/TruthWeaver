namespace TruthWeaver.Tests;

using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Diffing;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Feature backlog 21c (ADR-0009 decision 5): the internal BDD node budget of the comparison tools. Each public entry
/// point reports <c>Undecided</c> with a reason when its shared BDD would exceed the budget, and no exception escapes.
/// </summary>
/// <remarks>
/// The pathological pair is <c>x1 OR ... OR x17 OR y1 OR ... OR y17</c>, which fixes the variable order to every
/// <c>x</c> before every <c>y</c>, and then <c>(x1 AND y1) OR ... OR (x17 AND y17)</c>, whose BDD under that order has
/// about 2^17 nodes per rail: past the default budget of 100,000, with only 34 terms.
/// </remarks>
public sealed class BddNodeBudgetTests
{
    private const int PairCount = 17;
    private const int TermCap = 2 * PairCount;

    /// <summary>A manager with a node budget throws once a build would create more decision nodes than the budget.</summary>
    [Fact]
    public void MakeNode_BuildPastTheBudget_ThrowsBudgetExceeded_Test()
    {
        BddManager bdd = new(2);
        int x = bdd.Variable(0);
        int y = bdd.Variable(1);

        BddNodeBudgetExceededException exception = Assert.Throws<BddNodeBudgetExceededException>(() => bdd.And(x, y));

        Assert.Equal(2, exception.Budget);
    }

    /// <summary><see cref="RuleEquivalence.Compare{TContext}"/> reports <c>Undecided</c> with a reason past the node budget.</summary>
    [Fact]
    public void RuleEquivalence_Compare_PairPastTheNodeBudget_ReportsUndecided_Test()
    {
        (CompiledRule<RuleTestContext> order, CompiledRule<RuleTestContext> hard) = PathologicalPair();

        RuleEquivalenceResult result = RuleEquivalence.Compare(order, hard, TermCap);

        Assert.Equal(RuleEquivalenceOutcome.Undecided, result.Outcome);
        Assert.Contains("budget", result.Reason, StringComparison.Ordinal);
    }

    /// <summary><see cref="RuleImplication.Implies{TContext}"/> reports <c>Undecided</c> with a reason past the node budget.</summary>
    [Fact]
    public void RuleImplication_Implies_PairPastTheNodeBudget_ReportsUndecided_Test()
    {
        (CompiledRule<RuleTestContext> order, CompiledRule<RuleTestContext> hard) = PathologicalPair();

        RuleImplicationResult result = RuleImplication.Implies(order, hard, TermCap);

        Assert.Equal(RuleImplicationOutcome.Undecided, result.Outcome);
        Assert.Contains("budget", result.Reason, StringComparison.Ordinal);
    }

    /// <summary><see cref="RuleImplication.Compare{TContext}"/> reports <see cref="RuleRelation.Undecided"/> with a reason past the node budget.</summary>
    [Fact]
    public void RuleImplication_Compare_PairPastTheNodeBudget_ReportsUndecided_Test()
    {
        (CompiledRule<RuleTestContext> order, CompiledRule<RuleTestContext> hard) = PathologicalPair();

        RuleRelationResult result = RuleImplication.Compare(order, hard, TermCap);

        Assert.Equal(RuleRelation.Undecided, result.Relation);
        Assert.Contains("budget", result.Reason, StringComparison.Ordinal);
    }

    /// <summary><see cref="RuleOverlap.Compare{TContext}"/> reports <c>Undecided</c> with a reason and an empty table past the node budget.</summary>
    [Fact]
    public void RuleOverlap_Compare_PairPastTheNodeBudget_ReportsUndecided_Test()
    {
        (CompiledRule<RuleTestContext> order, CompiledRule<RuleTestContext> hard) = PathologicalPair();

        RuleOverlapResult result = RuleOverlap.Compare(order, hard, TermCap);

        Assert.Equal(RuleOverlapOutcome.Undecided, result.Outcome);
        Assert.Empty(result.Reachable);
        Assert.Contains("budget", result.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <see cref="RuleSetAnalysis"/> reports the set as <c>Undecided</c> past the node budget, and the pair that
    /// caused it is <c>Undecided</c> on its own as well.
    /// </summary>
    [Fact]
    public void RuleSetAnalysis_Analyze_SetPastTheNodeBudget_ReportsUndecided_Test()
    {
        (CompiledRule<RuleTestContext> order, CompiledRule<RuleTestContext> hard) = PathologicalPair();

        RuleSetAnalysisResult result = RuleSetAnalysis.Analyze([order, hard], TermCap);

        Assert.Equal(RuleSetAnalysisOutcome.Undecided, result.Outcome);
        Assert.Contains("budget", result.Reason, StringComparison.Ordinal);
        Assert.Equal(RuleOverlapOutcome.Undecided, Assert.Single(result.Pairs).Overlap.Outcome);
    }

    /// <summary>
    /// <see cref="RuleDiff.Compare{TContext}"/> reads the budget as an undecided semantic answer: no meaning verdict and
    /// an <see cref="RuleRelation.Undecided"/> relation, with the structural entries still present.
    /// </summary>
    [Fact]
    public void RuleDiff_Compare_PairPastTheNodeBudget_ReportsNoSemanticVerdict_Test()
    {
        (CompiledRule<RuleTestContext> order, CompiledRule<RuleTestContext> hard) = PathologicalPair();

        RuleDiffResult result = RuleDiff.Compare(order, hard, TermCap);

        Assert.Null(result.PreservesMeaning);
        Assert.Equal(RuleRelation.Undecided, result.Relation);
        Assert.NotEmpty(result.Entries);
    }

    private static (CompiledRule<RuleTestContext> Order, CompiledRule<RuleTestContext> Hard) PathologicalPair()
    {
        string[] xs = [.. Enumerable.Range(1, PairCount).Select(i => $"x{i}")];
        string[] ys = [.. Enumerable.Range(1, PairCount).Select(i => $"y{i}")];
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        foreach (string name in xs.Concat(ys))
        {
            builder.AddConstant(name, true);
        }

        RuleCompiler<RuleTestContext> compiler = new(builder.Build(), new CompilerOptions(MaxDepth: 256, MaxNodeCount: 4096));
        string orderText = string.Join(" OR ", xs.Concat(ys));
        string hardText = string.Join(" OR ", xs.Zip(ys, (x, y) => $"({x} AND {y})"));
        return (
            compiler.Compile(orderText).CompiledRule ?? throw new InvalidOperationException("The order rule did not compile."),
            compiler.Compile(hardText).CompiledRule ?? throw new InvalidOperationException("The hard rule did not compile.")
        );
    }
}
