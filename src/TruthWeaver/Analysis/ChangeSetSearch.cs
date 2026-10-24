namespace TruthWeaver.Analysis;

using TruthWeaver.Ast;

/// <summary>
/// Finds the minimal sets of term changes that give a tree a target value, on the analyzer's dual-rail BDD of "the tree
/// has the target value". Each term is a level of the diagram with up to four ways down: keep the fact (no cost) or set
/// one of the other values (one change). A term without a fact is kept only when the target holds for every value of
/// it, so a change set never depends on a value that nobody read. The cheapest way from each node to the true terminal
/// is computed once, and a best-first walk with that exact cost as its estimate then gives the change sets in order of
/// size. A path that contains a set already found is dropped, so every set returned is minimal.
/// </summary>
internal sealed class ChangeSetSearch
{
    /// <summary>The most search states the walk takes from its queue before it stops and reports an incomplete result.</summary>
    internal const int WorkLimit = 100_000;

    private const int Unreachable = int.MaxValue;

    private static readonly TruthValue[] Values = [TruthValue.True, TruthValue.False, TruthValue.Unknown];

    private readonly BddManager bdd;
    private readonly TruthValue?[] facts;
    private readonly bool[] isFixed;
    private readonly Dictionary<int, Option[]> waysDown = [];
    private readonly Dictionary<int, int> minCost = [];

    private ChangeSetSearch(BddManager bdd, TruthValue?[] facts, bool[] isFixed)
    {
        this.bdd = bdd;
        this.facts = facts;
        this.isFixed = isFixed;
    }

    /// <summary>Runs the search.</summary>
    /// <param name="root">The tree.</param>
    /// <param name="facts">The value of each term that has one. A term that is missing has no fact.</param>
    /// <param name="fixedTerms">The terms the search must keep.</param>
    /// <param name="target">The value the tree must take.</param>
    /// <param name="maxChangeSets">The most change sets to return.</param>
    /// <returns>The result, never <see cref="CounterfactualOutcome.Undecided"/>.</returns>
    public static CounterfactualResult Run(
        Expression root,
        IReadOnlyDictionary<TermIdentity, TruthValue> facts,
        IReadOnlySet<TermIdentity> fixedTerms,
        TruthValue target,
        int maxChangeSets
    )
    {
        (BddManager bdd, int start, Dictionary<TermIdentity, int> variableIndex) = Analyzer.TargetFunction(root, target);
        TermIdentity[] terms = [.. variableIndex.OrderBy(pair => pair.Value).Select(pair => pair.Key)];
        ChangeSetSearch search = new(
            bdd,
            [.. terms.Select(term => facts.TryGetValue(term, out TruthValue value) ? value : (TruthValue?)null)],
            [.. terms.Select(fixedTerms.Contains)]
        );

        return search.MinCost(start) switch
        {
            Unreachable => new CounterfactualResult(CounterfactualOutcome.NonePossible, [], true, null),
            0 => new CounterfactualResult(CounterfactualOutcome.NoChangeNeeded, [], true, null),
            _ => search.Walk(start, terms, maxChangeSets),
        };
    }

    /// <summary>Tells whether the changes on a path include every change of <paramref name="set"/>.</summary>
    private static bool Contains(Step? changes, Dictionary<int, TruthValue> set)
    {
        int matched = 0;
        for (Step? step = changes; step is not null; step = step.Previous)
        {
            if (set.TryGetValue(step.Term, out TruthValue value) && value == step.Value)
            {
                matched++;
            }
        }

        return matched == set.Count;
    }

    private static Dictionary<int, TruthValue> ToDictionary(Step? changes)
    {
        Dictionary<int, TruthValue> set = [];
        for (Step? step = changes; step is not null; step = step.Previous)
        {
            set[step.Term] = step.Value;
        }

        return set;
    }

    /// <summary>Walks the diagram best first and collects the minimal change sets in order of size.</summary>
    private CounterfactualResult Walk(int start, TermIdentity[] terms, int maxChangeSets)
    {
        PriorityQueue<State, (int Bound, int Order)> queue = new();
        queue.Enqueue(new State(start, null, 0), (this.MinCost(start), 0));
        List<Dictionary<int, TruthValue>> found = [];
        int order = 1;
        int work = 0;
        bool complete = true;

        while (queue.TryDequeue(out State state, out _))
        {
            if (++work > WorkLimit)
            {
                complete = false;
                break;
            }

            // A path that already holds a found set can only end in a larger set, which is not minimal.
            if (found.Exists(set => Contains(state.Changes, set)))
            {
                continue;
            }

            if (state.Node == BddManager.True)
            {
                found.Add(ToDictionary(state.Changes));

                // One set beyond the cap shows that the list is cut short; it is not returned.
                if (found.Count > maxChangeSets)
                {
                    complete = false;
                    break;
                }

                continue;
            }

            foreach (Option option in this.WaysDown(state.Node))
            {
                int remaining = this.MinCost(option.Child);
                if (remaining == Unreachable)
                {
                    continue;
                }

                (Step? changes, int cost) = option.Value is { } value
                    ? (new Step(option.Term, value, state.Changes), state.Cost + 1)
                    : (state.Changes, state.Cost);
                queue.Enqueue(new State(option.Child, changes, cost), (cost + remaining, order++));
            }
        }

        if (found.Count == 0)
        {
            found.Add(this.CheapestSet(start));
        }

        ChangeSet[] changeSets =
        [
            .. found
                .Take(maxChangeSets)
                .Select(set => new ChangeSet(
                    new EquatableArray<TermChange>(
                        set.OrderBy(pair => pair.Key)
                            .Select(pair => new TermChange(terms[pair.Key].ToString(), this.facts[pair.Key], pair.Value))
                    )
                )),
        ];
        return new CounterfactualResult(CounterfactualOutcome.Found, new EquatableArray<ChangeSet>(changeSets), complete, null);
    }

    /// <summary>The fewest changes that lead from <paramref name="node"/> to the true terminal, or <see cref="Unreachable"/>.</summary>
    private int MinCost(int node)
    {
        if (node <= BddManager.True)
        {
            return node == BddManager.True ? 0 : Unreachable;
        }

        if (this.minCost.TryGetValue(node, out int cached))
        {
            return cached;
        }

        int best = Unreachable;
        foreach (Option option in this.WaysDown(node))
        {
            int rest = this.MinCost(option.Child);
            if (rest != Unreachable)
            {
                best = Math.Min(best, rest + (option.Value is null ? 0 : 1));
            }
        }

        this.minCost[node] = best;
        return best;
    }

    /// <summary>
    /// The ways down from <paramref name="node"/> through the term it tests: keep the fact first, then each other value.
    /// A value is left out when every way on from it also works when the fact is kept, because a set with it is never
    /// minimal.
    /// </summary>
    private Option[] WaysDown(int node)
    {
        if (this.waysDown.TryGetValue(node, out Option[]? cached))
        {
            return cached;
        }

        int term = Analyzer.TermOf(this.bdd, node);
        TruthValue? fact = this.facts[term];
        int keep = fact is { } known ? this.Cofactor(node, term, known) : this.CofactorOverEveryValue(node, term);

        List<Option> result = [new Option(term, null, keep)];
        if (!this.isFixed[term])
        {
            foreach (TruthValue value in Values)
            {
                // A value whose function only holds where keeping the fact also holds can only give larger sets.
                int child = this.Cofactor(node, term, value);
                if (value != fact && this.bdd.And(child, this.bdd.Not(keep)) != BddManager.False)
                {
                    result.Add(new Option(term, value, child));
                }
            }
        }

        Option[] built = [.. result];
        this.waysDown[node] = built;
        return built;
    }

    /// <summary>The function below <paramref name="node"/> once term <paramref name="term"/> has <paramref name="value"/>.</summary>
    private int Cofactor(int node, int term, TruthValue value)
    {
        return Analyzer.Cofactor(this.bdd, node, term, value);
    }

    /// <summary>The fact at <paramref name="term"/> is unknown: the intersection of its cofactor under every value.</summary>
    private int CofactorOverEveryValue(int node, int term)
    {
        return this.bdd.And(
            this.bdd.And(this.Cofactor(node, term, TruthValue.True), this.Cofactor(node, term, TruthValue.False)),
            this.Cofactor(node, term, TruthValue.Unknown)
        );
    }

    /// <summary>
    /// Follows one cheapest way from <paramref name="start"/> to the true terminal. A set of the fewest changes is always
    /// minimal, so this gives one answer when the walk stops at its work limit before it finds any.
    /// </summary>
    private Dictionary<int, TruthValue> CheapestSet(int start)
    {
        Dictionary<int, TruthValue> set = [];
        int node = start;
        while (node != BddManager.True)
        {
            int cost = this.MinCost(node);
            Option next = this.WaysDown(node)
                .First(option =>
                    this.MinCost(option.Child) != Unreachable
                    && this.MinCost(option.Child) + (option.Value is null ? 0 : 1) == cost
                );
            if (next.Value is { } value)
            {
                set[next.Term] = value;
            }

            node = next.Child;
        }

        return set;
    }

    /// <summary>A partial path: the node reached, its changes and their number.</summary>
    /// <param name="Node">The node reached.</param>
    /// <param name="Changes">The last change on the path.</param>
    /// <param name="Cost">The number of changes.</param>
    private readonly record struct State(int Node, Step? Changes, int Cost);

    /// <summary>One way down through a term: keep its fact (<paramref name="Value"/> is <see langword="null"/>) or set a value.</summary>
    /// <param name="Term">The term index.</param>
    /// <param name="Value">The value set, or <see langword="null"/> to keep the fact.</param>
    /// <param name="Child">The function below the term.</param>
    private sealed record Option(int Term, TruthValue? Value, int Child);

    /// <summary>One change on a path, linked to the changes before it so that paths share their common part.</summary>
    /// <param name="Term">The term index.</param>
    /// <param name="Value">The value set.</param>
    /// <param name="Previous">The change before this one on the path.</param>
    private sealed record Step(int Term, TruthValue Value, Step? Previous);
}
