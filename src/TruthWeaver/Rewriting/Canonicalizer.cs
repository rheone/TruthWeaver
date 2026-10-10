namespace TruthWeaver.Rewriting;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;

/// <summary>
/// Gives every rule in an equivalence class one deterministic representation (ADR-0005 decision 10), using only rewrites that
/// are identities of Strong Kleene logic: exact alias collapsing, double-negation elimination, flattening of associative
/// operators, sorting of commutative operands and removal of repeated <c>AND</c>/<c>OR</c> operands. No complement law
/// (<c>a OR NOT a = True</c>) and no constant folding is applied here; that is simplification's job and only where sound.
/// </summary>
/// <remarks>
/// The order of commutative operands is the ordinal order of their canonical DSL text, which is a total order over
/// structurally distinct operands and does not depend on how the rule was built. Because the sort key is text, the order
/// puts upper-case operator spellings before lower-case term names (<c>NOT a</c> sorts before <c>a</c>); that is stable and
/// deterministic, not meaningful.
/// </remarks>
internal static class Canonicalizer
{
    /// <summary>A safety bound on the repeat-until-stable loop; one bottom-up pass is already a fixed point in practice.</summary>
    private const int MaxPasses = 8;

    /// <summary>Canonicalises <paramref name="root"/> and everything below it.</summary>
    /// <param name="root">The tree to canonicalise.</param>
    /// <param name="trace">Receives each change, or <see langword="null"/> to report nothing.</param>
    /// <returns>The canonical tree, which evaluates identically and is never larger.</returns>
    public static Expression Canonicalize(Expression root, RewriteTrace? trace = null)
    {
        Expression current = root;
        for (int pass = 0; pass < MaxPasses; pass++)
        {
            Expression next = new Pass(trace).Visit(current);
            if (next.Equals(current))
            {
                return current;
            }

            current = next;
        }

        return current;
    }

    /// <summary>
    /// The canonical sort key of an expression: its canonical DSL text. Exposed so the simplifier orders operands the same way.
    /// </summary>
    /// <param name="node">The expression to key.</param>
    /// <returns>The canonical text.</returns>
    public static string KeyOf(Expression node)
    {
        return CanonicalPrinter.Print(node);
    }

    /// <summary>One bottom-up sweep. The memos keep shared sub-trees shared and avoid re-printing a repeated operand.</summary>
    private sealed class Pass(RewriteTrace? trace)
    {
        private readonly Dictionary<Expression, Expression> memo = [with(ReferenceEqualityComparer.Instance)];
        private readonly Dictionary<Expression, string> keys = [with(ReferenceEqualityComparer.Instance)];

        public Expression Visit(Expression node)
        {
            if (this.memo.TryGetValue(node, out Expression? known))
            {
                return known;
            }

            // Children first, so every operand a rule below sees is already canonical.
            Expression rebuilt = ExpressionTools.MapChildren(node, this.Visit);
            Expression result = this.Normalise(rebuilt);
            this.memo[node] = result;
            return result;
        }

        private static ThresholdExpression Threshold(ThresholdComparison comparison, int k, EquatableArray<Expression> operands)
        {
            return new ThresholdExpression(comparison, k, operands);
        }

        /// <summary><c>COALESCE</c> is associative, so a nested <c>COALESCE</c> splices into its parent without reordering.</summary>
        private Expression FlattenCoalesce(CoalesceExpression c)
        {
            if (!c.Operands.Any(o => o is CoalesceExpression))
            {
                return c;
            }

            List<Expression> flat = [];
            foreach (Expression operand in c.Operands)
            {
                if (operand is CoalesceExpression nested)
                {
                    flat.AddRange(nested.Operands);
                }
                else
                {
                    flat.Add(operand);
                }
            }

            CoalesceExpression flattened = new(ExpressionTools.Array(flat));
            trace?.Report(RewriteLaw.Flatten, c, flattened);
            return flattened;
        }

        private Expression Normalise(Expression node)
        {
            return node switch
            {
                NotExpression { Operand: NotExpression inner } => this.Reported(RewriteLaw.DoubleNegation, node, inner.Operand),
                AndExpression a => this.Junction(a, a.Operands, isAnd: true),
                OrExpression o => this.Junction(o, o.Operands, isAnd: false),
                AllExpression all => this.Junction(all, all.Operands, isAnd: true),
                AnyExpression any => this.Junction(any, any.Operands, isAnd: false),
                CoalesceExpression c => this.FlattenCoalesce(c),
                ThresholdExpression t => this.NormaliseThreshold(t),
                ExactlyOneExpression one => this.SortedOperands(
                    one,
                    one.Operands,
                    sorted => Threshold(ThresholdComparison.Exactly, 1, sorted),
                    alias: Threshold(ThresholdComparison.Exactly, 1, one.Operands)
                ),
                BetweenExpression b => this.SortedOperands(
                    b,
                    b.Operands,
                    sorted => new BetweenExpression(b.Min, b.Max, sorted)
                ),
                ParityExpression nx => this.SortedOperands(nx, nx.Operands, sorted => new ParityExpression(sorted)),
                XorExpression x => this.OrderPair(x.Left, x.Right, static (l, r) => new XorExpression(l, r), x),
                EquivalentExpression e => this.OrderPair(e.Left, e.Right, static (l, r) => new EquivalentExpression(l, r), e),
                NandExpression nd => this.OrderPair(nd.Left, nd.Right, static (l, r) => new NandExpression(l, r), nd),
                NorExpression nr => this.OrderPair(nr.Left, nr.Right, static (l, r) => new NorExpression(l, r), nr),
                _ => node,
            };
        }

        /// <summary>Reports a step and returns its result.</summary>
        private Expression Reported(RewriteLaw law, Expression before, Expression after)
        {
            trace?.Report(law, before, after);
            return after;
        }

        /// <summary>
        /// Sorts <paramref name="operands"/> and builds the result. When <paramref name="alias"/> is given, an alias step from
        /// <paramref name="original"/> to it is reported first; a reorder step follows when the sort changes the order.
        /// </summary>
        /// <param name="original">The node being canonicalised.</param>
        /// <param name="operands">The operands to sort.</param>
        /// <param name="build">Builds the result from the sorted operands.</param>
        /// <param name="alias">The exact-alias spelling of <paramref name="original"/>, or <see langword="null"/> when it has none.</param>
        private Expression SortedOperands(
            Expression original,
            EquatableArray<Expression> operands,
            Func<EquatableArray<Expression>, Expression> build,
            Expression? alias = null
        )
        {
            Expression aliased = alias ?? original;
            if (alias is not null)
            {
                trace?.Report(RewriteLaw.AliasCollapse, original, alias);
            }

            Expression result = build(this.Sorted(operands));
            if (!result.Equals(aliased))
            {
                trace?.Report(RewriteLaw.Reorder, aliased, result);
            }

            return result;
        }

        /// <summary>
        /// The count of <c>True</c> operands decides every threshold, so <c>GreaterThan(k)</c> is <c>AtLeast(k + 1)</c> and
        /// <c>LessThan(k)</c> is <c>AtMost(k - 1)</c>, and <c>AtLeast(1)</c> / <c>AtLeast(n)</c> are exactly <c>OR</c> /
        /// <c>AND</c> (the interval collapses, ADR-0005 decision 6). Operand count does not matter for the order.
        /// </summary>
        private Expression NormaliseThreshold(ThresholdExpression t)
        {
            (ThresholdComparison comparison, int k) = ThresholdSemantics.Normalise(t.Comparison, t.K);

            ThresholdExpression aliased = Threshold(comparison, k, t.Operands);
            int n = t.Operands.Count;
            if (comparison == ThresholdComparison.AtLeast && n >= 2)
            {
                if (k == 1)
                {
                    return this.Junction(t, t.Operands, isAnd: false);
                }

                if (k == n)
                {
                    return this.Junction(t, t.Operands, isAnd: true);
                }
            }

            return this.SortedOperands(t, t.Operands, sorted => Threshold(comparison, k, sorted), alias: aliased);
        }

        /// <summary>Flattens nested same-operator operands, sorts, removes repeats; a single survivor replaces the operator.</summary>
        /// <remarks>Each stage reports its own step, so a trace shows flatten, reorder and idempotence one by one.</remarks>
        private Expression Junction(Expression original, EquatableArray<Expression> operands, bool isAnd)
        {
            Expression current = original;
            if (original is not (AndExpression or OrExpression))
            {
                // ANY, ALL and the AtLeast thresholds that equal OR and AND are aliases of the junction.
                current = ExpressionTools.Junction(operands, isAnd);
                trace?.Report(RewriteLaw.AliasCollapse, original, current);
            }

            List<Expression> flat = [];
            foreach (Expression operand in operands)
            {
                switch (operand)
                {
                    case AndExpression nested when isAnd:
                        flat.AddRange(nested.Operands);
                        break;
                    case OrExpression nested when !isAnd:
                        flat.AddRange(nested.Operands);
                        break;
                    default:
                        flat.Add(operand);
                        break;
                }
            }

            if (flat.Count != operands.Count)
            {
                current = this.Reported(RewriteLaw.Flatten, current, ExpressionTools.Junction(flat, isAnd));
            }

            // Sorting by text puts equal operands side by side, so removing neighbours that are equal removes every repeat.
            List<Expression> sorted = [.. flat.OrderBy(this.Key, StringComparer.Ordinal)];
            if (!sorted.SequenceEqual(flat))
            {
                current = this.Reported(RewriteLaw.Reorder, current, ExpressionTools.Junction(sorted, isAnd));
            }

            List<Expression> distinct = [];
            foreach (Expression operand in sorted)
            {
                if (distinct.Count == 0 || !distinct[^1].Equals(operand))
                {
                    distinct.Add(operand);
                }
            }

            if (distinct.Count != sorted.Count)
            {
                current = this.Reported(
                    RewriteLaw.Idempotence,
                    current,
                    distinct.Count == 1 ? distinct[0] : ExpressionTools.Junction(distinct, isAnd)
                );
            }

            return current;
        }

        private EquatableArray<Expression> Sorted(EquatableArray<Expression> operands)
        {
            return ExpressionTools.Array(operands.OrderBy(this.Key, StringComparer.Ordinal));
        }

        private Expression OrderPair(
            Expression left,
            Expression right,
            Func<Expression, Expression, Expression> build,
            Expression original
        )
        {
            if (string.CompareOrdinal(this.Key(left), this.Key(right)) <= 0)
            {
                return original;
            }

            return this.Reported(RewriteLaw.Reorder, original, build(right, left));
        }

        private string Key(Expression node)
        {
            if (!this.keys.TryGetValue(node, out string? key))
            {
                key = Canonicalizer.KeyOf(node);
                this.keys[node] = key;
            }

            return key;
        }
    }
}
