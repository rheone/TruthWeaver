namespace TruthWeaver.Ast;

/// <summary>
/// What the threshold comparisons mean, stated once. It works on <c>(comparison, k, operand count)</c> and has no
/// <see cref="Expression"/> in its interface, so the canonical form, the analyzer and the rewriters share one set of
/// <c>k + 1</c> and <c>k - 1</c> rules. The true-operand count decides every comparison.
/// </summary>
internal static class ThresholdSemantics
{
    /// <summary>
    /// Maps <c>GreaterThan(k)</c> to <c>AtLeast(k + 1)</c> and <c>LessThan(k)</c> to <c>AtMost(k - 1)</c>. The other
    /// comparisons are returned unchanged.
    /// </summary>
    /// <param name="comparison">The comparison to normalise.</param>
    /// <param name="k">The threshold.</param>
    /// <returns>The normalised comparison and threshold.</returns>
    public static (ThresholdComparison Comparison, int K) Normalise(ThresholdComparison comparison, int k)
    {
        return comparison switch
        {
            ThresholdComparison.GreaterThan => (ThresholdComparison.AtLeast, Next(k)),
            ThresholdComparison.LessThan => (ThresholdComparison.AtMost, k == int.MinValue ? k : k - 1),
            _ => (comparison, k),
        };
    }

    /// <summary>
    /// Writes a comparison with "at least" tests only. <c>AtMost(k)</c> is <c>NOT AtLeast(k + 1)</c> because the count
    /// is an integer, and <c>Exactly(k)</c> is <c>AtLeast(k) AND NOT AtLeast(k + 1)</c>.
    /// </summary>
    /// <param name="comparison">The comparison.</param>
    /// <param name="k">The threshold.</param>
    /// <returns>The lower and negated-upper tests.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="comparison"/> is not a defined comparison.</exception>
    public static ThresholdTerms Terms(ThresholdComparison comparison, int k)
    {
        (ThresholdComparison normalised, int normalisedK) = Normalise(comparison, k);
        return normalised switch
        {
            ThresholdComparison.AtLeast => new ThresholdTerms(normalisedK, null),

            // The count is an integer, so count <= k is the negation of count >= k + 1.
            ThresholdComparison.AtMost => new ThresholdTerms(null, Next(normalisedK)),
            ThresholdComparison.Exactly => new ThresholdTerms(normalisedK, Next(normalisedK)),
            _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Undefined threshold comparison."),
        };
    }

    /// <summary>Splits <c>Between(min, max)</c> into <c>AtLeast(min) AND NOT AtLeast(max + 1)</c>.</summary>
    /// <param name="min">The smallest accepted count.</param>
    /// <param name="max">The largest accepted count.</param>
    /// <returns>The lower and negated-upper tests.</returns>
    public static ThresholdTerms Between(int min, int max)
    {
        return new ThresholdTerms(min, Next(max));
    }

    /// <summary>
    /// Returns the outcome when the operand count alone decides the terms, or <see langword="null"/> when it does not.
    /// A lower test at or below zero and an upper test above the operand count hold for every count. A lower test above
    /// the operand count, or an empty interval, holds for none.
    /// </summary>
    /// <param name="terms">The terms to check.</param>
    /// <param name="operandCount">The number of operands.</param>
    /// <returns><see langword="true"/> or <see langword="false"/> when the outcome is fixed; otherwise <see langword="null"/>.</returns>
    public static bool? Constant(ThresholdTerms terms, int operandCount)
    {
        // Long arithmetic: an int.MaxValue threshold must not overflow while its neighbour is computed.
        long low = Math.Max(terms.AtLeast ?? 0L, 0L);
        long high = Math.Min((terms.NotAtLeast ?? (operandCount + 1L)) - 1L, operandCount);
        if (low > high)
        {
            return false;
        }

        return low == 0 && high == operandCount ? true : null;
    }

    /// <summary>Returns the fixed outcome of a comparison for an operand count, or <see langword="null"/> when it depends on the operands.</summary>
    /// <param name="comparison">The comparison.</param>
    /// <param name="k">The threshold.</param>
    /// <param name="operandCount">The number of operands.</param>
    /// <returns><see langword="true"/> or <see langword="false"/> when the outcome is fixed; otherwise <see langword="null"/>.</returns>
    public static bool? Constant(ThresholdComparison comparison, int k, int operandCount)
    {
        return Constant(Terms(comparison, k), operandCount);
    }

    /// <summary>
    /// Returns <c>k + 1</c>, held at <see cref="int.MaxValue"/> instead of wrapping. No operand count can reach
    /// <see cref="int.MaxValue"/>, so a held bound keeps its meaning ("never reached") where a wrapped one would flip it.
    /// </summary>
    private static int Next(int k)
    {
        return k == int.MaxValue ? k : k + 1;
    }
}
