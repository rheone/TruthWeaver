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

        if (node is QuantifierExpression q)
        {
            // Must match the description the evaluator and the collection binding build for this node's trace
            // entries exactly (Evaluator.EvalQuantifierAsync, CollectionBinding.EvaluateQuantifierAsync), since
            // RuleCoverageAccumulator compares this text against the trace text recorded at evaluation time.
            string label = QuantifierText.Label(q.Kind);
            return $"{label}({q.Variable} IN {q.CollectionName})";
        }

        return LabelOf(ExpressionShape.Of(node));
    }

    /// <summary>
    /// Gets the display label an operator node of <paramref name="shape"/> shows. The evaluator calls this for an
    /// evaluated node and this type's own <see cref="Of"/> calls it for a skipped node, so the two paths read the
    /// same table and a label change in it reaches both identically.
    /// </summary>
    /// <param name="shape">The node's structural shape.</param>
    /// <returns>The operator's display label, with its threshold and bound filled in.</returns>
    public static string LabelOf(NodeShape shape)
    {
        // Labels come from the operator table; the completeness test guarantees every operator has one, so the
        // op-name fallback is only reachable for a node that skipped the table.
        return OperatorDefinitions.TryGet(shape.OpName, out OperatorDefinition? definition)
            ? definition.Label(shape)
            : shape.OpName;
    }
}
