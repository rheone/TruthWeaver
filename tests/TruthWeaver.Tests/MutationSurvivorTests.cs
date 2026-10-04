namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Ast;
using TruthWeaver.Evaluation;

/// <summary>
/// Boundary behaviors that mutation testing (Stryker.NET, ticket k3-hardening 10) showed no other test pinned: the exact
/// edges of a limit, where an off-by-one mutant survived.
/// </summary>
public sealed class MutationSurvivorTests
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

    /// <summary>A whole decimal at the largest long narrows to an integer argument.</summary>
    [Fact]
    public void TryConvert_DecimalAtLongMaxValue_NarrowsToInt64_Test()
    {
        bool converted = VariableConversion.TryConvert(
            [LiteralValue.OfDecimal(long.MaxValue)],
            LiteralKind.Int64,
            out LiteralValue value,
            out _,
            out _
        );

        Assert.True(converted);
        Assert.Equal(long.MaxValue, value.AsInt64());
    }

    /// <summary>A whole decimal at the smallest long narrows to an integer argument.</summary>
    [Fact]
    public void TryConvert_DecimalAtLongMinValue_NarrowsToInt64_Test()
    {
        bool converted = VariableConversion.TryConvert(
            [LiteralValue.OfDecimal(long.MinValue)],
            LiteralKind.Int64,
            out LiteralValue value,
            out _,
            out _
        );

        Assert.True(converted);
        Assert.Equal(long.MinValue, value.AsInt64());
    }

    private static TermExpression Term(string name)
    {
        return new TermExpression(new TermIdentity(name, []));
    }
}
