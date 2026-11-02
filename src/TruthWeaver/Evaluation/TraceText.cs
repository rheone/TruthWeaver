namespace TruthWeaver.Evaluation;

using TruthWeaver.Ast;

/// <summary>The text that a trace entry, a trace tree node and a coverage node show for one node of a rule.</summary>
internal static class TraceText
{
    /// <summary>The text a trace tree node adds to a quantifier's description when the collection selector returned <see langword="null"/>.</summary>
    public const string NoCollectionSuffix = " [no collection]";

    /// <summary>
    /// Tests whether <paramref name="trace"/> is the trace tree node recorded for <paramref name="node"/>, by its text. A
    /// quantifier over a null collection records its description plus <see cref="NoCollectionSuffix"/>, an
    /// <see cref="TruthValue.Unknown"/> result and no children; the suffix on any other trace node does not match.
    /// </summary>
    /// <param name="node">The node of the rule.</param>
    /// <param name="trace">The trace tree node paired with it.</param>
    /// <returns><see langword="true"/> when the text of the trace node belongs to the node.</returns>
    public static bool Matches(Expression node, TraceNode trace)
    {
        string expected = Of(node);
        return trace.Text == expected
            || (
                node is QuantifierExpression
                && trace.Text == expected + NoCollectionSuffix
                && trace.Result == TruthValue.Unknown
                && trace.Children.Count == 0
            );
    }

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
    /// Adds the library locator of a node to the text of its flat trace entry, as in
    /// <c>a (in library rule 'adult' at line 1, column 1)</c>. The trace tree keeps the plain text, which coverage and the
    /// printers compare with the rule's own nodes.
    /// </summary>
    /// <param name="description">The text of the node.</param>
    /// <param name="libraryLocator">The locator of the node, or <see langword="null"/> when it is not from a library rule.</param>
    /// <returns>The text, with the locator when there is one.</returns>
    public static string WithLibraryLocator(string description, string? libraryLocator)
    {
        return libraryLocator is null ? description : $"{description} (in {libraryLocator})";
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
