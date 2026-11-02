namespace TruthWeaver.Compilation;

using TruthWeaver.Ast;
using TruthWeaver.Parsing;

/// <summary>
/// Collects the library locator of each compiled node that a <c>rule("name")</c> reference expanded to, keyed by the
/// compiled node itself. It walks the expanded raw tree and the compiled tree side by side, as <see cref="NoteCollector"/>
/// does; the locators stay beside the compiled tree and never enter it.
/// </summary>
internal static class LibraryLocatorCollector
{
    /// <summary>Collects the library locators of <paramref name="source"/> onto the nodes of <paramref name="compiled"/>.</summary>
    /// <param name="source">The expanded raw tree.</param>
    /// <param name="compiled">The tree the compiler built from <paramref name="source"/>.</param>
    /// <returns>The locator text by compiled node, compared by reference; empty when no node is from a library rule.</returns>
    public static Dictionary<Expression, string> Collect(RuleNode source, Expression compiled)
    {
        Dictionary<Expression, string> locators = [with(ReferenceEqualityComparer.Instance)];
        Walk(source, compiled, locators);
        return locators;
    }

    /// <summary>
    /// Carries the library locators of a rule across a rewrite. A rewrite builds new nodes, so the locators cannot follow
    /// the node by reference. A node of the rewritten tree takes the locator of the node of the original tree that equals
    /// it in structure when exactly one locator belongs to every such node. A node that the original tree writes in two
    /// places with different locators, or with and without one, has two sources and takes none, as does a node that the
    /// rewrite creates with operands that no original node has.
    /// </summary>
    /// <param name="locators">The locators of the original tree, or <see langword="null"/> for none.</param>
    /// <param name="before">The original tree.</param>
    /// <param name="after">The rewritten tree.</param>
    /// <returns>The locator text by node of the rewritten tree, or <see langword="null"/> when no node has one.</returns>
    public static Dictionary<Expression, string>? Carry(
        IReadOnlyDictionary<Expression, string>? locators,
        Expression before,
        Expression after
    )
    {
        if (locators is null || locators.Count == 0)
        {
            return null;
        }

        // Records compare by structure, so this index finds the original nodes that a rewritten node equals. A null value
        // marks a shape with more than one source.
        Dictionary<Expression, string?> byShape = [];
        Index(before, locators, byShape);

        Dictionary<Expression, string> carried = [with(ReferenceEqualityComparer.Instance)];
        Apply(after, byShape, carried);
        return carried.Count == 0 ? null : carried;
    }

    private static void Index(
        Expression node,
        IReadOnlyDictionary<Expression, string> locators,
        Dictionary<Expression, string?> byShape
    )
    {
        string? locator = locators.GetValueOrDefault(node);
        if (!byShape.TryGetValue(node, out string? known))
        {
            byShape[node] = locator;
        }
        else if (!string.Equals(known, locator, StringComparison.Ordinal))
        {
            byShape[node] = null;
        }

        if (node is ConstantExpression or TermExpression)
        {
            return;
        }

        foreach (Expression operand in ExpressionShape.Of(node).Operands)
        {
            Index(operand, locators, byShape);
        }
    }

    private static void Apply(Expression node, Dictionary<Expression, string?> byShape, Dictionary<Expression, string> carried)
    {
        if (byShape.TryGetValue(node, out string? locator) && locator is not null)
        {
            carried[node] = locator;
        }

        if (node is ConstantExpression or TermExpression)
        {
            return;
        }

        foreach (Expression operand in ExpressionShape.Of(node).Operands)
        {
            Apply(operand, byShape, carried);
        }
    }

    private static void Walk(RuleNode source, Expression compiled, Dictionary<Expression, string> locators)
    {
        if (source.Library is { } locator)
        {
            locators[compiled] = locator.ToString();
        }

        if (compiled is ConstantExpression or TermExpression)
        {
            return;
        }

        IReadOnlyList<RuleNode> sourceOperands = RuleNode.OperandsOf(source);
        IReadOnlyList<Expression> compiledOperands = ExpressionShape.Of(compiled).Operands;

        // The counts always agree for a tree that compiled; the check keeps a future mismatch from misplacing a locator.
        if (sourceOperands.Count != compiledOperands.Count)
        {
            return;
        }

        for (int i = 0; i < sourceOperands.Count; i++)
        {
            Walk(sourceOperands[i], compiledOperands[i], locators);
        }
    }
}
