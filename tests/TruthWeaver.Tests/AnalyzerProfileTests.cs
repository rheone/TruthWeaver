namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Ast;

/// <summary>The term cap that decides whether <see cref="Analyzer.Profile"/> analyses a tree.</summary>
public sealed class AnalyzerProfileTests
{
    /// <summary>A tree with exactly the term cap is analysed; the cap is inclusive.</summary>
    [Fact]
    public void Profile_TreeWithExactlyTheTermCap_IsAnalysed_Test()
    {
        AndExpression tree = new(new EquatableArray<Expression>([Term("a"), Term("b")]));

        ValueProfile? profile = Analyzer.Profile(tree, 2);

        Assert.NotNull(profile);
    }

    /// <summary>A tree with more terms than the cap is not analysed.</summary>
    [Fact]
    public void Profile_TreeAboveTheTermCap_IsNotAnalysed_Test()
    {
        AndExpression tree = new(new EquatableArray<Expression>([Term("a"), Term("b")]));

        Assert.Null(Analyzer.Profile(tree, 1));
    }

    private static TermExpression Term(string name)
    {
        return new TermExpression(new TermIdentity(name, []));
    }
}
