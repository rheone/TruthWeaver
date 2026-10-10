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
        if (SubsetCost(primitive, maxNodes, [with(ReferenceEqualityComparer.Instance)]) > maxNodes)
        {
            return null;
        }

        Conversion conversion = new(nand, maxNodes);
        Expression result = Convert(primitive, conversion);
        return conversion.OverBudget || ExpressionTools.Size(result) > maxNodes ? null : result;
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

    /// <summary>The operand slots of every subset conjunction the threshold expansion will build for <paramref name="t"/>.</summary>
    private static long ThresholdCost(ThresholdExpression t, int cap)
    {
        int n = t.Operands.Count;
        long limit = (long)cap + 1;
        return t.Comparison switch
        {
            ThresholdComparison.AtLeast => ThresholdExpansion.SubsetSlots(n, t.K, limit),
            ThresholdComparison.AtMost => ThresholdExpansion.SubsetSlots(n, t.K + 1, limit),
            ThresholdComparison.Exactly => Math.Min(
                (t.K >= 1 ? ThresholdExpansion.SubsetSlots(n, t.K, limit) : 0)
                    + (t.K <= n - 1 ? ThresholdExpansion.SubsetSlots(n, t.K + 1, limit) : 0),
                limit
            ),
            _ => 0,
        };
    }

    private static Expression Convert(Expression node, Conversion conversion)
    {
        bool nand = conversion.Nand;
        return node switch
        {
            ConstantExpression or TermExpression => node,
            NotExpression n => Not(Convert(n.Operand, conversion), nand),
            AndExpression a => Fold(ConvertAll(a.Operands, conversion), (l, r) => And(l, r, nand)),
            OrExpression o => Fold(ConvertAll(o.Operands, conversion), (l, r) => Or(l, r, nand)),

            // The boundary: not expressible with a monotone connective, so only the operands are rewritten.
            CoalesceExpression c => new CoalesceExpression(new EquatableArray<Expression>(ConvertAll(c.Operands, conversion))),
            ThresholdExpression t => ConvertThreshold(t, ConvertAll(t.Operands, conversion), conversion),

            // PrimitiveExpander has already removed every other node type.
            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType().Name}'."),
        };
    }

    private static List<Expression> ConvertAll(EquatableArray<Expression> operands, Conversion conversion)
    {
        return [.. operands.Select(operand => Convert(operand, conversion))];
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
    /// Builds the threshold from the (already converted) operands through <see cref="ThresholdExpansion"/>, written in
    /// the target connective. A threshold whose outcome the operand count alone fixes becomes a constant. A threshold
    /// over budget marks the conversion, and the caller discards the result.
    /// </summary>
    private static Expression ConvertThreshold(ThresholdExpression t, List<Expression> operands, Conversion conversion)
    {
        bool nand = conversion.Nand;
        if (ThresholdSemantics.Constant(t.Comparison, t.K, operands.Count) is { } fixedOutcome)
        {
            return new ConstantExpression(fixedOutcome ? TruthValue.True : TruthValue.False);
        }

        ThresholdConnectives connectives = new((l, r) => And(l, r, nand), (l, r) => Or(l, r, nand), o => Not(o, nand));
        Expression? expansion = ThresholdExpansion.Expand(t.Comparison, t.K, operands, connectives, conversion.MaxNodes);
        if (expansion is null)
        {
            conversion.OverBudget = true;
            return new ConstantExpression(TruthValue.Unknown);
        }

        return expansion;
    }

    /// <summary>The target connective, the node cap, and whether any threshold exceeded the cap during one conversion.</summary>
    private sealed class Conversion(bool nand, int maxNodes)
    {
        public bool Nand { get; } = nand;

        public int MaxNodes { get; } = maxNodes;

        public bool OverBudget { get; set; }
    }
}
