namespace TruthWeaver.Rewriting;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// Structural helpers shared by the opt-in rewrites (compression, canonicalisation and simplification): generic child
/// mapping, tree size and operand-array construction. Nothing here changes what an expression means.
/// </summary>
internal static class ExpressionTools
{
    /// <summary>Builds an operand array from <paramref name="operands"/>.</summary>
    /// <param name="operands">The operands, in order.</param>
    /// <returns>The immutable operand array.</returns>
    public static EquatableArray<Expression> Array(IEnumerable<Expression> operands)
    {
        return new EquatableArray<Expression>(operands);
    }

    /// <summary>
    /// Builds an <c>AND</c> or <c>OR</c> node over <paramref name="operands"/> as given: no folding, flattening or
    /// single-operand collapse, so the caller decides what the operand list means.
    /// </summary>
    /// <param name="operands">The operands, in order.</param>
    /// <param name="isAnd"><see langword="true"/> for <c>AND</c>, <see langword="false"/> for <c>OR</c>.</param>
    /// <returns>The junction node.</returns>
    public static Expression Junction(IEnumerable<Expression> operands, bool isAnd)
    {
        EquatableArray<Expression> array = Array(operands);
        return isAnd ? new AndExpression(array) : new OrExpression(array);
    }

    /// <summary>
    /// Returns <paramref name="node"/> with every direct child replaced by <paramref name="map"/> of that child. When no
    /// child changes (by reference) the original node is returned, which keeps shared sub-trees shared and lets callers
    /// detect "nothing happened" cheaply.
    /// </summary>
    /// <param name="node">The node whose children are mapped.</param>
    /// <param name="map">The child transformation.</param>
    /// <returns>The node with mapped children, or <paramref name="node"/> itself when none changed.</returns>
    public static Expression MapChildren(Expression node, Func<Expression, Expression> map)
    {
        if (node is ConstantExpression or TermExpression)
        {
            return node;
        }

        // The child list comes from ExpressionShape, the one place that knows each operator's operands; only the
        // rebuild below is per operator.
        IReadOnlyList<Expression> operands = ExpressionShape.Of(node).Operands;
        Expression[] mapped = new Expression[operands.Count];
        bool changed = false;
        for (int i = 0; i < mapped.Length; i++)
        {
            mapped[i] = map(operands[i]);
            changed |= !ReferenceEquals(mapped[i], operands[i]);
        }

        return changed ? Rebuild(node, mapped) : node;
    }

    /// <summary>Lists the direct children of <paramref name="node"/> in operand order.</summary>
    /// <param name="node">The node.</param>
    /// <returns>Its operands (empty for a constant or term).</returns>
    public static List<Expression> Children(Expression node)
    {
        List<Expression> children = [];
        MapChildren(
            node,
            child =>
            {
                children.Add(child);
                return child;
            }
        );
        return children;
    }

    /// <summary>
    /// The number of nodes in <paramref name="root"/> counted as a tree: a sub-expression shared in memory but appearing
    /// twice in the printed rule counts twice, because the printed (recompilable) size is what the node limit measures.
    /// </summary>
    /// <param name="root">The tree to measure.</param>
    /// <returns>The node count.</returns>
    public static long Size(Expression root)
    {
        return Size(root, [with(ReferenceEqualityComparer.Instance)]);
    }

    /// <summary>
    /// The depth of <paramref name="root"/>: the number of nodes on its longest root-to-leaf path, so a lone term or
    /// constant has depth 1. This is the measure <c>CompilerOptions.MaxDepth</c> limits.
    /// </summary>
    /// <param name="root">The tree to measure.</param>
    /// <returns>The depth.</returns>
    public static int Depth(Expression root)
    {
        return Depth(root, [with(ReferenceEqualityComparer.Instance)]);
    }

    private static int Depth(Expression node, Dictionary<Expression, int> memo)
    {
        if (memo.TryGetValue(node, out int known))
        {
            return known;
        }

        int deepestChild = 0;
        MapChildren(
            node,
            child =>
            {
                deepestChild = Math.Max(deepestChild, Depth(child, memo));
                return child;
            }
        );
        memo[node] = deepestChild + 1;
        return deepestChild + 1;
    }

    private static long Size(Expression node, Dictionary<Expression, long> memo)
    {
        if (memo.TryGetValue(node, out long known))
        {
            return known;
        }

        // MapChildren is reused as a child enumerator so a new node type only needs one traversal to be added.
        long total = 1;
        MapChildren(
            node,
            child =>
            {
                total += Size(child, memo);
                return child;
            }
        );
        memo[node] = total;
        return total;
    }

    /// <summary>
    /// Builds a node of the same operator as <paramref name="original"/> over <paramref name="operands"/>, keeping
    /// the operator's own parameters (comparison, bounds, inspection kind). This switch is deliberately closed: a new
    /// operator without a case fails loudly instead of silently keeping its old children.
    /// </summary>
    private static Expression Rebuild(Expression original, Expression[] operands)
    {
        return original switch
        {
            NotExpression => new NotExpression(operands[0]),
            AndExpression => new AndExpression(new EquatableArray<Expression>(operands)),
            OrExpression => new OrExpression(new EquatableArray<Expression>(operands)),
            XorExpression => new XorExpression(operands[0], operands[1]),
            EquivalentExpression => new EquivalentExpression(operands[0], operands[1]),
            ImpliesExpression => new ImpliesExpression(operands[0], operands[1]),
            NandExpression => new NandExpression(operands[0], operands[1]),
            NorExpression => new NorExpression(operands[0], operands[1]),
            ParityExpression => new ParityExpression(new EquatableArray<Expression>(operands)),
            AnyExpression => new AnyExpression(new EquatableArray<Expression>(operands)),
            AllExpression => new AllExpression(new EquatableArray<Expression>(operands)),
            NoneExpression => new NoneExpression(new EquatableArray<Expression>(operands)),
            ExactlyOneExpression => new ExactlyOneExpression(new EquatableArray<Expression>(operands)),
            CoalesceExpression => new CoalesceExpression(new EquatableArray<Expression>(operands)),
            ThresholdExpression t => new ThresholdExpression(t.Comparison, t.K, new EquatableArray<Expression>(operands)),
            BetweenExpression b => new BetweenExpression(b.Min, b.Max, new EquatableArray<Expression>(operands)),
            InspectionExpression s => new InspectionExpression(s.Kind, operands[0]),
            IfExpression => new IfExpression(operands[0], operands[1], operands[2]),
            _ => throw new InvalidOperationException($"Unhandled expression type '{original.GetType().Name}'."),
        };
    }
}
