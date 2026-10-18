namespace TruthWeaver.Tool.Core.Tests.Reports;

using System.Numerics;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Core.Reports;

/// <summary>The statistics of a rule match the counts done by hand.</summary>
public sealed class RuleStatisticsTests
{
    // OR( AtLeast(2, isActive, hasRole, NOT isActive), AND(isLocked, inRegion(from user)) )
    // Nodes: OR, AtLeast, isActive, hasRole, NOT, isActive, AND, isLocked, inRegion = 9.
    // Depth: OR > AtLeast > NOT > isActive = 4.
    // Term nodes: isActive twice, hasRole, isLocked, inRegion = 5; distinct terms = 4.
    private const string Rule =
        "AtLeast(2, isActive, hasRole(role: \"editor\"), NOT isActive) OR (isLocked AND inRegion(region: from(\"user\", \"$.region\")))";

    /// <summary>The size, the terms, the predicates and the operators of a rule are the hand counts.</summary>
    [Fact]
    public void Compute_HandCountedRule_GivesTheSizeTermsAndOperators_Test()
    {
        RuleStatistics statistics = RuleStatistics.Compute(Compile(Rule));

        Assert.Equal(9, statistics.NodeCount);
        Assert.Equal(4, statistics.Depth);
        Assert.Equal(5, statistics.TermCount);
        Assert.Equal(4, statistics.DistinctTermCount);
        Assert.Equal(["hasRole", "inRegion", "isActive", "isLocked"], statistics.Predicates);
        Assert.Equal(
            [
                new KeyValuePair<string, int>("and", 1),
                new KeyValuePair<string, int>("atLeast", 1),
                new KeyValuePair<string, int>("not", 1),
                new KeyValuePair<string, int>("or", 1),
            ],
            statistics.OperatorHistogram
        );
    }

    /// <summary>The variables and the data sources are the references of the rule, each once.</summary>
    [Fact]
    public void Compute_RuleWithVariables_ListsEachVariableAndSourceOnce_Test()
    {
        RuleStatistics statistics = RuleStatistics.Compute(
            Compile("p(a: from(\"user\", \"$.b\")) AND q(a: from(\"org\", \"$.a\")) AND r(a: from(\"user\", \"$.b\"))")
        );

        Assert.Equal([new VariableUse("org", "$.a"), new VariableUse("user", "$.b")], statistics.Variables);
        Assert.Equal(["org", "user"], statistics.DataSources);
    }

    /// <summary>A rule with four distinct terms has 81 K3 rows and 16 classical rows, within the cap.</summary>
    [Fact]
    public void Compute_FourTerms_GivesTruthTableSizeWithinTheCap_Test()
    {
        RuleStatistics statistics = RuleStatistics.Compute(Compile(Rule));

        Assert.Equal(new BigInteger(81), statistics.TruthTableRows);
        Assert.Equal(new BigInteger(16), statistics.ClassicalTruthTableRows);
        Assert.True(statistics.TruthTableWithinCap);
    }

    /// <summary>A rule with nine distinct terms is over the default cap of eight. (<c>f</c> is the constant <c>False</c>, so it is not used.)</summary>
    [Fact]
    public void Compute_NineTerms_IsOverTheCap_Test()
    {
        RuleStatistics statistics = RuleStatistics.Compute(Compile("a AND b AND c AND d AND e AND g AND h AND i AND j"));

        Assert.Equal(new BigInteger(19683), statistics.TruthTableRows);
        Assert.Equal(new BigInteger(512), statistics.ClassicalTruthTableRows);
        Assert.False(statistics.TruthTableWithinCap);
    }

    /// <summary>The constancy says whether the whole rule is a tautology, a contradiction or neither.</summary>
    /// <param name="text">The rule.</param>
    /// <param name="expected">The constancy.</param>
    [Theory]
    [InlineData("a OR True", RuleConstancy.Tautology)]
    [InlineData("a AND False", RuleConstancy.Contradiction)]
    [InlineData("a OR NOT a", RuleConstancy.Contingent)]
    [InlineData("IsKnown(a) OR IsUnknown(a)", RuleConstancy.Tautology)]
    public void Compute_Constancy_MatchesTheRule_Test(string text, RuleConstancy expected)
    {
        RuleStatistics statistics = RuleStatistics.Compute(Compile(text));

        Assert.Equal(expected, statistics.Constancy);
    }

    /// <summary>Over the analysis limit, the constancy is undecided and the BDD size is not computed.</summary>
    [Fact]
    public void Compute_OverTheAnalysisLimit_IsUndecidedWithNoBddSize_Test()
    {
        RuleCompiler<StubAssignment> compiler = RuleConverter.CreateCompiler(
            ToolSettings.Default with
            {
                MaxAnalysisTerms = 1,
            }
        );
        CompiledRule<StubAssignment> rule = compiler.Compile("a AND b").GetRuleOrThrow();

        RuleStatistics statistics = RuleStatistics.Compute(rule, maxAnalysisTerms: 1);

        Assert.Equal(RuleConstancy.Undecided, statistics.Constancy);
        Assert.Null(statistics.BddNodeCount);
    }

    /// <summary>Within the analysis limit, the BDD size is the size that the rule's metrics give.</summary>
    [Fact]
    public void Compute_WithinTheAnalysisLimit_GivesTheBddSize_Test()
    {
        CompiledRule<StubAssignment> rule = Compile(Rule);

        RuleStatistics statistics = RuleStatistics.Compute(rule);

        Assert.NotNull(statistics.BddNodeCount);
        Assert.Equal(rule.Metrics.BddNodeCount, statistics.BddNodeCount);
    }

    private static CompiledRule<StubAssignment> Compile(string text)
    {
        return RuleConverter.CreateCompiler().Compile(text).GetRuleOrThrow();
    }
}
