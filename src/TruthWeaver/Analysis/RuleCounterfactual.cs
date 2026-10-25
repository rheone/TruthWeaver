namespace TruthWeaver.Analysis;

using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Parsing;

/// <summary>
/// Answers "what would make this rule <c>True</c>?" for the current facts: the smallest sets of term changes that give a
/// compiled rule a target value. Use the result for a denied decision as the "what to fix" list. The search uses the
/// same dual-rail BDD as <see cref="RuleEquivalence"/>, so it is exact (not sampled) and calls no predicate.
/// </summary>
/// <remarks>
/// <para>
/// A change sets one term to <c>True</c>, <c>False</c> or <c>Unknown</c>. Every term that a change set does not name
/// keeps its fact. A term without a fact (not in the facts, or not evaluated by the decision) can have any value, so a
/// change set that does not name it gives the target value for each of its three values.
/// </para>
/// <para>
/// Each change set is minimal: no smaller part of it gives the target value. The sets come fewest changes first, at most
/// <see cref="CounterfactualOptions.MaxChangeSets"/> of them. Terms are opaque and independent, as for
/// <see cref="RuleEquivalence"/>: the search cannot know that two different predicates are related, so a change set can
/// name values that cannot occur together in the application.
/// </para>
/// </remarks>
public static class RuleCounterfactual
{
    /// <summary>The default for <see cref="CounterfactualOptions.MaxChangeSets"/>.</summary>
    public const int DefaultMaxChangeSets = 10;

    private static readonly CounterfactualOptions Defaults = new();

    /// <summary>Finds the smallest sets of term changes that give <paramref name="rule"/> the target value from the given facts.</summary>
    /// <typeparam name="TContext">The application context type the rule was compiled for.</typeparam>
    /// <param name="rule">The rule.</param>
    /// <param name="facts">
    /// The current value of each term, keyed by the printed term (such as <c>hasRole(role: "admin")</c>). A term that is not
    /// in the dictionary has no fact and can have any value.
    /// </param>
    /// <param name="options">The search settings. <see langword="null"/> searches for <c>True</c> with the defaults.</param>
    /// <returns>
    /// The change sets, <see cref="CounterfactualOutcome.NoChangeNeeded"/>, <see cref="CounterfactualOutcome.NonePossible"/>,
    /// or <see cref="CounterfactualOutcome.Undecided"/> when the rule has more distinct terms than the term cap, or when
    /// building its shared decision diagram would exceed the search's internal node budget (ticket 24).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> or <paramref name="facts"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A key of <paramref name="facts"/> or a fixed term is not a term of the rule.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The target is not a defined value, or the change set cap is below 1.</exception>
    public static CounterfactualResult FindChanges<TContext>(
        CompiledRule<TContext> rule,
        IReadOnlyDictionary<string, TruthValue> facts,
        CounterfactualOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(facts);

        Dictionary<string, TermIdentity> terms = TermsByText(rule.Root);
        Dictionary<TermIdentity, TruthValue> known = [];
        foreach ((string text, TruthValue value) in facts)
        {
            known[Resolve(terms, text, nameof(facts))] = value;
        }

        return Search(rule.Root, terms, known, options ?? Defaults);
    }

    /// <summary>
    /// Finds the smallest sets of term changes that give <paramref name="rule"/> the target value from the term values in
    /// a decision of that rule. A term the decision did not evaluate (skipped by short-circuiting) has no fact and can have
    /// any value; a term that faulted is <c>Unknown</c>, as the decision recorded it.
    /// </summary>
    /// <typeparam name="TContext">The application context type the rule was compiled for.</typeparam>
    /// <param name="rule">The rule that made the decision.</param>
    /// <param name="decision">A decision of <paramref name="rule"/>, with its trace tree.</param>
    /// <param name="options">The search settings. <see langword="null"/> searches for <c>True</c> with the defaults.</param>
    /// <returns>
    /// The change sets, <see cref="CounterfactualOutcome.NoChangeNeeded"/>, <see cref="CounterfactualOutcome.NonePossible"/>,
    /// or <see cref="CounterfactualOutcome.Undecided"/> when the rule has more distinct terms than the term cap, or when
    /// building its shared decision diagram would exceed the search's internal node budget (ticket 24).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> or <paramref name="decision"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// The decision has no trace tree, its trace tree does not match the rule, or a fixed term is not a term of the rule.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The target is not a defined value, or the change set cap is below 1.</exception>
    public static CounterfactualResult FindChanges<TContext>(
        CompiledRule<TContext> rule,
        Decision decision,
        CounterfactualOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(decision);
        TraceNode tree =
            decision.TraceTree
            ?? throw new ArgumentException(
                "The decision has no trace tree, so its term values are not known.",
                nameof(decision)
            );

        Dictionary<TermIdentity, TruthValue> known = [];
        if (ReadTermValues(rule.Root, tree, TreePath.Root, known) is { } mismatch)
        {
            throw new ArgumentException(
                $"The trace tree of the decision does not match the rule at '{mismatch}'. Use a decision of the same rule.",
                nameof(decision)
            );
        }

        return Search(rule.Root, TermsByText(rule.Root), known, options ?? Defaults);
    }

    private static CounterfactualResult Search(
        Expression root,
        Dictionary<string, TermIdentity> terms,
        Dictionary<TermIdentity, TruthValue> known,
        CounterfactualOptions options
    )
    {
        if (!Enum.IsDefined(options.Target))
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Target, "The target is not a defined TruthValue.");
        }

        if (options.MaxChangeSets < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.MaxChangeSets,
                "The change set cap must be at least 1."
            );
        }

        HashSet<TermIdentity> fixedTerms =
        [
            .. (options.FixedTerms ?? []).Select(text => Resolve(terms, text, nameof(options))),
        ];

        int cap = options.MaxAnalysisTerms ?? CompilerOptions.Default.MaxAnalysisTerms;
        if (terms.Count > cap)
        {
            return new CounterfactualResult(
                CounterfactualOutcome.Undecided,
                [],
                false,
                $"The rule has {terms.Count} distinct terms, which exceeds the configured cap of {cap}."
            );
        }

        try
        {
            return ChangeSetSearch.Run(root, known, fixedTerms, options.Target, options.MaxChangeSets);
        }
        catch (BddNodeBudgetExceededException exception)
        {
            // Within the term cap, the shared BDD for this term ordering and rule shape still grew past the search's
            // internal node budget (ticket 24): report the same way the term-count cap does, rather than hanging or
            // allocating unboundedly.
            return new CounterfactualResult(CounterfactualOutcome.Undecided, [], false, exception.Message);
        }
    }

    /// <summary>Maps the printed form of each distinct term of the rule to its identity.</summary>
    private static Dictionary<string, TermIdentity> TermsByText(Expression root)
    {
        Dictionary<string, TermIdentity> terms = [];
        foreach (TermIdentity term in Analyzer.DistinctTerms(root))
        {
            terms[term.ToString()] = term;
        }

        return terms;
    }

    private static TermIdentity Resolve(Dictionary<string, TermIdentity> terms, string text, string parameter)
    {
        // An unknown name is refused: a misspelled fixed term would otherwise let the search change the real one.
        return terms.TryGetValue(text, out TermIdentity? term)
            ? term
            : throw new ArgumentException($"'{text}' is not a term of the rule.", parameter);
    }

    /// <summary>Reads the value of each evaluated term from the trace tree, which must have the shape of the rule.</summary>
    /// <returns>The tree path of the first node where the trace tree does not match the rule, or <see langword="null"/>.</returns>
    private static string? ReadTermValues(
        Expression node,
        TraceNode trace,
        string path,
        Dictionary<TermIdentity, TruthValue> values
    )
    {
        // A term with variable arguments shows their resolved values in its trace text, so only its name is compared.
        bool matches = node is TermExpression { Identity.Variables.Count: > 0 } term
            ? trace.Text.StartsWith(term.Identity.PredicateName + "(", StringComparison.Ordinal)
            : trace.Text == TraceText.Of(node);
        if (!matches)
        {
            return path;
        }

        if (trace.NotEvaluated)
        {
            return null;
        }

        IReadOnlyList<Expression> operands = node is TermExpression or ConstantExpression
            ? []
            : ExpressionShape.Of(node).Operands;
        if (trace.Result is not { } result || trace.Children.Count != operands.Count)
        {
            return path;
        }

        if (node is TermExpression evaluated)
        {
            // The evaluator runs a term once per evaluation, so every evaluated occurrence holds the same value.
            values[evaluated.Identity] = result;
            return null;
        }

        string operandsPath = TreePath.Property(path, "operands");
        for (int i = 0; i < operands.Count; i++)
        {
            if (ReadTermValues(operands[i], trace.Children[i], TreePath.Index(operandsPath, i), values) is { } mismatch)
            {
                return mismatch;
            }
        }

        return null;
    }
}
