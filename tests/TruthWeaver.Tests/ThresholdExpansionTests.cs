namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Rewriting;

/// <summary>Tests the shared threshold subset enumeration and budgeted expansion directly.</summary>
public sealed class ThresholdExpansionTests
{
    private static readonly ThresholdConnectives Primitive = new(
        (l, r) => new AndExpression(new EquatableArray<Expression>([l, r])),
        (l, r) => new OrExpression(new EquatableArray<Expression>([l, r])),
        operand => new NotExpression(operand)
    );

    /// <summary>Subsets are listed in lexicographic order and there are C(n, k) of them.</summary>
    [Fact]
    public void Subsets_FourChooseTwo_ListsSixAscendingCombinations_Test()
    {
        List<string> subsets = [.. ThresholdExpansion.Subsets(4, 2).Select(s => string.Join(',', s))];

        Assert.Equal(["0,1", "0,2", "0,3", "1,2", "1,3", "2,3"], subsets);
    }

    /// <summary>Choosing every item yields the single full subset.</summary>
    [Fact]
    public void Subsets_SizeEqualToCount_ListsOneSubset_Test()
    {
        Assert.Equal(
            [
                [0, 1, 2],
            ],
            ThresholdExpansion.Subsets(3, 3).ToList()
        );
    }

    /// <summary>The slot count is C(n, k) times k, and saturates at the limit instead of overflowing.</summary>
    [Fact]
    public void SubsetSlots_WideThreshold_SaturatesAtTheLimit_Test()
    {
        Assert.Equal(12, ThresholdExpansion.SubsetSlots(4, 2, 1000));
        Assert.Equal(1000, ThresholdExpansion.SubsetSlots(200, 100, 1000));
    }

    /// <summary>AtLeast(2) over three operands is the disjunction of the three pair conjunctions.</summary>
    [Fact]
    public void Expand_AtLeastTwoOfThree_IsTheDisjunctionOfPairs_Test()
    {
        Expression? result = ThresholdExpansion.Expand(ThresholdComparison.AtLeast, 2, Terms(3), Primitive, 1000);

        Assert.NotNull(result);
        Assert.Equal(3, CountAnd(result));
    }

    /// <summary>AtMost(k) is the negation of AtLeast(k + 1).</summary>
    [Fact]
    public void Expand_AtMostOne_IsTheNegationOfAtLeastTwo_Test()
    {
        Expression? atMost = ThresholdExpansion.Expand(ThresholdComparison.AtMost, 1, Terms(3), Primitive, 1000);
        Expression? atLeast = ThresholdExpansion.Expand(ThresholdComparison.AtLeast, 2, Terms(3), Primitive, 1000);

        Assert.Equal(new NotExpression(atLeast!), atMost);
    }

    /// <summary>Exactly(k) joins the lower test and the negated upper test.</summary>
    [Fact]
    public void Expand_ExactlyOneOfThree_ConjoinsBothTests_Test()
    {
        Expression? result = ThresholdExpansion.Expand(ThresholdComparison.Exactly, 1, Terms(3), Primitive, 1000);

        AndExpression and = Assert.IsType<AndExpression>(result);
        Assert.IsType<NotExpression>(and.Operands[1]);
    }

    /// <summary>Exactly(0) has no lower test, so only the negated upper test remains.</summary>
    [Fact]
    public void Expand_ExactlyZero_KeepsOnlyTheUpperTest_Test()
    {
        Expression? result = ThresholdExpansion.Expand(ThresholdComparison.Exactly, 0, Terms(3), Primitive, 1000);

        Assert.IsType<NotExpression>(result);
    }

    /// <summary>A threshold that needs more nodes than the budget returns null.</summary>
    [Fact]
    public void Expand_OverBudget_ReturnsNull_Test()
    {
        Assert.Null(ThresholdExpansion.Expand(ThresholdComparison.AtLeast, 5, Terms(12), Primitive, 50));
    }

    /// <summary>A wide threshold is rejected from the slot count alone, before any subset is built.</summary>
    [Fact]
    public void Expand_AbsurdlyWideThreshold_ReturnsNullWithoutBuilding_Test()
    {
        bool built = false;
        ThresholdConnectives connectives = new(
            (l, _) =>
            {
                built = true;
                return l;
            },
            (l, _) => l,
            operand => operand
        );

        Expression? result = ThresholdExpansion.Expand(ThresholdComparison.AtLeast, 100, Terms(200), connectives, 10_000);

        Assert.Null(result);
        Assert.False(built);
    }

    /// <summary>Thousands of subsets fold into a shallow tree, not a chain thousands of levels deep.</summary>
    [Fact]
    public void Expand_ManySubsets_FoldsTheDisjunctionBalanced_Test()
    {
        Expression? result = ThresholdExpansion.Expand(ThresholdComparison.AtLeast, 7, Terms(14), Primitive, 1_000_000);

        Assert.NotNull(result);
        Assert.InRange(Depth(result), 1, 30);
    }

    /// <summary>A comparison the operand count decides has nothing to expand.</summary>
    [Fact]
    public void Expand_ThresholdDecidedByOperandCount_Throws_Test()
    {
        Assert.Throws<ArgumentException>(() =>
            ThresholdExpansion.Expand(ThresholdComparison.AtLeast, 0, Terms(3), Primitive, 1000)
        );
    }

    /// <summary>The estimate counts one group per subset plus a join for each test, and one more to join two tests.</summary>
    [Theory]
    [InlineData(ThresholdComparison.AtLeast, 2, 4, 19)]
    [InlineData(ThresholdComparison.AtMost, 1, 4, 19)]
    [InlineData(ThresholdComparison.Exactly, 1, 4, 9 + 19 + 1)]
    public void EstimateNodes_Comparison_CountsSubsetGroupsPerTest_Test(
        ThresholdComparison comparison,
        int k,
        int operandCount,
        double expected
    )
    {
        double estimate = ThresholdExpansion.EstimateNodes(comparison, k, operandCount);

        Assert.Equal(expected, estimate);
    }

    private static List<Expression> Terms(int count)
    {
        return [.. Enumerable.Range(0, count).Select(i => (Expression)new TermExpression(new TermIdentity($"p{i}", [])))];
    }

    private static int Depth(Expression node)
    {
        // Iterative would be overkill: the balanced fold under test keeps the depth small enough to recurse.
        return 1 + ExpressionTools.Children(node).Select(Depth).DefaultIfEmpty(0).Max();
    }

    private static int CountAnd(Expression node)
    {
        return (node is AndExpression ? 1 : 0) + ExpressionTools.Children(node).Sum(CountAnd);
    }
}
