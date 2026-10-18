namespace TruthWeaver.Analysis;

using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;

/// <summary>
/// Analyzes a group of peer rules at once: the joint-outcome table of every pair of rules (the
/// <see cref="RuleOverlap.Compare{TContext}"/> question for each pair), the redundancy of each rule under the
/// "any grants" reading, where the set is satisfied when any of its rules is <c>True</c>, and the shadowing of each
/// rule when the list is read in order as "the first rule that applies wins". Every rule is built once on
/// one shared dual-rail BDD, the same model <see cref="RuleEquivalence"/>, <see cref="RuleImplication"/> and
/// <see cref="RuleOverlap"/> use, so the answers are exact (not sampled) and no truth table is enumerated.
/// </summary>
/// <remarks>
/// <para>
/// Rules are identified by their index in the input list. A missing pair of values, and a rule reported redundant, are
/// proofs for every real application context, provided the predicate-author contract holds (the same term answers the
/// same way within one evaluation). A reachable pair, and the example of a rule that is not redundant, are candidates
/// only: terms are opaque and independent, so an example can be impossible in the application (ADR-0009 decision 4).
/// No predicate runs, and the analysis does not look at evaluation order, short-circuiting or faults.
/// </para>
/// <para>
/// Shadowing reads an earlier rule that is <c>True</c> or <c>Unknown</c> as a block on every later rule, the same as
/// the <c>FirstApplicable</c> combining algorithm, which never skips an <c>Unknown</c> policy (ADR-0015 decision 5).
/// A rule is shadowed when the earlier rules block it wherever it is <c>True</c>. There is no reading parameter.
/// </para>
/// <para>
/// Three limits keep the cost bounded, and none of them throws. The rule cap stops the analysis before it starts
/// (<see cref="RuleSetAnalysisOutcome.TooManyRules"/>). The term cap, counted over the distinct terms of the whole set,
/// and an internal BDD node budget stop the set-level answers (<see cref="RuleSetAnalysisOutcome.Undecided"/>); the
/// pairwise answers are then computed pair by pair, each under the same term cap counted over the pair and its own
/// node budget.
/// </para>
/// </remarks>
public static class RuleSetAnalysis
{
    /// <summary>The default rule cap: the most rules <see cref="Analyze{TContext}(IReadOnlyList{CompiledRule{TContext}}, int?, int?)"/> analyzes.</summary>
    public const int DefaultMaxRules = 50;

    /// <summary>Analyzes the rules of <paramref name="rules"/>: every pair's joint outcomes, each rule's redundancy and shadowing, and one group of rules removable together.</summary>
    /// <typeparam name="TContext">The application context type every rule was compiled for.</typeparam>
    /// <param name="rules">The rules. A rule's index in this list is its identity in the result. The same rule can appear more than once.</param>
    /// <param name="maxAnalysisTerms">
    /// The most distinct terms the set-level answers decide, counted across every rule; each pair is also checked
    /// against it, counted across the two rules of the pair. <see langword="null"/> uses the
    /// <see cref="CompilerOptions.MaxAnalysisTerms"/> default.
    /// </param>
    /// <param name="maxRules">
    /// The most rules the analysis accepts. <see langword="null"/> uses <see cref="DefaultMaxRules"/>. The number of
    /// pairs grows with the square of the number of rules, so raise it with care.
    /// </param>
    /// <returns>
    /// The result, with outcome <see cref="RuleSetAnalysisOutcome.Decided"/>,
    /// <see cref="RuleSetAnalysisOutcome.Undecided"/> or <see cref="RuleSetAnalysisOutcome.TooManyRules"/> and a reason
    /// for the last two. It never throws for a normal rule.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="rules"/> is <see langword="null"/>, or contains a <see langword="null"/> rule.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRules"/> is less than 1.</exception>
    public static RuleSetAnalysisResult Analyze<TContext>(
        IReadOnlyList<CompiledRule<TContext>> rules,
        int? maxAnalysisTerms = null,
        int? maxRules = null
    )
    {
        return AnalyzeCore(rules, maxAnalysisTerms, maxRules, BddManager.DefaultMaxBddNodes);
    }

    /// <summary>Analyzes the rules of <paramref name="rules"/>: every pair's joint outcomes, each rule's redundancy and shadowing, and one group of rules removable together.</summary>
    /// <typeparam name="TContext">The application context type every rule was compiled for.</typeparam>
    /// <param name="rules">The rule set. A rule's index in the set is its identity in the result.</param>
    /// <param name="maxAnalysisTerms">
    /// The most distinct terms the set-level answers decide, counted across every rule; each pair is also checked
    /// against it, counted across the two rules of the pair. <see langword="null"/> uses the
    /// <see cref="CompilerOptions.MaxAnalysisTerms"/> default.
    /// </param>
    /// <param name="maxRules">The most rules the analysis accepts. <see langword="null"/> uses <see cref="DefaultMaxRules"/>.</param>
    /// <returns>The same result as the <see cref="IReadOnlyList{T}"/> overload gives for the rules of the set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rules"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRules"/> is less than 1.</exception>
    public static RuleSetAnalysisResult Analyze<TContext>(
        RuleSet<TContext> rules,
        int? maxAnalysisTerms = null,
        int? maxRules = null
    )
    {
        return AnalyzeCore(rules, maxAnalysisTerms, maxRules, BddManager.DefaultMaxBddNodes);
    }

    /// <summary>
    /// The body of both public overloads, with the BDD node budget as a parameter so a test can reach the budget path
    /// with a small set instead of a pathological one.
    /// </summary>
    internal static RuleSetAnalysisResult AnalyzeCore<TContext>(
        IReadOnlyList<CompiledRule<TContext>> rules,
        int? maxAnalysisTerms,
        int? maxRules,
        int maxBddNodes
    )
    {
        ArgumentNullException.ThrowIfNull(rules);
        for (int i = 0; i < rules.Count; i++)
        {
            if (rules[i] is null)
            {
                throw new ArgumentNullException(nameof(rules), $"The rule at index {i} is null.");
            }
        }

        int ruleCap = maxRules ?? DefaultMaxRules;
        if (ruleCap < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRules), maxRules, "The rule cap must be at least 1.");
        }

        if (rules.Count > ruleCap)
        {
            return new RuleSetAnalysisResult(
                RuleSetAnalysisOutcome.TooManyRules,
                [],
                [],
                [],
                [],
                $"The set has {rules.Count} rules, which exceeds the configured cap of {ruleCap}."
            );
        }

        int termCap = maxAnalysisTerms ?? CompilerOptions.Default.MaxAnalysisTerms;
        List<HashSet<string>> printedTerms = [.. rules.Select(rule => PrintedTerms(rule.Root))];
        HashSet<TermIdentity> setTerms = [];
        foreach (CompiledRule<TContext> rule in rules)
        {
            setTerms.UnionWith(Analyzer.DistinctTerms(rule.Root));
        }

        if (setTerms.Count > termCap)
        {
            return PairByPair(
                rules,
                maxAnalysisTerms,
                $"The rules have {setTerms.Count} distinct terms between them, which exceeds the configured cap of {termCap}."
            );
        }

        try
        {
            (
                IReadOnlyList<(
                    int First,
                    int Second,
                    IReadOnlyList<(
                        TruthValue FirstValue,
                        TruthValue SecondValue,
                        IReadOnlyDictionary<TermIdentity, TruthValue> Example
                    )> Cells
                )> setPairs,
                IReadOnlyList<(
                    int Index,
                    bool Redundant,
                    IReadOnlyDictionary<TermIdentity, TruthValue>? UniqueContribution
                )> setRedundancy,
                IReadOnlyList<int> removable,
                IReadOnlyList<(int Index, bool Shadowed, IReadOnlyDictionary<TermIdentity, TruthValue>? Reached)> setShadowing
            ) = Analyzer.AnalyzeSet([.. rules.Select(rule => rule.Root)], maxBddNodes);

            List<RuleSetPair> pairs =
            [
                .. setPairs.Select(pair => new RuleSetPair(
                    pair.First,
                    pair.Second,
                    new RuleOverlapResult(
                        RuleOverlapOutcome.Decided,
                        [
                            .. pair.Cells.Select(cell => new JointOutcome(
                                cell.FirstValue,
                                cell.SecondValue,
                                PairExample(cell.Example, printedTerms[pair.First], printedTerms[pair.Second])
                            )),
                        ],
                        null
                    )
                )),
            ];
            List<RuleRedundancy> redundancy =
            [
                .. setRedundancy.Select(rule => new RuleRedundancy(
                    rule.Index,
                    rule.Redundant,
                    rule.UniqueContribution?.ToDictionary(p => p.Key.ToString(), p => p.Value)
                )),
            ];
            List<RuleShadowing> shadowing =
            [
                .. setShadowing.Select(rule => new RuleShadowing(
                    rule.Index,
                    rule.Shadowed,
                    rule.Reached?.ToDictionary(p => p.Key.ToString(), p => p.Value)
                )),
            ];
            return new RuleSetAnalysisResult(RuleSetAnalysisOutcome.Decided, pairs, redundancy, removable, shadowing, null);
        }
        catch (BddNodeBudgetExceededException exception)
        {
            return PairByPair(rules, maxAnalysisTerms, exception.Message);
        }
    }

    /// <summary>
    /// The fallback when the set-level answers are out of reach: each pair on its own BDD through
    /// <see cref="RuleOverlap.Compare{TContext}"/>, which applies the term cap to the pair and catches its own node
    /// budget (design note section 5: the pairwise answers still run pair by pair).
    /// </summary>
    private static RuleSetAnalysisResult PairByPair<TContext>(
        IReadOnlyList<CompiledRule<TContext>> rules,
        int? maxAnalysisTerms,
        string reason
    )
    {
        List<RuleSetPair> pairs = [];
        for (int i = 0; i < rules.Count; i++)
        {
            for (int j = i + 1; j < rules.Count; j++)
            {
                pairs.Add(new RuleSetPair(i, j, RuleOverlap.Compare(rules[i], rules[j], maxAnalysisTerms)));
            }
        }

        return new RuleSetAnalysisResult(RuleSetAnalysisOutcome.Undecided, pairs, [], [], [], reason);
    }

    /// <summary>The printed form of each distinct term of a tree, the key every example uses.</summary>
    private static HashSet<string> PrintedTerms(Expression root)
    {
        return [.. Analyzer.DistinctTerms(root).Select(term => term.ToString())];
    }

    /// <summary>
    /// Narrows a witness from the shared BDD of the whole set to the terms of one pair, so a pair's example matches
    /// what <see cref="RuleOverlap.Compare{TContext}"/> reports. The cell depends only on those terms, so dropping the
    /// rest keeps the example valid.
    /// </summary>
    private static Dictionary<string, TruthValue> PairExample(
        IReadOnlyDictionary<TermIdentity, TruthValue> example,
        HashSet<string> firstTerms,
        HashSet<string> secondTerms
    )
    {
        Dictionary<string, TruthValue> narrowed = [];
        foreach ((TermIdentity term, TruthValue value) in example)
        {
            string printed = term.ToString();
            if (firstTerms.Contains(printed) || secondTerms.Contains(printed))
            {
                narrowed[printed] = value;
            }
        }

        return narrowed;
    }
}
