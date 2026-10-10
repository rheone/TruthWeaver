namespace TruthWeaver.Rewriting;

using TruthWeaver.Ast;

/// <summary>
/// Expands a threshold into plain connectives. <c>AtLeast(k)</c> is the disjunction, over every k-element subset of the
/// operands, of the conjunction of that subset. That formula is monotone, so in Strong Kleene logic it is <c>True</c>
/// exactly when the threshold is definitely met and <c>False</c> exactly when it is impossible, which is the
/// definitely-true / possibly-true interval semantics of the threshold. The other comparisons follow from
/// <see cref="ThresholdSemantics.Terms"/>: <c>AtMost(k)</c> is <c>NOT AtLeast(k + 1)</c> and <c>Exactly(k)</c> is the
/// conjunction of both tests.
/// </summary>
/// <remarks>
/// The subset count is <c>C(n, k)</c>, so a wide threshold gives a very large tree. The entry point therefore takes a
/// node budget, checks a cheap lower bound before it builds anything, and returns <see langword="null"/> when the result
/// would not fit, like <see cref="NandNorExpander"/>.
/// </remarks>
internal static class ThresholdExpansion
{
    /// <summary>Expands a threshold comparison over already-built operands.</summary>
    /// <param name="comparison">The comparison.</param>
    /// <param name="k">The threshold.</param>
    /// <param name="operands">The operands, already in the target form.</param>
    /// <param name="connectives">The connectives the result is written in.</param>
    /// <param name="maxNodes">The most nodes, counted as a printed tree, the result may have.</param>
    /// <returns>The expansion, or <see langword="null"/> when it would exceed <paramref name="maxNodes"/>.</returns>
    /// <exception cref="ArgumentException">The operand count alone decides the comparison, so there is nothing to expand.</exception>
    public static Expression? Expand(
        ThresholdComparison comparison,
        int k,
        IReadOnlyList<Expression> operands,
        ThresholdConnectives connectives,
        int maxNodes
    )
    {
        if (ThresholdSemantics.Constant(comparison, k, operands.Count) is not null)
        {
            throw new ArgumentException(
                "The operand count alone decides this threshold; it has no expansion.",
                nameof(comparison)
            );
        }

        ThresholdTerms terms = ThresholdSemantics.Terms(comparison, k);

        // A test that holds for every count (AtLeast(0), or an upper bound above n) adds nothing and is left out.
        int? lower = terms.AtLeast is > 0 ? terms.AtLeast : null;
        int? upper = terms.NotAtLeast is { } bound && bound <= operands.Count ? bound : null;

        long limit = (long)maxNodes + 1;
        long slots = Math.Min(
            (lower is { } l ? SubsetSlots(operands.Count, l, limit) : 0)
                + (upper is { } u ? SubsetSlots(operands.Count, u, limit) : 0),
            limit
        );
        if (slots > maxNodes)
        {
            return null;
        }

        Expression? result = lower is { } low ? AtLeast(low, operands, connectives) : null;
        if (upper is { } high)
        {
            Expression negated = connectives.Not(AtLeast(high, operands, connectives));
            result = result is null ? negated : connectives.And(result, negated);
        }

        // The slot check is only a lower bound, so the finished tree is measured once.
        return ExpressionTools.Size(result!) > maxNodes ? null : result;
    }

    /// <summary>Every ascending index combination of size <paramref name="size"/> drawn from <c>0..count-1</c>.</summary>
    /// <param name="count">The number of items to choose from.</param>
    /// <param name="size">The size of each subset; must be between 1 and <paramref name="count"/>.</param>
    /// <returns>The subsets in lexicographic order, each as a fresh array of indexes.</returns>
    public static IEnumerable<int[]> Subsets(int count, int size)
    {
        int[] current = new int[size];
        for (int i = 0; i < size; i++)
        {
            current[i] = i;
        }

        while (true)
        {
            yield return [.. current];

            // Advance the rightmost index that still has room, then reset everything after it.
            int position = size - 1;
            while (position >= 0 && current[position] == count - size + position)
            {
                position--;
            }

            if (position < 0)
            {
                yield break;
            }

            current[position]++;
            for (int i = position + 1; i < size; i++)
            {
                current[i] = current[i - 1] + 1;
            }
        }
    }

    /// <summary><c>C(n, k) * k</c>, the operand slots over every k-subset, saturated at <paramref name="limit"/>.</summary>
    /// <param name="n">The number of operands.</param>
    /// <param name="k">The subset size.</param>
    /// <param name="limit">The value at which the result saturates, so the arithmetic cannot overflow.</param>
    /// <returns>The slot count, or <paramref name="limit"/> when it is at least that large.</returns>
    public static long SubsetSlots(int n, int k, long limit)
    {
        // C(n, k) = C(n, n - k); walking the smaller side keeps the running product exact (each step is an exact division).
        int small = Math.Min(k, n - k);
        long combinations = 1;
        for (int i = 0; i < small; i++)
        {
            combinations = combinations * (n - i) / (i + 1);
            if (combinations > limit)
            {
                return limit;
            }
        }

        return Math.Min(combinations * Math.Max(k, 1), limit);
    }

    /// <summary>
    /// The approximate node count of a full expansion, as a <see langword="double"/> so a very wide threshold gives a
    /// large estimate instead of a saturated or overflowed one. It is a message figure, not a budget check.
    /// </summary>
    /// <param name="comparison">The comparison.</param>
    /// <param name="k">The threshold.</param>
    /// <param name="operandCount">The number of operands.</param>
    /// <returns>The estimated node count.</returns>
    public static double EstimateNodes(ThresholdComparison comparison, int k, int operandCount)
    {
        ThresholdTerms terms = ThresholdSemantics.Terms(comparison, k);
        double total = 0;
        if (terms.AtLeast is { } lower)
        {
            total += LevelSize(lower, operandCount);
        }

        if (terms.NotAtLeast is { } upper)
        {
            total += LevelSize(upper, operandCount);
        }

        // Two tests are joined by one more connective.
        return terms is { AtLeast: not null, NotAtLeast: not null } ? total + 1 : total;
    }

    /// <summary>One group of <c>k</c> operands per subset, plus the join; a level the count decides is a single constant.</summary>
    private static double LevelSize(int k, int n)
    {
        if (k <= 0 || k > n)
        {
            return 1;
        }

        // C(n, k) as a double: it loses precision instead of overflowing. Walking the smaller side keeps it short.
        int small = Math.Min(k, n - k);
        double combinations = 1;
        for (int i = 1; i <= small; i++)
        {
            combinations = combinations * (n - small + i) / i;
        }

        return (combinations * (k + 1)) + 1;
    }

    /// <summary>The disjunction of the conjunction of every k-subset, balanced so the depth is logarithmic.</summary>
    private static Expression AtLeast(int k, IReadOnlyList<Expression> operands, ThresholdConnectives connectives)
    {
        List<Expression> conjunctions = [];
        foreach (int[] subset in Subsets(operands.Count, k))
        {
            Expression conjunction = operands[subset[0]];
            for (int i = 1; i < subset.Length; i++)
            {
                conjunction = connectives.And(conjunction, operands[subset[i]]);
            }

            conjunctions.Add(conjunction);
        }

        // Balanced rather than left-folded: with thousands of subsets a left fold is thousands of levels deep, which the
        // size check (and any later recursive walk of the result) cannot traverse without exhausting the stack.
        return FoldBalanced(conjunctions, 0, conjunctions.Count, connectives.Or);
    }

    /// <summary>Combines <c>items[start..end)</c> as a balanced tree; sound for the associative <c>OR</c>.</summary>
    private static Expression FoldBalanced(
        List<Expression> items,
        int start,
        int end,
        Func<Expression, Expression, Expression> combine
    )
    {
        if (end - start == 1)
        {
            return items[start];
        }

        int middle = start + ((end - start) / 2);
        return combine(FoldBalanced(items, start, middle, combine), FoldBalanced(items, middle, end, combine));
    }
}
