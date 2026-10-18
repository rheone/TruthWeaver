namespace TruthWeaver.Evaluation;

/// <summary>
/// The coverage of one rule over a set of decisions: for each term and each node, how often it was evaluated, its
/// <see cref="TruthValue.True"/>, <see cref="TruthValue.False"/> and <see cref="TruthValue.Unknown"/> counts, its faults
/// and how often it decided the result. Use it on decisions recorded in production to find dead branches (a term or
/// branch that never decides) and unreliable data (a term that is always <see cref="TruthValue.Unknown"/> or faults).
/// </summary>
/// <remarks>
/// The coverage reads the trace tree of each <see cref="Decision"/> and never calls a predicate. A node decides a
/// decision when a different value at that node, with every other evaluated node kept and every skipped node read as
/// <see cref="TruthValue.Unknown"/>, changes the result of the rule. In <see cref="EvaluationMode.ShortCircuit"/> mode a
/// skipped operand is not evaluated, so it cannot decide; <see cref="EvaluationMode.Exhaustive"/> gives every reachable
/// term a value in every decision.
/// </remarks>
public sealed class RuleCoverage
{
    internal RuleCoverage(
        string rule,
        int trueCount,
        int falseCount,
        int unknownCount,
        EquatableArray<TermCoverage> terms,
        EquatableArray<NodeCoverage> nodes
    )
    {
        this.Rule = rule;
        this.TrueCount = trueCount;
        this.FalseCount = falseCount;
        this.UnknownCount = unknownCount;
        this.Terms = terms;
        this.Nodes = nodes;
    }

    /// <summary>Gets the canonical text of the rule.</summary>
    public string Rule { get; }

    /// <summary>Gets the number of decisions read.</summary>
    public int DecisionCount => this.TrueCount + this.FalseCount + this.UnknownCount;

    /// <summary>Gets the number of decisions with the result <see cref="TruthValue.True"/>.</summary>
    public int TrueCount { get; }

    /// <summary>Gets the number of decisions with the result <see cref="TruthValue.False"/>.</summary>
    public int FalseCount { get; }

    /// <summary>Gets the number of decisions with the result <see cref="TruthValue.Unknown"/>.</summary>
    public int UnknownCount { get; }

    /// <summary>Gets the coverage of each distinct term, in the order of the first occurrence of the term in the rule.</summary>
    public EquatableArray<TermCoverage> Terms { get; }

    /// <summary>Gets the coverage of each node, root first, in depth-first source order.</summary>
    public EquatableArray<NodeCoverage> Nodes { get; }

    /// <summary>Gets the coverage of the root node. Its <see cref="NodeCoverage.Operands"/> give the tree.</summary>
    public NodeCoverage Root => this.Nodes[0];

    /// <summary>Gets each operator with at least one operand that decided no decision, in the order of <see cref="Nodes"/>. <see cref="NodeCoverage.UndecidedOperands"/> lists those operands.</summary>
    public IReadOnlyList<NodeCoverage> OperatorsWithUndecidedOperands =>
        [.. this.Nodes.Where(node => node.UndecidedOperands.Count > 0)];

    /// <summary>Measures the coverage of <paramref name="rule"/> over decisions that it made. No predicate is called.</summary>
    /// <typeparam name="TContext">The context type of the rule.</typeparam>
    /// <param name="rule">The rule that made the decisions.</param>
    /// <param name="decisions">The decisions, each from <see cref="CompiledRule{TContext}.EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/> or <c>EvaluateManyAsync</c> of <paramref name="rule"/>.</param>
    /// <returns>The coverage.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/>, <paramref name="decisions"/> or one decision is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A decision has no trace tree, or its trace tree does not match <paramref name="rule"/>.</exception>
    public static RuleCoverage FromDecisions<TContext>(CompiledRule<TContext> rule, IEnumerable<Decision> decisions)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(decisions);
        RuleCoverageAccumulator accumulator = new(rule.Root);
        foreach (Decision decision in decisions)
        {
            accumulator.Add(decision);
        }

        return accumulator.Build(rule.CanonicalText);
    }

    /// <summary>
    /// Measures the coverage of <paramref name="rule"/> over a stream of decisions that it made, for example the output of
    /// <c>EvaluateManyAsync</c>. Each decision is read once and not kept. No predicate is called by the coverage itself.
    /// </summary>
    /// <typeparam name="TContext">The context type of the rule.</typeparam>
    /// <param name="rule">The rule that made the decisions.</param>
    /// <param name="decisions">The decisions of <paramref name="rule"/>.</param>
    /// <param name="cancellationToken">A token that stops the read.</param>
    /// <returns>The coverage.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/>, <paramref name="decisions"/> or one decision is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A decision has no trace tree, or its trace tree does not match <paramref name="rule"/>.</exception>
    /// <exception cref="OperationCanceledException">The token cancelled the read.</exception>
    public static async Task<RuleCoverage> FromDecisionsAsync<TContext>(
        CompiledRule<TContext> rule,
        IAsyncEnumerable<Decision> decisions,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(decisions);
        RuleCoverageAccumulator accumulator = new(rule.Root);
        await foreach (Decision decision in decisions.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            accumulator.Add(decision);
        }

        return accumulator.Build(rule.CanonicalText);
    }

    /// <summary>
    /// Prints the coverage as a Markdown report: the rule, the result counts, a table of the terms, the terms that never
    /// decided, were always <see cref="TruthValue.Unknown"/> or faulted, and a table of the branches that never decided,
    /// one row per branch with its operator.
    /// </summary>
    /// <returns>The Markdown text, with line feeds.</returns>
    public string PrintMarkdown()
    {
        using StringWriter writer = new();
        RuleCoveragePrinter.WriteMarkdown(this, writer);
        return writer.ToString();
    }

    /// <summary>Writes the text of <see cref="PrintMarkdown()"/> to <paramref name="writer"/>. The writer is not flushed or disposed.</summary>
    /// <param name="writer">The writer.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    public void PrintMarkdown(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        RuleCoveragePrinter.WriteMarkdown(this, writer);
    }

    /// <summary>
    /// Prints the coverage as indented JSON with the members <c>rule</c>, <c>decisions</c>, <c>outcomes</c>,
    /// <c>terms</c>, <c>nodes</c> (each with the paths of its operands) and <c>undecidedBranches</c> (one entry per
    /// operator, with the paths of its operands that never decided).
    /// </summary>
    /// <returns>The JSON text, with line feeds.</returns>
    public string PrintJson()
    {
        using StringWriter writer = new();
        RuleCoveragePrinter.WriteJson(this, writer);
        return writer.ToString();
    }

    /// <summary>Writes the text of <see cref="PrintJson()"/> to <paramref name="writer"/>. The writer is not flushed or disposed.</summary>
    /// <param name="writer">The writer.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    public void PrintJson(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        RuleCoveragePrinter.WriteJson(this, writer);
    }
}
