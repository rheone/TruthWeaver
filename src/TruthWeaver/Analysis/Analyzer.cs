namespace TruthWeaver.Analysis;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Printing;

/// <summary>
/// The BDD-based analyzer step of the compilation pipeline (ADR-0003: Parse → Validate → Analyze →
/// Build). It reasons in Strong K3 (ADR-0005 decision 17) with a dual-rail BDD: every sub-expression is
/// represented by two BDDs, <em>definitely true</em> (it is <c>True</c>) and <em>possibly true</em>
/// (it is <c>True</c> or <c>Unknown</c>, i.e. not <c>False</c>). A sub-expression is reported as a
/// tautology only when it is <c>True</c> for every <c>{True, False, Unknown}</c> assignment of its terms
/// (the definitely-true rail is constant true), and as a contradiction only when it is <c>False</c> for
/// every assignment (the possibly-true rail is constant false). So <c>A AND NOT A</c> and
/// <c>A OR NOT A</c> are not reported: both are <c>Unknown</c> when <c>A</c> is. Term identity
/// (CONTEXT.md) recognises repeated references to the same variable. Findings are
/// <see cref="DiagnosticSeverity.Warning"/> diagnostics — they never block compilation.
/// </summary>
internal static class Analyzer
{
    private static readonly DualRail TrueRail = new(BddManager.True, BddManager.True);
    private static readonly DualRail FalseRail = new(BddManager.False, BddManager.False);
    private static readonly DualRail UnknownRail = new(BddManager.False, BddManager.True);

    /// <summary>Analyzes a compiled tree for Strong K3 tautologies and contradictions.</summary>
    /// <param name="root">The compiled expression tree.</param>
    /// <param name="options">The compiler options, whose <c>MaxAnalysisTerms</c> caps this analysis.</param>
    /// <returns>The diagnostics raised (never <see cref="DiagnosticSeverity.Error"/>).</returns>
    public static IReadOnlyList<Diagnostic> Analyze(Expression root, CompilerOptions options)
    {
        HashSet<TermIdentity> distinctTerms = [];
        CollectTerms(root, distinctTerms);
        if (distinctTerms.Count > options.MaxAnalysisTerms)
        {
            return
            [
                Diagnostic.Info(
                    DiagnosticCodes.AnalysisSkippedTooManyTerms,
                    $"Constant/contradiction analysis skipped: {distinctTerms.Count} distinct terms exceeds the configured cap of {options.MaxAnalysisTerms}.",
                    SourceSpan.None
                ),
            ];
        }

        Dictionary<TermIdentity, int> variableIndex = [];
        BddManager bdd = new();
        List<Diagnostic> diagnostics = [];
        _ = Build(root, bdd, variableIndex, diagnostics);
        return diagnostics;
    }

    /// <summary>
    /// Decides whether two trees have the same Strong K3 value for every <c>{True, False, Unknown}</c> assignment of
    /// their terms, by building both on one shared BDD (so equal terms share variables) and comparing both rails: the
    /// diagrams are canonical, so equal functions are the same node.
    /// </summary>
    /// <param name="left">The first tree.</param>
    /// <param name="right">The second tree.</param>
    /// <returns>
    /// <see langword="null"/> when the trees are equivalent; otherwise an assignment of every term in either tree
    /// for which they differ. Terms the difference does not depend on are <c>False</c>.
    /// </returns>
    public static IReadOnlyDictionary<TermIdentity, TruthValue>? FindDifference(Expression left, Expression right)
    {
        Dictionary<TermIdentity, int> variableIndex = [];
        BddManager bdd = new();
        List<Diagnostic> ignored = [];
        DualRail leftRail = Build(left, bdd, variableIndex, ignored);
        DualRail rightRail = Build(right, bdd, variableIndex, ignored);

        // The values differ exactly when either rail differs.
        int difference = bdd.Or(bdd.Xor(leftRail.Definite, rightRail.Definite), bdd.Xor(leftRail.Possible, rightRail.Possible));
        IReadOnlyDictionary<int, bool>? path = bdd.FindSatisfyingAssignment(difference);
        if (path is null)
        {
            return null;
        }

        Dictionary<TermIdentity, TruthValue> assignment = [];
        foreach ((TermIdentity term, int index) in variableIndex)
        {
            // Each term is two BDD variables: "is True" (2i) and "is Unknown" (2i + 1); unmentioned ones read as false.
            if (path.GetValueOrDefault(2 * index))
            {
                assignment[term] = TruthValue.True;
            }
            else
            {
                assignment[term] = path.GetValueOrDefault((2 * index) + 1) ? TruthValue.Unknown : TruthValue.False;
            }
        }

        return assignment;
    }

    /// <summary>
    /// Works out which of <c>True</c>, <c>False</c> and <c>Unknown</c> a tree can take over every assignment of its terms,
    /// from the two rails: <c>True</c> is possible unless the definitely-true rail is empty, <c>Unknown</c> unless
    /// "possibly true and not definitely true" is empty, and <c>False</c> unless the possibly-true rail is full.
    /// </summary>
    /// <param name="root">The tree.</param>
    /// <param name="maxTerms">The distinct-term cap; a tree with more is not analysed.</param>
    /// <returns>The reachable values, or <see langword="null"/> when the tree has more than <paramref name="maxTerms"/> terms.</returns>
    public static ValueProfile? Profile(Expression root, int maxTerms)
    {
        if (DistinctTerms(root).Count > maxTerms)
        {
            return null;
        }

        BddManager bdd = new();
        DualRail rail = Build(root, bdd, [], []);
        return new ValueProfile(
            CanBeTrue: rail.Definite != BddManager.False,
            CanBeFalse: rail.Possible != BddManager.True,
            CanBeUnknown: bdd.And(rail.Possible, bdd.Not(rail.Definite)) != BddManager.False
        );
    }

    /// <summary>
    /// Measures the cost of a tree's Strong K3 analysis: the number of decision nodes in the shared dual-rail BDD
    /// manager after the tree is built. Sub-graphs shared between the two rails or between sub-expressions count once.
    /// </summary>
    /// <param name="root">The tree.</param>
    /// <param name="maxTerms">The distinct-term cap; a tree with more is not analysed.</param>
    /// <returns>The node count, or <see langword="null"/> when the tree has more than <paramref name="maxTerms"/> terms.</returns>
    public static int? BddNodeCount(Expression root, int maxTerms)
    {
        if (DistinctTerms(root).Count > maxTerms)
        {
            return null;
        }

        BddManager bdd = new();
        _ = Build(root, bdd, [], []);
        return bdd.NodeCount;
    }

    /// <summary>Collects the distinct terms of a tree.</summary>
    /// <param name="root">The tree.</param>
    /// <returns>The distinct term identities.</returns>
    public static HashSet<TermIdentity> DistinctTerms(Expression root)
    {
        HashSet<TermIdentity> terms = [];
        CollectTerms(root, terms);
        return terms;
    }

    /// <summary>
    /// Reads a tree's Strong K3 value for one assignment of its terms directly from the analyzer's own dual-rail
    /// BDD, by walking both rails for that assignment (ticket 17). Used to pin <c>Evaluator</c> to the analyzer:
    /// today the two are compared only at the tautology/contradiction extremes (<see cref="Analyze"/>), so a
    /// property test instead checks every generated assignment, not only those two.
    /// </summary>
    /// <param name="root">The tree.</param>
    /// <param name="assignment">Each term's value, keyed by predicate name (terms in this analysis are zero-argument).</param>
    /// <returns>The tree's Strong K3 value under <paramref name="assignment"/>.</returns>
    internal static TruthValue ValueAt(Expression root, IReadOnlyDictionary<string, TruthValue> assignment)
    {
        Dictionary<TermIdentity, int> variableIndex = [];
        BddManager bdd = new();
        DualRail rail = Build(root, bdd, variableIndex, []);

        // Each term's two BDD variables (2i definite, 2i+1 unknown) read back from the assignment by name.
        Dictionary<int, TruthValue> valueByVariableIndex = variableIndex.ToDictionary(
            kv => kv.Value,
            kv => assignment[kv.Key.PredicateName]
        );

        bool ValueOf(int variable)
        {
            TruthValue value = valueByVariableIndex[variable / 2];
            return variable % 2 == 0 ? value == TruthValue.True : value == TruthValue.Unknown;
        }

        bool definite = bdd.Evaluate(rail.Definite, ValueOf);
        bool possible = bdd.Evaluate(rail.Possible, ValueOf);
        if (definite)
        {
            return TruthValue.True;
        }

        return possible ? TruthValue.Unknown : TruthValue.False;
    }

    /// <summary>
    /// Adds every term under <paramref name="node"/> to <paramref name="terms"/>. Children come from
    /// <see cref="ExpressionShape"/>, the single child list, so a new operator cannot have its terms skipped here.
    /// </summary>
    private static void CollectTerms(Expression node, HashSet<TermIdentity> terms)
    {
        // Terms and constants are leaves: ExpressionShape has no operands for them.
        if (node is TermExpression term)
        {
            terms.Add(term.Identity);
            return;
        }

        if (node is ConstantExpression)
        {
            return;
        }

        foreach (Expression operand in ExpressionShape.Of(node).Operands)
        {
            CollectTerms(operand, terms);
        }
    }

    /// <summary>
    /// Strong K3 negation on the rails: <c>NOT x</c> is definitely true when <c>x</c> is not even possibly
    /// true, and possibly true when <c>x</c> is not definitely true. <c>Unknown</c> stays <c>Unknown</c>.
    /// </summary>
    private static DualRail Not(BddManager bdd, DualRail x)
    {
        return new DualRail(bdd.Not(x.Possible), bdd.Not(x.Definite));
    }

    /// <summary>Strong K3 conjunction: definitely true needs both definitely true; possibly true needs both possibly true.</summary>
    private static DualRail And(BddManager bdd, DualRail x, DualRail y)
    {
        return new DualRail(bdd.And(x.Definite, y.Definite), bdd.And(x.Possible, y.Possible));
    }

    /// <summary>Strong K3 disjunction: the dual of <see cref="And"/>.</summary>
    private static DualRail Or(BddManager bdd, DualRail x, DualRail y)
    {
        return new DualRail(bdd.Or(x.Definite, y.Definite), bdd.Or(x.Possible, y.Possible));
    }

    /// <summary>NAND as its primitive definition <c>NOT (x AND y)</c>.</summary>
    private static DualRail Nand(BddManager bdd, DualRail x, DualRail y)
    {
        return Not(bdd, And(bdd, x, y));
    }

    /// <summary>NOR as its primitive definition <c>NOT (x OR y)</c>.</summary>
    private static DualRail Nor(BddManager bdd, DualRail x, DualRail y)
    {
        return Not(bdd, Or(bdd, x, y));
    }

    /// <summary>Material implication as its primitive definition <c>NOT x OR y</c>.</summary>
    private static DualRail Implies(BddManager bdd, DualRail x, DualRail y)
    {
        return Or(bdd, Not(bdd, x), y);
    }

    /// <summary>
    /// Binary XOR as <c>(x AND NOT y) OR (NOT x AND y)</c> — the primitive definition, so it is
    /// <c>Unknown</c> whenever either side is.
    /// </summary>
    private static DualRail Xor(BddManager bdd, DualRail x, DualRail y)
    {
        return Or(bdd, And(bdd, x, Not(bdd, y)), And(bdd, Not(bdd, x), y));
    }

    /// <summary>
    /// N-ary parity as the left fold of binary <see cref="Xor"/>. Since XOR is <c>Unknown</c> whenever either side
    /// is, the fold is <c>Unknown</c> whenever any operand is, and otherwise true for an odd number of true operands.
    /// </summary>
    private static DualRail Parity(BddManager bdd, IReadOnlyList<DualRail> operands)
    {
        DualRail result = operands[0];
        for (int i = 1; i < operands.Count; i++)
        {
            result = Xor(bdd, result, operands[i]);
        }

        return result;
    }

    /// <summary>
    /// <c>COALESCE</c> as a right fold of the binary form. For <c>x</c> with rail <c>(D, P)</c>, <c>x</c> is
    /// <c>True</c> iff <c>D</c>, <c>False</c> iff <c>NOT P</c> and <c>Unknown</c> iff <c>P AND NOT D</c>; the result is
    /// <c>x</c> when known and <c>y</c> when <c>x</c> is Unknown. So it is definitely true when
    /// <c>D_x OR (Unknown_x AND D_y)</c>, which simplifies to <c>D_x OR (P_x AND D_y)</c>, and possibly true when it is
    /// not definitely false: <c>NOT (NOT P_x OR (Unknown_x AND NOT P_y))</c>, i.e. <c>P_x AND (D_x OR P_y)</c>.
    /// </summary>
    private static DualRail Coalesce(BddManager bdd, IReadOnlyList<DualRail> operands)
    {
        DualRail result = operands[^1];
        for (int i = operands.Count - 2; i >= 0; i--)
        {
            DualRail x = operands[i];
            result = new DualRail(
                bdd.Or(x.Definite, bdd.And(x.Possible, result.Definite)),
                bdd.And(x.Possible, bdd.Or(x.Definite, result.Possible))
            );
        }

        return result;
    }

    /// <summary>
    /// An inspection on the rails. The tested state is a definite fact about <c>x</c>, so both result rails are the same
    /// BDD: <c>IsTrue</c> is <c>D</c>, <c>IsFalse</c> is <c>NOT P</c>, <c>IsUnknown</c> is <c>P AND NOT D</c> and
    /// <c>IsKnown</c> is <c>D OR NOT P</c> (for <c>x</c> with rail <c>(D, P)</c>); the result is never <c>Unknown</c>.
    /// </summary>
    private static DualRail Inspect(BddManager bdd, InspectionKind kind, DualRail x)
    {
        int isTrue = kind switch
        {
            InspectionKind.IsTrue => x.Definite,
            InspectionKind.IsFalse => bdd.Not(x.Possible),
            InspectionKind.IsUnknown => bdd.And(x.Possible, bdd.Not(x.Definite)),
            InspectionKind.IsKnown => bdd.Or(x.Definite, bdd.Not(x.Possible)),
            _ => throw new InvalidOperationException($"Unhandled inspection kind '{kind}'."),
        };
        return new DualRail(isTrue, isTrue);
    }

    /// <summary>
    /// <c>If(c, t, f)</c> as its primitive definition <c>(c AND t) OR (NOT c AND f) OR (t AND f)</c>. The last, consensus
    /// term is what stops an Unknown condition from guessing: with it <c>If(Unknown, True, True)</c> is <c>True</c>, and
    /// for a definite condition it never changes the multiplexer's value.
    /// </summary>
    private static DualRail If(BddManager bdd, DualRail condition, DualRail whenTrue, DualRail whenFalse)
    {
        return Or(
            bdd,
            Or(bdd, And(bdd, condition, whenTrue), And(bdd, Not(bdd, condition), whenFalse)),
            And(bdd, whenTrue, whenFalse)
        );
    }

    /// <summary>
    /// "At least <paramref name="k"/> operands are true" over the interval semantics: definitely true when
    /// the definitely-true operands alone reach <paramref name="k"/>, possibly true when the possibly-true
    /// operands can. Every other cardinality operator is built from this and <see cref="Not"/>.
    /// </summary>
    private static DualRail AtLeast(BddManager bdd, IReadOnlyList<DualRail> operands, int k)
    {
        return new DualRail(
            AtLeastBdd(bdd, [.. operands.Select(o => o.Definite)], k, 0),
            AtLeastBdd(bdd, [.. operands.Select(o => o.Possible)], k, 0)
        );
    }

    private static int AtLeastBdd(BddManager bdd, IReadOnlyList<int> operandIds, int k, int fromIndex)
    {
        int remaining = operandIds.Count - fromIndex;
        if (k <= 0)
        {
            return BddManager.True;
        }

        if (k > remaining)
        {
            return BddManager.False;
        }

        int withFirstTrue = bdd.And(operandIds[fromIndex], AtLeastBdd(bdd, operandIds, k - 1, fromIndex + 1));
        int withoutFirst = bdd.And(bdd.Not(operandIds[fromIndex]), AtLeastBdd(bdd, operandIds, k, fromIndex + 1));
        return bdd.Or(withFirstTrue, withoutFirst);
    }

    /// <summary>"Exactly <paramref name="k"/> operands are true": <c>AtLeast(k) AND NOT AtLeast(k + 1)</c>.</summary>
    private static DualRail Exactly(BddManager bdd, IReadOnlyList<DualRail> operands, int k)
    {
        return Threshold(bdd, operands, ThresholdSemantics.Terms(ThresholdComparison.Exactly, k));
    }

    /// <summary>
    /// Builds a count condition from its "at least" terms: the lower test and the negated upper test, joined with
    /// <c>AND</c> when both are present. <see cref="ThresholdSemantics"/> owns the <c>k + 1</c> rules.
    /// </summary>
    private static DualRail Threshold(BddManager bdd, IReadOnlyList<DualRail> operands, ThresholdTerms terms)
    {
        DualRail? lower = terms.AtLeast is int low ? AtLeast(bdd, operands, low) : null;
        DualRail? upper = terms.NotAtLeast is int high ? Not(bdd, AtLeast(bdd, operands, high)) : null;
        return (lower, upper) switch
        {
            ({ } l, { } u) => And(bdd, l, u),
            ({ } l, null) => l,
            (null, { } u) => u,
            _ => throw new InvalidOperationException("A threshold has at least one test."),
        };
    }

    private static string Message(string alwaysValue, Expression node)
    {
        return $"Strong K3 analysis: this sub-expression is {alwaysValue} for every True/False/Unknown assignment of its terms: {CanonicalPrinter.Print(node)}";
    }

    private static void Diagnose(DualRail rail, Expression node, List<Diagnostic> diagnostics)
    {
        if (rail.Definite == BddManager.True)
        {
            // Definitely true in every assignment: it can never be False or Unknown.
            diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.StructuralTautology, Message("True", node), SourceSpan.None));
        }
        else if (rail.Possible == BddManager.False)
        {
            // Never possibly true in any assignment: it can never be True or Unknown.
            diagnostics.Add(
                Diagnostic.Warning(DiagnosticCodes.StructuralContradiction, Message("False", node), SourceSpan.None)
            );
        }
    }

    private static DualRail Build(
        Expression node,
        BddManager bdd,
        Dictionary<TermIdentity, int> variableIndex,
        List<Diagnostic> diagnostics
    )
    {
        DualRail rail;
        switch (node)
        {
            case ConstantExpression c:
                return c.Value switch
                {
                    TruthValue.True => TrueRail,
                    TruthValue.False => FalseRail,
                    _ => UnknownRail,
                };
            case TermExpression t:
                if (!variableIndex.TryGetValue(t.Identity, out int index))
                {
                    index = variableIndex.Count;
                    variableIndex[t.Identity] = index;
                }

                // Each term gets two independent BDD variables: d ("is True") and q ("is Unknown").
                // The rails are d and d OR q, so every variable setting is a valid K3 state
                // (d=1 is True, d=0 and q=1 is Unknown, d=0 and q=0 is False) and a "definitely true but
                // not possibly true" state can never be produced.
                int definite = bdd.Variable(2 * index);
                int unknown = bdd.Variable((2 * index) + 1);
                return new DualRail(definite, bdd.Or(definite, unknown));
            case NotExpression n:
                rail = Not(bdd, Build(n.Operand, bdd, variableIndex, diagnostics));
                break;
            case AndExpression a:
                rail = FoldRight(BuildOperands(a.Operands, bdd, variableIndex, diagnostics), TrueRail, bdd, And);
                break;
            case OrExpression o:
                rail = FoldRight(BuildOperands(o.Operands, bdd, variableIndex, diagnostics), FalseRail, bdd, Or);
                break;
            case XorExpression x:
                rail = Xor(
                    bdd,
                    Build(x.Left, bdd, variableIndex, diagnostics),
                    Build(x.Right, bdd, variableIndex, diagnostics)
                );
                break;
            case EquivalentExpression xn:
                rail = Not(
                    bdd,
                    Xor(bdd, Build(xn.Left, bdd, variableIndex, diagnostics), Build(xn.Right, bdd, variableIndex, diagnostics))
                );
                break;
            case NandExpression nd:
                rail = Nand(
                    bdd,
                    Build(nd.Left, bdd, variableIndex, diagnostics),
                    Build(nd.Right, bdd, variableIndex, diagnostics)
                );
                break;
            case NorExpression nr:
                rail = Nor(
                    bdd,
                    Build(nr.Left, bdd, variableIndex, diagnostics),
                    Build(nr.Right, bdd, variableIndex, diagnostics)
                );
                break;
            case ImpliesExpression im:
                rail = Implies(
                    bdd,
                    Build(im.Antecedent, bdd, variableIndex, diagnostics),
                    Build(im.Consequent, bdd, variableIndex, diagnostics)
                );
                break;
            case ParityExpression nx:
                rail = Parity(bdd, BuildOperands(nx.Operands, bdd, variableIndex, diagnostics));
                break;
            case AnyExpression an:
                rail = AtLeast(bdd, BuildOperands(an.Operands, bdd, variableIndex, diagnostics), 1);
                break;
            case AllExpression al:
                List<DualRail> allOperands = BuildOperands(al.Operands, bdd, variableIndex, diagnostics);
                rail = AtLeast(bdd, allOperands, allOperands.Count);
                break;
            case NoneExpression no:
                // AtMost(0, ...) is "not even one operand is true": the negation of AtLeast(1, ...).
                rail = Not(bdd, AtLeast(bdd, BuildOperands(no.Operands, bdd, variableIndex, diagnostics), 1));
                break;
            case ExactlyOneExpression e:
                rail = Exactly(bdd, BuildOperands(e.Operands, bdd, variableIndex, diagnostics), 1);
                break;
            case BetweenExpression bt:
                rail = Threshold(
                    bdd,
                    BuildOperands(bt.Operands, bdd, variableIndex, diagnostics),
                    ThresholdSemantics.Between(bt.Min, bt.Max)
                );
                break;
            case CoalesceExpression co:
                rail = Coalesce(bdd, BuildOperands(co.Operands, bdd, variableIndex, diagnostics));
                break;
            case InspectionExpression ins:
                rail = Inspect(bdd, ins.Kind, Build(ins.Operand, bdd, variableIndex, diagnostics));
                break;
            case IfExpression iff:
                rail = If(
                    bdd,
                    Build(iff.Condition, bdd, variableIndex, diagnostics),
                    Build(iff.WhenTrue, bdd, variableIndex, diagnostics),
                    Build(iff.WhenFalse, bdd, variableIndex, diagnostics)
                );
                break;
            case ThresholdExpression th:
                List<DualRail> operands = BuildOperands(th.Operands, bdd, variableIndex, diagnostics);
                rail = Threshold(bdd, operands, ThresholdSemantics.Terms(th.Comparison, th.K));
                break;
            default:
                throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'.");
        }

        Diagnose(rail, node, diagnostics);
        return rail;
    }

    /// <summary>
    /// Combines already-built operand rails with an associative, commutative connective (<see cref="And"/> or
    /// <see cref="Or"/>), from the last operand to the first. The result is the same canonical BDD as a left fold; only
    /// the cost differs. Operands are built left to right, so an earlier operand's terms take lower variable indices
    /// and sit above a later operand's in the ordering. Combining <c>op(earlier, accumulated)</c> lets <c>Ite</c> walk
    /// only the earlier operand's nodes and reuse the accumulated BDD unchanged under it. A left fold puts the growing
    /// accumulated BDD on top and rebuilds all of it for every operand, which is quadratic in the operand count
    /// (k3-hardening ticket 19).
    /// </summary>
    private static DualRail FoldRight(
        List<DualRail> operands,
        DualRail identity,
        BddManager bdd,
        Func<BddManager, DualRail, DualRail, DualRail> combine
    )
    {
        DualRail result = identity;
        for (int i = operands.Count - 1; i >= 0; i--)
        {
            result = combine(bdd, operands[i], result);
        }

        return result;
    }

    private static List<DualRail> BuildOperands(
        IEnumerable<Expression> operands,
        BddManager bdd,
        Dictionary<TermIdentity, int> variableIndex,
        List<Diagnostic> diagnostics
    )
    {
        return [.. operands.Select(o => Build(o, bdd, variableIndex, diagnostics))];
    }

    /// <summary>
    /// The two BDDs that describe one K3 value: <c>Definite</c> is true exactly when the value is
    /// <c>True</c>, <c>Possible</c> exactly when it is <c>True</c> or <c>Unknown</c>. The pair
    /// (<c>Definite</c>, <c>Possible</c>) encodes <c>True</c> = (1,1), <c>Unknown</c> = (0,1) and
    /// <c>False</c> = (0,0).
    /// </summary>
    /// <param name="Definite">The BDD node for "is definitely True".</param>
    /// <param name="Possible">The BDD node for "is True or Unknown" (not False).</param>
    private readonly record struct DualRail(int Definite, int Possible);
}
