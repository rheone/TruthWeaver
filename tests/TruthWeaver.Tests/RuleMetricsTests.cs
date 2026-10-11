namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>A host reads the size and analysis cost of a compiled rule from <see cref="CompiledRule{TContext}.Metrics"/>.</summary>
public sealed class RuleMetricsTests
{
    private static readonly PredicateRegistry<RuleTestContext> Registry = PredicateRegistry<RuleTestContext>
        .CreateBuilder()
        .AddConstant("a", true)
        .AddConstant("b", true)
        .AddConstant("c", true)
        .AddConstant("d", true)
        .Build();

    /// <summary>A small rule reports its tree size, depth and distinct terms, and a BDD node count.</summary>
    [Fact]
    public void Metrics_ForASmallRule_ReportsTreeShapeAndBddSize_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND NOT b");

        RuleMetrics metrics = rule.Metrics;

        Assert.Equal(4, metrics.NodeCount);
        Assert.Equal(3, metrics.MaxDepth);
        Assert.Equal(2, metrics.DistinctTermCount);
        Assert.True(metrics.BddNodeCount > 0);
    }

    /// <summary>The metrics are computed once and the same instance is returned on every read.</summary>
    [Fact]
    public void Metrics_ReadTwice_ReturnsTheCachedInstance_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b");

        Assert.Same(rule.Metrics, rule.Metrics);
    }

    /// <summary>A term used twice counts once as a distinct term.</summary>
    [Fact]
    public void Metrics_WhenATermRepeats_CountsItOnce_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND (a OR b)");

        Assert.Equal(2, rule.Metrics.DistinctTermCount);
    }

    /// <summary>A rule with more distinct terms than <c>MaxAnalysisTerms</c> has no BDD node count; the other measures remain.</summary>
    [Fact]
    public void Metrics_AboveTheAnalysisTermCap_HasNullBddNodeCount_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b AND c", new CompilerOptions(MaxAnalysisTerms: 2));

        Assert.Null(rule.Metrics.BddNodeCount);
        Assert.Equal(3, rule.Metrics.DistinctTermCount);
        Assert.True(rule.Metrics.NodeCount >= 4);
    }

    /// <summary>A rule at exactly the cap is still analysed.</summary>
    [Fact]
    public void Metrics_AtTheAnalysisTermCap_HasABddNodeCount_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b", new CompilerOptions(MaxAnalysisTerms: 2));

        Assert.NotNull(rule.Metrics.BddNodeCount);
    }

    /// <summary>
    /// A sub-expression written twice adds tree nodes but no diagram nodes, because the shared manager stores each
    /// sub-graph once.
    /// </summary>
    [Fact]
    public void Metrics_WhenASubExpressionRepeats_CountsItsBddNodesOnce_Test()
    {
        RuleMetrics once = Compile("a AND b").Metrics;
        RuleMetrics twice = Compile("(a AND b) AND (a AND b)").Metrics;

        Assert.True(twice.NodeCount > once.NodeCount);
        Assert.Equal(once.BddNodeCount, twice.BddNodeCount);
    }

    /// <summary>An XOR chain is small as a tree but large as a K3 analysis diagram.</summary>
    [Fact]
    public void Metrics_ForAnXorChain_HasMoreBddNodesThanTreeNodes_Test()
    {
        RuleMetrics metrics = Compile("((a XOR b) XOR c) XOR d").Metrics;

        Assert.True(metrics.BddNodeCount > metrics.NodeCount);
    }

    /// <summary>NodeCount follows the tree-size definition: a sub-expression written twice counts twice.</summary>
    [Fact]
    public void Metrics_NodeCount_MatchesThePrintedTreeSize_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("(a AND b) OR (a AND b)");

        Assert.Equal(PrintedTreeSize.NodeCount(rule), rule.Metrics.NodeCount);
    }

    private static CompiledRule<RuleTestContext> Compile(string text, CompilerOptions? options = null)
    {
        return new RuleCompiler<RuleTestContext>(Registry, options).Compile(text).CompiledRule!;
    }
}
