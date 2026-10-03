namespace TruthWeaver.Ast;

using TruthWeaver.Abstractions;

/// <summary>
/// Looks up the <see cref="OperatorDescriptor"/> (label + description) for any operator node in a
/// compiled expression tree. Every operator in the closed set (ADR-0004) has one; a
/// <see cref="TermExpression"/> does not — a term's label/description come from its predicate's
/// registered <see cref="Abstractions.PredicateSchema"/> instead, since that's where they are
/// authored, not from the AST node itself.
/// </summary>
public static class OperatorInfo
{
    // Note: descriptors are static text today; enrich them with context only where a static value is not enough.

    /// <summary>Gets the label and description for an operator node.</summary>
    /// <param name="node">The expression node.</param>
    /// <returns>The operator's label and description.</returns>
    /// <exception cref="ArgumentException"><paramref name="node"/> is a <see cref="TermExpression"/>.</exception>
    public static OperatorDescriptor Describe(Expression node)
    {
        if (node is ConstantExpression c)
        {
            string label = TruthValueText.Canonical(c.Value);
            return new OperatorDescriptor(label, $"A fixed {label} value.");
        }

        if (node is TermExpression)
        {
            throw new ArgumentException(
                "A TermExpression has no operator descriptor — look up its label/description from the "
                    + "registered PredicateSchema via the term's predicate name instead.",
                nameof(node)
            );
        }

        NodeShape shape = ExpressionShape.Of(node);
        return OperatorDefinitions.TryGet(shape.OpName, out OperatorDefinition? definition)
            ? new OperatorDescriptor(definition.Label(shape), definition.Describe(shape))
            : new OperatorDescriptor(shape.OpName, ThresholdDescription(shape));
    }

    // Defensive: every node reaches here through ExpressionShape.Of, so a missing definition means an operator was added
    // to the shape seam without a table entry; fail loudly. An existing test pins this method and message via reflection.
    private static string ThresholdDescription(NodeShape shape)
    {
        throw new InvalidOperationException($"Unhandled threshold comparison '{shape.OpName}'.");
    }
}
