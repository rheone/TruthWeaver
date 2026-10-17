namespace TruthWeaver.Evaluation;

using TruthWeaver.Ast;
using TruthWeaver.Parsing;

/// <summary>
/// Collects the counts of a <see cref="RuleCoverage"/> one decision at a time. The rule tree is flattened once into
/// depth-first arrays (node, parent, operands, path); each decision is then paired with those arrays through its trace
/// tree, so no predicate runs and a decision is not kept after it is counted.
/// </summary>
internal sealed class RuleCoverageAccumulator
{
    private static readonly TruthValue[] Values = [TruthValue.True, TruthValue.False, TruthValue.Unknown];

    private readonly List<Expression> nodes = [];
    private readonly List<int> parents = [];
    private readonly List<int[]> operands = [];
    private readonly List<int> subtreeEnds = [];
    private readonly List<string> paths = [];
    private readonly List<string> texts = [];
    private readonly List<int> termOfNode = [];
    private readonly List<TermIdentity> terms = [];
    private readonly Dictionary<TermIdentity, int> termIndex = [];
    private readonly List<int>[] termNodes;

    // ADR-0010 decision 7: a quantifier's trace has one child per element, not one child per written operand, so
    // its body cannot be paired against the per-decision value/evaluated arrays below like every other node (one
    // slot per node per decision). True for every node under a quantifier's body (including a nested quantifier's
    // own body), at any depth; the per-decision loop in Add skips these, and PairElement folds each element's
    // contribution into nodeCounts directly, immediately, instead.
    private readonly List<bool> underQuantifierBody = [];

    // Counts per node and per term, indexed like the lists above: true, false, unknown, evaluated, not evaluated, decided, faults.
    private readonly int[,] nodeCounts;
    private readonly int[,] termCounts;
    private readonly int[] outcomes = new int[3];

    /// <summary>Initializes a new instance of the <see cref="RuleCoverageAccumulator"/> class.</summary>
    /// <param name="root">The root of the rule.</param>
    public RuleCoverageAccumulator(Expression root)
    {
        this.Flatten(root, -1, TreePath.Root, insideBody: false);
        this.termNodes = [.. this.terms.Select(_ => new List<int>())];
        for (int i = 0; i < this.nodes.Count; i++)
        {
            if (this.termOfNode[i] >= 0)
            {
                this.termNodes[this.termOfNode[i]].Add(i);
            }
        }

        this.nodeCounts = new int[this.nodes.Count, 6];
        this.termCounts = new int[this.terms.Count, 7];
    }

    private enum Count
    {
        True = 0,
        False = 1,
        Unknown = 2,
        Evaluated = 3,
        NotEvaluated = 4,
        Decided = 5,
        Faults = 6,
    }

    /// <summary>Counts one decision.</summary>
    /// <param name="decision">A decision of the rule.</param>
    /// <exception cref="ArgumentNullException"><paramref name="decision"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The decision has no trace tree, or its trace tree does not match the rule.</exception>
    public void Add(Decision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        TraceNode tree =
            decision.TraceTree
            ?? throw new ArgumentException("The decision has no trace tree, so its coverage is not known.", nameof(decision));

        int count = this.nodes.Count;
        TruthValue[] value = new TruthValue[count];
        bool[] evaluated = new bool[count];
        Array.Fill(value, TruthValue.Unknown);
        this.Pair(0, tree, value, evaluated);
        this.CheckValues(value, evaluated, decision.Result);

        this.outcomes[(int)ToCount(decision.Result)]++;
        for (int i = 0; i < count; i++)
        {
            if (this.underQuantifierBody[i])
            {
                // Folded per element by PairElement already, as Pair walked the trace above.
                continue;
            }

            if (!evaluated[i])
            {
                this.nodeCounts[i, (int)Count.NotEvaluated]++;
                continue;
            }

            this.nodeCounts[i, (int)Count.Evaluated]++;
            this.nodeCounts[i, (int)ToCount(value[i])]++;
            if (Values.Any(other => other != value[i] && this.ChangesResult(i, other, value)))
            {
                this.nodeCounts[i, (int)Count.Decided]++;
            }
        }

        for (int term = 0; term < this.terms.Count; term++)
        {
            this.CountTerm(term, value, evaluated);
        }

        foreach (Fault fault in decision.Faults)
        {
            if (this.termIndex.TryGetValue(fault.Term, out int term))
            {
                this.termCounts[term, (int)Count.Faults]++;
            }
        }
    }

    /// <summary>Builds the coverage from the decisions counted so far.</summary>
    /// <param name="ruleText">The canonical text of the rule.</param>
    /// <returns>The coverage.</returns>
    public RuleCoverage Build(string ruleText)
    {
        NodeCoverage[] built = new NodeCoverage[this.nodes.Count];

        // Children come after their parent in depth-first order, so building from the end has every operand ready.
        for (int i = this.nodes.Count - 1; i >= 0; i--)
        {
            built[i] = new NodeCoverage(
                this.paths[i],
                this.texts[i],
                this.nodeCounts[i, (int)Count.Evaluated],
                this.nodeCounts[i, (int)Count.NotEvaluated],
                this.nodeCounts[i, (int)Count.True],
                this.nodeCounts[i, (int)Count.False],
                this.nodeCounts[i, (int)Count.Unknown],
                this.nodeCounts[i, (int)Count.Decided],
                new EquatableArray<NodeCoverage>(this.operands[i].Select(operand => built[operand]))
            );
        }

        TermCoverage[] termCoverage =
        [
            .. this.terms.Select(
                (identity, term) =>
                    new TermCoverage(
                        identity.ToString(),
                        this.termCounts[term, (int)Count.Evaluated],
                        this.termCounts[term, (int)Count.True],
                        this.termCounts[term, (int)Count.False],
                        this.termCounts[term, (int)Count.Unknown],
                        this.termCounts[term, (int)Count.Faults],
                        this.termCounts[term, (int)Count.Decided]
                    )
            ),
        ];

        return new RuleCoverage(
            ruleText,
            this.outcomes[(int)Count.True],
            this.outcomes[(int)Count.False],
            this.outcomes[(int)Count.Unknown],
            new EquatableArray<TermCoverage>(termCoverage),
            new EquatableArray<NodeCoverage>(built)
        );
    }

    /// <summary>
    /// The value of an operator for the given operand values: the same Strong Kleene functions the evaluator applies,
    /// read from the operand values only, so a skipped operand (read as <see cref="TruthValue.Unknown"/>) gives the value
    /// the evaluator recorded.
    /// </summary>
    private static TruthValue Combine(Expression node, IReadOnlyList<TruthValue> values)
    {
        return node switch
        {
            NotExpression => K3Logic.Not(values[0]),
            AndExpression => values.Aggregate(TruthValue.True, K3Logic.And),
            OrExpression => values.Aggregate(TruthValue.False, K3Logic.Or),
            CoalesceExpression => values.FirstOrDefault(value => value != TruthValue.Unknown, TruthValue.Unknown),
            InspectionExpression inspection => K3Logic.Inspect(inspection.Kind, values[0]),
            IfExpression => values[0] switch
            {
                TruthValue.True => values[1],
                TruthValue.False => values[2],
                _ => values[1] == values[2] ? values[1] : TruthValue.Unknown,
            },
            XorExpression => K3Logic.Xor(values[0], values[1]),
            EquivalentExpression => K3Logic.Equivalent(values[0], values[1]),
            ImpliesExpression => K3Logic.Implies(values[0], values[1]),
            NandExpression => K3Logic.Nand(values[0], values[1]),
            NorExpression => K3Logic.Nor(values[0], values[1]),
            ParityExpression => K3Logic.Parity(values),
            AnyExpression => K3Logic.Threshold(ThresholdComparison.AtLeast, 1, values),
            AllExpression => K3Logic.Threshold(ThresholdComparison.AtLeast, values.Count, values),
            NoneExpression => K3Logic.Threshold(ThresholdComparison.AtMost, 0, values),
            ExactlyOneExpression => K3Logic.Threshold(ThresholdComparison.Exactly, 1, values),
            BetweenExpression between => K3Logic.And(
                K3Logic.Threshold(ThresholdComparison.AtLeast, between.Min, values),
                K3Logic.Threshold(ThresholdComparison.AtMost, between.Max, values)
            ),
            ThresholdExpression threshold => K3Logic.Threshold(threshold.Comparison, threshold.K, values),
            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'."),
        };
    }

    private static Count ToCount(TruthValue value)
    {
        return value switch
        {
            TruthValue.True => Count.True,
            TruthValue.False => Count.False,
            _ => Count.Unknown,
        };
    }

    private static ArgumentException Mismatch(string path)
    {
        return new ArgumentException(
            $"The trace tree of the decision does not match the rule at '{path}'. Measure coverage only over decisions of the same rule."
        );
    }

    /// <summary>Registers one term-like leaf (a predicate or a quantifier atom) and returns its term number.</summary>
    private int RegisterTerm(TermIdentity identity)
    {
        if (!this.termIndex.TryGetValue(identity, out int termNumber))
        {
            termNumber = this.terms.Count;
            this.terms.Add(identity);
            this.termIndex.Add(identity, termNumber);
        }

        return termNumber;
    }

    /// <summary>Flattens the compiled tree into the depth-first arrays this accumulator pairs a trace tree against.</summary>
    /// <param name="node">The node to flatten.</param>
    /// <param name="parent">The flattened index of <paramref name="node"/>'s parent, or -1 for the root.</param>
    /// <param name="path">The tree path to <paramref name="node"/>.</param>
    /// <param name="insideBody">
    /// Whether <paramref name="node"/> is under a quantifier's body (at any depth, including a nested
    /// quantifier's own body). Such a node's coverage is folded per element by <see cref="PairElement"/> instead
    /// of once per decision (ADR-0010 decision 7), and its terms are not listed in <see cref="RuleCoverage.Terms"/>
    /// (ADR-0010 decision 8: "the terms of a rule" are the outer terms and the quantifier atoms, not a body's).
    /// </param>
    private void Flatten(Expression node, int parent, string path, bool insideBody)
    {
        int index = this.nodes.Count;
        this.nodes.Add(node);
        this.parents.Add(parent);
        this.paths.Add(path);
        this.texts.Add(TraceText.Of(node));
        this.subtreeEnds.Add(index + 1);
        this.operands.Add([]);
        this.termOfNode.Add(-1);
        this.underQuantifierBody.Add(insideBody);

        if (node is TermExpression term)
        {
            if (!insideBody)
            {
                this.termOfNode[index] = this.RegisterTerm(term.Identity);
            }

            return;
        }

        if (node is ConstantExpression)
        {
            return;
        }

        bool isQuantifier = node is QuantifierExpression;
        if (node is QuantifierExpression quantifier)
        {
            // ADR-0010 decision 8: the quantifier is an opaque atom for "the terms of a rule", keyed by its own
            // canonical text (including the body) exactly as the analyzer keys it; the body's own terms, flattened
            // below with insideBody: true, are excluded from this list.
            this.termOfNode[index] = this.RegisterTerm(QuantifierAtom.IdentityOf(quantifier));
        }

        IReadOnlyList<Expression> children = ExpressionShape.Of(node).Operands;
        string operandsPath = TreePath.Property(path, "operands");
        int[] childIndexes = new int[children.Count];
        for (int i = 0; i < children.Count; i++)
        {
            childIndexes[i] = this.nodes.Count;
            this.Flatten(children[i], index, TreePath.Index(operandsPath, i), insideBody || isQuantifier);
        }

        this.operands[index] = childIndexes;
        this.subtreeEnds[index] = this.nodes.Count;
    }

    /// <summary>
    /// Reads the value of node <paramref name="index"/> and its operands from the trace tree. A skipped node and
    /// every node below it stay not evaluated. A quantifier (ADR-0010 decision 7) is the one exception: its own
    /// value is read here like any other node, but its trace has one child per element rather than one child per
    /// written operand, so each element is folded into coverage separately and immediately by
    /// <see cref="PairElement"/> instead of being paired positionally into <paramref name="value"/>/<paramref name="evaluated"/>.
    /// </summary>
    private void Pair(int index, TraceNode traceNode, TruthValue[] value, bool[] evaluated)
    {
        Expression node = this.nodes[index];

        // A term's trace text can carry resolved variable values, so only its predicate name is compared.
        bool textMatches = node is TermExpression term
            ? traceNode.Text.StartsWith(term.Identity.PredicateName, StringComparison.Ordinal)
            : traceNode.Text == this.texts[index];
        if (!textMatches)
        {
            throw Mismatch(this.paths[index]);
        }

        if (traceNode.NotEvaluated)
        {
            return;
        }

        if (traceNode.Result is not { } result)
        {
            throw Mismatch(this.paths[index]);
        }

        if (node is QuantifierExpression)
        {
            evaluated[index] = true;
            value[index] = result;
            int bodyIndex = this.operands[index][0];
            if (traceNode.Children.Any(elementTrace => elementTrace.Children.Count != 1))
            {
                throw Mismatch(this.paths[index]);
            }

            foreach (TraceNode bodyTrace in traceNode.Children.Select(elementTrace => elementTrace.Children[0]))
            {
                this.PairElement(bodyIndex, bodyTrace);
            }

            return;
        }

        int[] children = this.operands[index];
        if (traceNode.Children.Count != children.Length)
        {
            throw Mismatch(this.paths[index]);
        }

        evaluated[index] = true;
        value[index] = result;
        for (int i = 0; i < children.Length; i++)
        {
            this.Pair(children[i], traceNode.Children[i], value, evaluated);
        }
    }

    /// <summary>
    /// Folds one element's body evaluation (the single child of a quantifier's per-element trace node, ADR-0010
    /// decision 7) into <see cref="nodeCounts"/> directly, as if it were its own one-decision coverage pass over
    /// just the body subtree. Scoped to its own local <c>value</c>/<c>evaluated</c> arrays, because the body's
    /// nodes get a different value for every element, which the shared per-decision arrays cannot hold.
    /// </summary>
    private void PairElement(int bodyIndex, TraceNode bodyTrace)
    {
        TruthValue[] elementValue = new TruthValue[this.nodes.Count];
        bool[] elementEvaluated = new bool[this.nodes.Count];
        Array.Fill(elementValue, TruthValue.Unknown);
        this.Pair(bodyIndex, bodyTrace, elementValue, elementEvaluated);
        this.CheckBodyValues(bodyIndex, elementValue, elementEvaluated);

        int end = this.subtreeEnds[bodyIndex];
        for (int i = bodyIndex; i < end; i++)
        {
            if (!elementEvaluated[i])
            {
                this.nodeCounts[i, (int)Count.NotEvaluated]++;
                continue;
            }

            this.nodeCounts[i, (int)Count.Evaluated]++;
            this.nodeCounts[i, (int)ToCount(elementValue[i])]++;
            if (Values.Any(other => other != elementValue[i] && this.ChangesResultBounded(i, other, elementValue, bodyIndex)))
            {
                this.nodeCounts[i, (int)Count.Decided]++;
            }
        }
    }

    /// <summary>Checks that each operator value follows from its operand values, so the decision belongs to this rule and every later recomputation starts from the recorded values.</summary>
    private void CheckValues(TruthValue[] value, bool[] evaluated, TruthValue result)
    {
        if (!evaluated[0] || value[0] != result)
        {
            throw Mismatch(this.paths[0]);
        }

        for (int i = 0; i < this.nodes.Count; i++)
        {
            bool consistent = this.nodes[i] switch
            {
                ConstantExpression constant => !evaluated[i] || constant.Value == value[i],
                TermExpression => true,

                // Trusted like a term: the fold over its elements is not recomputable from a single operand value
                // (ADR-0010 decision 7), so its recorded result is taken as given, as a term's is.
                QuantifierExpression => true,
                _ => !evaluated[i] || this.CombineAt(i, value) == value[i],
            };
            if (!consistent)
            {
                throw Mismatch(this.paths[i]);
            }
        }
    }

    /// <summary>The <see cref="CheckValues"/> consistency check, scoped to one element's body subtree instead of the whole decision.</summary>
    private void CheckBodyValues(int bodyIndex, TruthValue[] value, bool[] evaluated)
    {
        int end = this.subtreeEnds[bodyIndex];
        for (int i = bodyIndex; i < end; i++)
        {
            bool consistent = this.nodes[i] switch
            {
                ConstantExpression constant => !evaluated[i] || constant.Value == value[i],
                TermExpression => true,
                QuantifierExpression => true,
                _ => !evaluated[i] || this.CombineAt(i, value) == value[i],
            };
            if (!consistent)
            {
                throw Mismatch(this.paths[i]);
            }
        }
    }

    private TruthValue CombineAt(int index, TruthValue[] value)
    {
        return Combine(this.nodes[index], [.. this.operands[index].Select(operand => value[operand])]);
    }

    /// <summary>
    /// Tells whether the rule result changes when node <paramref name="index"/> alone takes <paramref name="replacement"/>.
    /// Only the ancestors are recomputed, and the walk stops at the first ancestor whose value does not change.
    /// </summary>
    private bool ChangesResult(int index, TruthValue replacement, TruthValue[] value)
    {
        int current = index;
        TruthValue changed = replacement;
        for (int parent = this.parents[current]; parent >= 0; parent = this.parents[current])
        {
            TruthValue[] operandValues =
            [
                .. this.operands[parent].Select(operand => operand == current ? changed : value[operand]),
            ];
            changed = Combine(this.nodes[parent], operandValues);
            if (changed == value[parent])
            {
                return false;
            }

            current = parent;
        }

        return changed != value[0];
    }

    /// <summary>
    /// <see cref="ChangesResult"/>, bounded at <paramref name="subtreeRoot"/> instead of the rule's root: used by
    /// <see cref="PairElement"/>, whose <paramref name="value"/> array holds only one element's body subtree, so
    /// ascending past <paramref name="subtreeRoot"/> (into the quantifier node and beyond) would read values for
    /// nodes this array was never filled for.
    /// </summary>
    private bool ChangesResultBounded(int index, TruthValue replacement, TruthValue[] value, int subtreeRoot)
    {
        if (index == subtreeRoot)
        {
            return replacement != value[subtreeRoot];
        }

        int current = index;
        TruthValue changed = replacement;
        while (true)
        {
            int parent = this.parents[current];
            TruthValue[] operandValues =
            [
                .. this.operands[parent].Select(operand => operand == current ? changed : value[operand]),
            ];
            changed = Combine(this.nodes[parent], operandValues);
            if (changed == value[parent])
            {
                return false;
            }

            if (parent == subtreeRoot)
            {
                return true;
            }

            current = parent;
        }
    }

    private void CountTerm(int term, TruthValue[] value, bool[] evaluated)
    {
        int[] occurrences = [.. this.termNodes[term].Where(node => evaluated[node])];
        if (occurrences.Length == 0)
        {
            return;
        }

        // The evaluator runs a term once per evaluation, so every evaluated occurrence holds the same value.
        TruthValue termValue = value[occurrences[0]];
        this.termCounts[term, (int)Count.Evaluated]++;
        this.termCounts[term, (int)ToCount(termValue)]++;

        bool decided =
            occurrences.Length == 1
                ? Values.Any(other => other != termValue && this.ChangesResult(occurrences[0], other, value))
                : Values.Any(other => other != termValue && this.RecomputeRoot(occurrences, other, value) != value[0]);
        if (decided)
        {
            this.termCounts[term, (int)Count.Decided]++;
        }
    }

    /// <summary>Recomputes the whole tree with every node in <paramref name="replaced"/> set to <paramref name="replacement"/>, and returns the root value.</summary>
    private TruthValue RecomputeRoot(int[] replaced, TruthValue replacement, TruthValue[] value)
    {
        TruthValue[] recomputed = [.. value];
        foreach (int node in replaced)
        {
            recomputed[node] = replacement;
        }

        // Operands follow their operator in depth-first order, so a backward pass sees every operand before its operator.
        for (int i = this.nodes.Count - 1; i >= 0; i--)
        {
            bool isLeaf = this.operands[i].Length == 0;
            bool isWithinReplaced = replaced.Any(node => node >= i && node < this.subtreeEnds[i]);
            if (!isLeaf && isWithinReplaced && !replaced.Contains(i))
            {
                recomputed[i] = this.CombineAt(i, recomputed);
            }
        }

        return recomputed[0];
    }
}
