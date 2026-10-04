namespace TruthWeaver.Rewriting;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// Rewrites an <see cref="Expression"/> tree so its only logical operator is a single universal connective, <c>NAND</c> or
/// <c>NOR</c> (ADR-0005 decision 10). It first expands to the primitive kernel (<see cref="PrimitiveExpander"/>) and then
/// rewrites each kernel operator. The input is never modified.
/// </summary>
/// <remarks>
/// <para>
/// Identities (shown for <c>NAND</c>; the <c>NOR</c> rewrite is the exact dual with <c>AND</c> and <c>OR</c> swapped):
/// <c>NOT a = a NAND a</c>; <c>a AND b = (a NAND b) NAND (a NAND b)</c>;
/// <c>a OR b = (a NAND a) NAND (b NAND b)</c>. All three hold for <c>Unknown</c> because they only compose Kleene
/// connectives. <c>AND</c>/<c>OR</c> chains of more than two operands are folded left, which is sound because both are
/// associative in K3.
/// </para>
/// <para>
/// Cardinality: <c>AtLeast(k)</c> is the disjunction, over every k-element subset of the operands, of the conjunction of
/// that subset. That formula is monotone, and a monotone Kleene formula is <c>True</c> exactly when its pessimistic
/// completion (<c>Unknown</c> read as <c>False</c>) is true and <c>False</c> exactly when its optimistic completion is false,
/// which is the definitely-true / possibly-true interval semantics of the threshold. <c>AtMost(k)</c> is
/// <c>NOT AtLeast(k + 1)</c> and <c>Exactly(k)</c> is the conjunction of the two non-vacuous sides. The number of subsets is
/// <c>C(n, k)</c>, so very wide thresholds produce very large trees; the subset disjunction is balanced (logarithmic depth)
/// and every rewrite is capped by <c>CompilerOptions.MaxRewriteNodeCount</c>.
/// </para>
/// <para>
/// <b>Semantic boundary: <c>COALESCE</c>.</b> Every function built from <c>NAND</c>/<c>NOR</c> and constants is monotone
/// in the information order (<c>Unknown</c> below <c>True</c> and <c>False</c>), but <c>COALESCE(x, True)</c> maps
/// <c>Unknown</c> to <c>True</c> while mapping <c>False</c> to <c>False</c>, which no monotone function can do. So a
/// <c>COALESCE</c> node (and the inspection operators, which expand to it) cannot be rewritten; it is
/// left in place with its operands rewritten.
/// </para>
/// </remarks>
internal static class NandNorExpander
{
    /// <summary>Rewrites <paramref name="node"/> into <c>NAND</c>-only form (plus the <c>COALESCE</c> boundary).</summary>
    /// <param name="node">The tree to rewrite.</param>
    /// <param name="maxNodes">The most nodes, counted as a printed tree, the result may have.</param>
    /// <returns>The rewritten tree, or <see langword="null"/> when it would exceed <paramref name="maxNodes"/>.</returns>
    public static Expression? ToNand(Expression node, int maxNodes)
    {
        return Rewrite(node, nand: true, maxNodes);
    }

    /// <summary>Rewrites <paramref name="node"/> into <c>NOR</c>-only form (plus the <c>COALESCE</c> boundary).</summary>
    /// <param name="node">The tree to rewrite.</param>
    /// <param name="maxNodes">The most nodes, counted as a printed tree, the result may have.</param>
    /// <returns>The rewritten tree, or <see langword="null"/> when it would exceed <paramref name="maxNodes"/>.</returns>
    public static Expression? ToNor(Expression node, int maxNodes)
    {
        return Rewrite(node, nand: false, maxNodes);
    }

    /// <summary>
    /// Expands, checks the cost, and only then converts. The conversion walks the primitive tree as a tree (shared
    /// sub-expressions are visited once per appearance) and builds one conjunction per operand subset of every threshold,
    /// so both costs are bounded before any of that work is done.
    /// </summary>
    private static Expression? Rewrite(Expression node, bool nand, int maxNodes)
    {
        Expression primitive = PrimitiveExpander.Expand(node);

        // The NAND-only or NOR-only form is never smaller than the primitive form, so a primitive tree over the cap is already too big. This
        // also bounds the conversion walk, which would otherwise be exponential on nested XOR/EQUIVALENT/If.
        if (ExpressionTools.Size(primitive) > maxNodes)
        {
            return null;
        }

        // Each threshold subset contributes at least one node per operand, so that is a lower bound on the result.
        if (SubsetCost(primitive, maxNodes, new Dictionary<Expression, long>(ReferenceEqualityComparer.Instance)) > maxNodes)
        {
            return null;
        }

        Expression result = Convert(primitive, nand);
        return ExpressionTools.Size(result) > maxNodes ? null : result;
    }

    /// <summary>
    /// A lower bound on the nodes the threshold expansions below <paramref name="node"/> will add, counted per appearance
    /// and saturated just above <paramref name="cap"/> so the arithmetic cannot overflow.
    /// </summary>
    private static long SubsetCost(Expression node, int cap, Dictionary<Expression, long> memo)
    {
        if (memo.TryGetValue(node, out long known))
        {
            return known;
        }

        long total = node is ThresholdExpression t ? ThresholdCost(t, cap) : 0;
        foreach (Expression child in ExpressionTools.Children(node))
        {
            total = Math.Min(total + SubsetCost(child, cap, memo), (long)cap + 1);
        }

        memo[node] = total;
        return total;
    }

    /// <summary>The operand slots of every subset conjunction <see cref="ConvertThreshold"/> will build for <paramref name="t"/>.</summary>
    private static long ThresholdCost(ThresholdExpression t, int cap)
    {
        int n = t.Operands.Count;
        long limit = (long)cap + 1;
        return t.Comparison switch
        {
            ThresholdComparison.AtLeast => SubsetSlots(n, t.K, limit),
            ThresholdComparison.AtMost => SubsetSlots(n, t.K + 1, limit),
            ThresholdComparison.Exactly => Math.Min(
                (t.K >= 1 ? SubsetSlots(n, t.K, limit) : 0) + (t.K <= n - 1 ? SubsetSlots(n, t.K + 1, limit) : 0),
                limit
            ),
            _ => 0,
        };
    }

    /// <summary><c>C(n, k) * k</c> (the operand slots over every k-subset), saturated at <paramref name="limit"/>.</summary>
    private static long SubsetSlots(int n, int k, long limit)
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

    private static Expression Convert(Expression node, bool nand)
    {
        return node switch
        {
            ConstantExpression or TermExpression => node,
            NotExpression n => Not(Convert(n.Operand, nand), nand),
            AndExpression a => Fold(ConvertAll(a.Operands, nand), (l, r) => And(l, r, nand)),
            OrExpression o => Fold(ConvertAll(o.Operands, nand), (l, r) => Or(l, r, nand)),

            // The boundary: not expressible with a monotone connective, so only the operands are rewritten.
            CoalesceExpression c => new CoalesceExpression(new EquatableArray<Expression>(ConvertAll(c.Operands, nand))),
            ThresholdExpression t => ConvertThreshold(t, ConvertAll(t.Operands, nand), nand),

            // PrimitiveExpander has already removed every other node type.
            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType().Name}'."),
        };
    }

    private static List<Expression> ConvertAll(EquatableArray<Expression> operands, bool nand)
    {
        return [.. operands.Select(operand => Convert(operand, nand))];
    }

    private static Expression Fold(List<Expression> operands, Func<Expression, Expression, Expression> combine)
    {
        Expression result = operands[0];
        for (int i = 1; i < operands.Count; i++)
        {
            result = combine(result, operands[i]);
        }

        return result;
    }

    private static Expression Apply(Expression left, Expression right, bool nand)
    {
        return nand ? new NandExpression(left, right) : new NorExpression(left, right);
    }

    /// <summary><c>NOT a = a NAND a</c> (or <c>a NOR a</c> for NOR).</summary>
    private static Expression Not(Expression operand, bool nand)
    {
        return Apply(operand, operand, nand);
    }

    /// <summary>The operator the target connective negates (<c>AND</c> for NAND, <c>OR</c> for NOR): <c>X(X(a, b), X(a, b))</c> with <c>X</c> the target connective.</summary>
    private static Expression Negated(Expression left, Expression right, bool nand)
    {
        Expression inner = Apply(left, right, nand);
        return Apply(inner, inner, nand);
    }

    /// <summary>The dual operator (<c>OR</c> for NAND, <c>AND</c> for NOR): <c>X(NOT a, NOT b)</c> with <c>X</c> the target connective.</summary>
    private static Expression Dual(Expression left, Expression right, bool nand)
    {
        return Apply(Not(left, nand), Not(right, nand), nand);
    }

    private static Expression And(Expression left, Expression right, bool nand)
    {
        return nand ? Negated(left, right, nand) : Dual(left, right, nand);
    }

    private static Expression Or(Expression left, Expression right, bool nand)
    {
        return nand ? Dual(left, right, nand) : Negated(left, right, nand);
    }

    /// <summary>
    /// Builds the threshold from the (already converted) operands. <c>AtLeast(k)</c> / <c>AtMost(k)</c> /
    /// <c>Exactly(k)</c> only reach here with the compiler's valid <c>k</c>, so <c>AtLeast</c> is asked for only
    /// <c>1..n</c> and <c>Exactly(k)</c> drops whichever side would be vacuous (<c>AtLeast(0)</c> / <c>AtMost(n)</c>).
    /// </summary>
    private static Expression ConvertThreshold(ThresholdExpression t, List<Expression> operands, bool nand)
    {
        return t.Comparison switch
        {
            ThresholdComparison.AtLeast => AtLeast(t.K, operands, nand),
            ThresholdComparison.AtMost => AtMost(t.K, operands, nand),
            ThresholdComparison.Exactly => Exactly(t.K, operands, nand),
            _ => throw new InvalidOperationException(
                $"Threshold '{t.Comparison}' should have been expanded to AtLeast/AtMost."
            ),
        };
    }

    private static Expression AtLeast(int k, List<Expression> operands, bool nand)
    {
        List<Expression> subsetConjunctions = [];
        foreach (int[] subset in Combinations(operands.Count, k))
        {
            subsetConjunctions.Add(Fold([.. subset.Select(index => operands[index])], (l, r) => And(l, r, nand)));
        }

        // Balanced rather than left-folded: with thousands of subsets a left fold is thousands of levels deep, which the
        // size check (and any later recursive walk of the result) cannot traverse without exhausting the stack.
        return FoldBalanced(subsetConjunctions, 0, subsetConjunctions.Count, (l, r) => Or(l, r, nand));
    }

    /// <summary>Combines <c>items[start..end)</c> as a balanced tree; sound for the associative <c>OR</c>, and depth is logarithmic.</summary>
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

    /// <summary><c>count &lt;= k</c> is the negation of <c>count &gt;= k + 1</c> for every count in the interval, so it holds in K3.</summary>
    private static Expression AtMost(int k, List<Expression> operands, bool nand)
    {
        return Not(AtLeast(k + 1, operands, nand), nand);
    }

    private static Expression Exactly(int k, List<Expression> operands, bool nand)
    {
        bool hasLower = k >= 1;
        bool hasUpper = k <= operands.Count - 1;
        if (hasLower && hasUpper)
        {
            return And(AtLeast(k, operands, nand), AtMost(k, operands, nand), nand);
        }

        return hasLower ? AtLeast(k, operands, nand) : AtMost(k, operands, nand);
    }

    /// <summary>Every ascending index combination of size <paramref name="size"/> drawn from <c>0..count-1</c>.</summary>
    private static IEnumerable<int[]> Combinations(int count, int size)
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
}
