namespace TruthWeaver.Tests;

using TruthWeaver.Ast;

/// <summary>Tests the threshold core directly, with no <see cref="Expression"/> involved.</summary>
public sealed class ThresholdSemanticsTests
{
    /// <summary>GreaterThan(k) becomes AtLeast(k + 1) and LessThan(k) becomes AtMost(k - 1); the rest are unchanged.</summary>
    [Theory]
    [InlineData(ThresholdComparison.GreaterThan, 2, ThresholdComparison.AtLeast, 3)]
    [InlineData(ThresholdComparison.LessThan, 2, ThresholdComparison.AtMost, 1)]
    [InlineData(ThresholdComparison.AtLeast, 2, ThresholdComparison.AtLeast, 2)]
    [InlineData(ThresholdComparison.AtMost, 2, ThresholdComparison.AtMost, 2)]
    [InlineData(ThresholdComparison.Exactly, 2, ThresholdComparison.Exactly, 2)]
    public void Normalise_Comparison_MapsStrictFormsToInclusiveOnes_Test(
        ThresholdComparison comparison,
        int k,
        ThresholdComparison expectedComparison,
        int expectedK
    )
    {
        (ThresholdComparison actualComparison, int actualK) = ThresholdSemantics.Normalise(comparison, k);

        Assert.Equal(expectedComparison, actualComparison);
        Assert.Equal(expectedK, actualK);
    }

    /// <summary>Each comparison is written with at-least tests; AtMost is the negation of AtLeast(k + 1).</summary>
    [Theory]
    [InlineData(ThresholdComparison.AtLeast, 2, 2, null)]
    [InlineData(ThresholdComparison.AtMost, 2, null, 3)]
    [InlineData(ThresholdComparison.GreaterThan, 2, 3, null)]
    [InlineData(ThresholdComparison.LessThan, 2, null, 2)]
    [InlineData(ThresholdComparison.Exactly, 2, 2, 3)]
    public void Terms_Comparison_ReturnsLowerAndNegatedUpperTests_Test(
        ThresholdComparison comparison,
        int k,
        int? expectedAtLeast,
        int? expectedNotAtLeast
    )
    {
        ThresholdTerms terms = ThresholdSemantics.Terms(comparison, k);

        Assert.Equal(new ThresholdTerms(expectedAtLeast, expectedNotAtLeast), terms);
    }

    /// <summary>The negation of AtLeast(k) is AtMost(k - 1) and of AtMost(k) is AtLeast(k + 1); Exactly has no single negation.</summary>
    [Theory]
    [InlineData(ThresholdComparison.AtLeast, 2, ThresholdComparison.AtMost, 1)]
    [InlineData(ThresholdComparison.AtMost, 2, ThresholdComparison.AtLeast, 3)]
    [InlineData(ThresholdComparison.AtMost, int.MaxValue, ThresholdComparison.AtLeast, int.MaxValue)]
    [InlineData(ThresholdComparison.AtLeast, int.MinValue, ThresholdComparison.AtMost, int.MinValue)]
    public void Negate_AtLeastOrAtMost_ReturnsTheOppositeComparison_Test(
        ThresholdComparison comparison,
        int k,
        ThresholdComparison expectedComparison,
        int expectedK
    )
    {
        (ThresholdComparison, int)? negated = ThresholdSemantics.Negate(comparison, k);

        Assert.Equal((expectedComparison, expectedK), negated);
    }

    /// <summary>Exactly is not the negation of any single threshold, so Negate reports none.</summary>
    [Fact]
    public void Negate_Exactly_ReturnsNull_Test()
    {
        Assert.Null(ThresholdSemantics.Negate(ThresholdComparison.Exactly, 2));
    }

    /// <summary>Between(min, max) splits into AtLeast(min) and NOT AtLeast(max + 1).</summary>
    [Fact]
    public void Between_MinAndMax_SplitsIntoLowerAndNegatedUpperTests_Test()
    {
        ThresholdTerms terms = ThresholdSemantics.Between(1, 3);

        Assert.Equal(new ThresholdTerms(1, 4), terms);
    }

    /// <summary>An undefined comparison is a programming error and is reported as one.</summary>
    [Fact]
    public void Terms_UndefinedComparison_Throws_Test()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ThresholdSemantics.Terms((ThresholdComparison)99, 1));
    }

    /// <summary>With three operands, a threshold beyond either end of the count range gives a fixed outcome for every comparison.</summary>
    [Theory]
    [InlineData(ThresholdComparison.AtLeast, 0, true)]
    [InlineData(ThresholdComparison.AtLeast, -1, true)]
    [InlineData(ThresholdComparison.AtLeast, 4, false)]
    [InlineData(ThresholdComparison.AtMost, -1, false)]
    [InlineData(ThresholdComparison.AtMost, 3, true)]
    [InlineData(ThresholdComparison.AtMost, 4, true)]
    [InlineData(ThresholdComparison.GreaterThan, -1, true)]
    [InlineData(ThresholdComparison.GreaterThan, 3, false)]
    [InlineData(ThresholdComparison.GreaterThan, 4, false)]
    [InlineData(ThresholdComparison.LessThan, 0, false)]
    [InlineData(ThresholdComparison.LessThan, 4, true)]
    [InlineData(ThresholdComparison.LessThan, 5, true)]
    [InlineData(ThresholdComparison.Exactly, -1, false)]
    [InlineData(ThresholdComparison.Exactly, 4, false)]
    public void Constant_ThresholdOutsideOperandRange_ReturnsFixedOutcome_Test(
        ThresholdComparison comparison,
        int k,
        bool expected
    )
    {
        bool? outcome = ThresholdSemantics.Constant(comparison, k, 3);

        Assert.Equal(expected, outcome);
    }

    /// <summary>A threshold inside the operand range depends on the operands, so no outcome is fixed.</summary>
    [Theory]
    [InlineData(ThresholdComparison.AtLeast, 1)]
    [InlineData(ThresholdComparison.AtLeast, 3)]
    [InlineData(ThresholdComparison.AtMost, 0)]
    [InlineData(ThresholdComparison.AtMost, 2)]
    [InlineData(ThresholdComparison.GreaterThan, 0)]
    [InlineData(ThresholdComparison.GreaterThan, 2)]
    [InlineData(ThresholdComparison.LessThan, 1)]
    [InlineData(ThresholdComparison.LessThan, 3)]
    [InlineData(ThresholdComparison.Exactly, 0)]
    [InlineData(ThresholdComparison.Exactly, 3)]
    public void Constant_ThresholdInsideOperandRange_ReturnsNull_Test(ThresholdComparison comparison, int k)
    {
        bool? outcome = ThresholdSemantics.Constant(comparison, k, 3);

        Assert.Null(outcome);
    }

    /// <summary>The extreme threshold values do not overflow while the neighbouring bound is computed.</summary>
    [Fact]
    public void Constant_MaxIntThreshold_DoesNotOverflow_Test()
    {
        Assert.False(ThresholdSemantics.Constant(ThresholdComparison.AtLeast, int.MaxValue, 3));
        Assert.True(ThresholdSemantics.Constant(ThresholdComparison.AtMost, int.MaxValue, 3));
    }

    /// <summary>Between with an empty or out-of-range interval is a fixed outcome.</summary>
    [Theory]
    [InlineData(2, 1, false)]
    [InlineData(4, 6, false)]
    [InlineData(-2, -1, false)]
    [InlineData(0, 3, true)]
    [InlineData(-1, 9, true)]
    public void Constant_BetweenOutsideOrCoveringOperandRange_ReturnsFixedOutcome_Test(int min, int max, bool expected)
    {
        bool? outcome = ThresholdSemantics.Constant(ThresholdSemantics.Between(min, max), 3);

        Assert.Equal(expected, outcome);
    }
}
