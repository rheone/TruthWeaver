namespace TruthWeaver.Evaluation;

using TruthWeaver.Ast;

/// <summary>The text that a trace entry, a trace tree node and a coverage node show for one node of a rule.</summary>
internal static class TraceText
{
    /// <summary>Gets the text a trace entry and a trace tree node show for <paramref name="node"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The constant spelling, the term identity text or the operator label.</returns>
    public static string Of(Expression node)
    {
        if (node is ConstantExpression c)
        {
            return TruthValueText.Canonical(c.Value);
        }

        if (node is TermExpression t)
        {
            return t.Identity.ToString();
        }

        NodeShape shape = ExpressionShape.Of(node);

        // Labels come from the operator table; the completeness test guarantees every operator has one, so the
        // op-name fallback is only reachable for a node that skipped the table.
        return OperatorDefinitions.TryGet(shape.OpName, out OperatorDefinition? definition)
            ? definition.Label(shape)
            : shape.OpName;
    }
}
