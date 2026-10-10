namespace TruthWeaver.Rewriting;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// Rewrites an <see cref="Expression"/> tree into negation, conjunctive or disjunctive normal form using only Strong
/// Kleene (K3) laws: De Morgan, double negation and the distribution of <c>AND</c> over <c>OR</c> (and the reverse).
/// No classical complement law is used, so <c>a AND NOT a</c> is kept as written.
/// </summary>
/// <remarks>
/// The K3 connectives form a distributive lattice with De Morgan negation, so every step is an identity. <c>COALESCE</c>,
/// the inspections and <c>If</c> are not information-monotone, so each is an atom: its operands are normalized, and a
/// <c>NOT</c> above it stays above it. A threshold also stays an atom unless the caller opts in to expanding it,
/// because its expansion is binomial in the operand count.
/// </remarks>
internal static class NormalForms
{
    /// <summary>The normal form to build.</summary>
    internal enum Form
    {
        /// <summary>Negation normal form: <c>NOT</c> only directly above a term or an atom.</summary>
        Nnf,

        /// <summary>Conjunctive normal form: an <c>AND</c> of <c>OR</c>s of literals.</summary>
        Cnf,

        /// <summary>Disjunctive normal form: an <c>OR</c> of <c>AND</c>s of literals.</summary>
        Dnf,
    }

    /// <summary>Rewrites <paramref name="root"/> into <paramref name="form"/>.</summary>
    /// <param name="root">The tree to rewrite.</param>
    /// <param name="form">The normal form.</param>
    /// <param name="expandThresholds">Whether thresholds expand to <c>AND</c>/<c>OR</c>/<c>NOT</c> instead of staying atoms.</param>
    /// <param name="cap">The most nodes, counted as a printed tree, that the rewrite may build.</param>
    /// <returns>The tree and the kept thresholds; the tree is <see langword="null"/> when the cap was exceeded.</returns>
    public static Result Build(Expression root, Form form, bool expandThresholds, long cap)
    {
        NnfBuilder nnf = new(expandThresholds, cap);
        Expression negated = nnf.Visit(root, negate: false);
        if (nnf.Exceeded)
        {
            return new Result(null, nnf.Kept);
        }

        if (form == Form.Nnf)
        {
            return new Result(negated, nnf.Kept);
        }

        List<List<Expression>>? clauses = Distribute(negated, form, cap);
        return new Result(clauses is null ? null : Assemble(clauses, form), nnf.Kept);
    }

    /// <summary>
    /// Distributes the inner connective over the outer one. For disjunctive form the clauses are the <c>AND</c> groups
    /// joined by <c>OR</c>; for conjunctive form they are the <c>OR</c> groups joined by <c>AND</c>. Returns
    /// <see langword="null"/> as soon as the clause set can no longer fit in <paramref name="cap"/> nodes.
    /// </summary>
    private static List<List<Expression>>? Distribute(Expression node, Form form, long cap)
    {
        bool disjunctive = form == Form.Dnf;
        bool isOuter = disjunctive ? node is OrExpression : node is AndExpression;
        bool isInner = disjunctive ? node is AndExpression : node is OrExpression;
        if (!isOuter && !isInner)
        {
            return
            [
                [node],
            ];
        }

        EquatableArray<Expression> operands = node is OrExpression o ? o.Operands : ((AndExpression)node).Operands;
        List<List<Expression>>? acc = isInner
            ?
            [
                [],
            ]
            : [];
        foreach (Expression operand in operands)
        {
            List<List<Expression>>? child = Distribute(operand, form, cap);
            if (child is null)
            {
                return null;
            }

            acc = isOuter ? Union(acc, child) : Product(acc, child, cap);
            if (acc is null || Weight(acc) > cap)
            {
                return null;
            }
        }

        return acc;
    }

    /// <summary>Joins two clause sets of the outer connective, dropping a clause that repeats an earlier one.</summary>
    private static List<List<Expression>> Union(List<List<Expression>> left, List<List<Expression>> right)
    {
        List<List<Expression>> result = [.. left];
        result.AddRange(right.Where(c => !result.Any(existing => existing.SequenceEqual(c))));

        return result;
    }

    /// <summary>Distributes the inner connective over two clause sets: every clause of one with every clause of the other.</summary>
    private static List<List<Expression>>? Product(List<List<Expression>> left, List<List<Expression>> right, long cap)
    {
        if ((long)left.Count * right.Count > cap)
        {
            return null;
        }

        List<List<Expression>> result = [];
        foreach (List<Expression> a in left)
        {
            foreach (List<Expression> b in right)
            {
                // Idempotence (x AND x is x, x OR x is x) holds in K3, so a repeated literal in a clause is dropped.
                List<Expression> merged = [.. a];
                merged.AddRange(b.Where(l => !merged.Contains(l)));

                if (!result.Any(existing => existing.SequenceEqual(merged)))
                {
                    result.Add(merged);
                }
            }

            if (Weight(result) > cap)
            {
                return null;
            }
        }

        return result;
    }

    /// <summary>The literal count plus the clause count: a lower bound on the node count of the assembled tree.</summary>
    private static long Weight(List<List<Expression>> clauses)
    {
        long total = clauses.Count;
        foreach (List<Expression> clause in clauses)
        {
            total += clause.Count;
        }

        return total;
    }

    /// <summary>
    /// Turns the clause set back into a tree: each clause is a group of the inner connective (a lone literal stays as it
    /// is), and the groups are joined by the outer connective. DNF clauses are <c>AND</c> groups under an <c>OR</c>; CNF
    /// clauses are <c>OR</c> groups under an <c>AND</c>.
    /// </summary>
    private static Expression Assemble(List<List<Expression>> clauses, Form form)
    {
        bool disjunctive = form == Form.Dnf;
        Expression[] groups =
        [
            .. clauses.Select(clause => clause.Count == 1 ? clause[0] : ExpressionTools.Junction(clause, isAnd: disjunctive)),
        ];
        return groups.Length == 1 ? groups[0] : ExpressionTools.Junction(groups, isAnd: !disjunctive);
    }

    /// <summary>
    /// The stateless helpers of <see cref="NnfBuilder"/>: threshold cost estimates, constant-aware joining and negation.
    /// They hold no builder state, so they live apart from the memoized visitor.
    /// </summary>
    private static class NnfSupport
    {
        /// <summary>
        /// A threshold is cheap to expand when every <c>AtLeast</c> level it needs is a single group or a single subset:
        /// <c>AtLeast(1)</c> is an <c>OR</c> and <c>AtLeast(n)</c> an <c>AND</c>. Such a threshold expands without the option.
        /// </summary>
        internal static bool IsCheap(ThresholdExpression t, int n)
        {
            return t.Comparison switch
            {
                ThresholdComparison.AtLeast => CheapLevel(t.K, n),
                ThresholdComparison.AtMost => CheapLevel(t.K + 1, n),
                _ => CheapLevel(t.K, n) && CheapLevel(t.K + 1, n),
            };
        }

        /// <summary>Whether <c>AtLeast(k)</c> over <paramref name="n"/> operands is a single group (<c>k</c> of 1 or at least <c>n</c>).</summary>
        internal static bool CheapLevel(int k, int n)
        {
            return k <= 1 || k >= n;
        }

        /// <summary>The approximate node count of the expansion of a threshold.</summary>
        internal static double EstimateExpansion(ThresholdExpression t)
        {
            int n = t.Operands.Count;
            return t.Comparison switch
            {
                ThresholdComparison.AtLeast => LevelSize(t.K, n),
                ThresholdComparison.AtMost => LevelSize(t.K + 1, n),
                _ => LevelSize(t.K, n) + LevelSize(t.K + 1, n) + 1,
            };
        }

        /// <summary>The approximate node count of <c>AtLeast(k)</c> over <paramref name="n"/> operands: one group of <c>k</c> literals per subset, plus the join.</summary>
        internal static double LevelSize(int k, int n)
        {
            return k <= 0 || k > n ? 1 : (Binomial(n, k) * (k + 1)) + 1;
        }

        /// <summary>The binomial coefficient <c>C(n, k)</c> as a double, which loses precision instead of overflowing.</summary>
        internal static double Binomial(int n, int k)
        {
            k = Math.Min(k, n - k);
            double result = 1;
            for (int i = 1; i <= k; i++)
            {
                result = result * (n - k + i) / i;
            }

            return result;
        }

        /// <summary>
        /// Joins <paramref name="parts"/> with <c>AND</c> or <c>OR</c>, dropping the identity constant, collapsing on the
        /// annihilator and returning a lone part as itself. Only the threshold boundary cases produce constants here.
        /// </summary>
        internal static Expression SmartJoin(IReadOnlyList<Expression> parts, bool andJoin)
        {
            TruthValue identity = andJoin ? TruthValue.True : TruthValue.False;
            TruthValue annihilator = andJoin ? TruthValue.False : TruthValue.True;
            List<Expression> kept = [];
            foreach (Expression part in parts)
            {
                if (part is ConstantExpression c && c.Value == annihilator)
                {
                    return part;
                }

                if (part is not ConstantExpression k || k.Value != identity)
                {
                    kept.Add(part);
                }
            }

            return kept.Count switch
            {
                0 => new ConstantExpression(identity),
                1 => kept[0],
                _ => ExpressionTools.Junction(kept, andJoin),
            };
        }

        /// <summary>Strong Kleene negation of a value: it swaps <c>True</c> and <c>False</c> and leaves <c>Unknown</c> alone.</summary>
        internal static TruthValue Negate(TruthValue value)
        {
            return value switch
            {
                TruthValue.True => TruthValue.False,
                TruthValue.False => TruthValue.True,
                _ => TruthValue.Unknown,
            };
        }

        /// <summary>An atom as a literal: the atom itself, or its negation when the polarity is negative.</summary>
        internal static Expression Literal(Expression atom, bool negate)
        {
            return negate ? new NotExpression(atom) : atom;
        }
    }

    /// <summary>Builds negation normal form, memoizing each (node, polarity) so a shared sub-tree is visited once.</summary>
    private sealed class NnfBuilder(bool expandThresholds, long cap)
    {
        private readonly Dictionary<Expression, Expression> positive = [with(ReferenceEqualityComparer.Instance)];
        private readonly Dictionary<Expression, Expression> negative = [with(ReferenceEqualityComparer.Instance)];
        private readonly List<KeptThreshold> kept = [];

        public bool Exceeded { get; private set; }

        public IReadOnlyList<KeptThreshold> Kept => this.kept;

        public Expression Visit(Expression node, bool negate)
        {
            Dictionary<Expression, Expression> memo = negate ? this.negative : this.positive;
            if (memo.TryGetValue(node, out Expression? known))
            {
                return known;
            }

            Expression result = this.VisitCore(node, negate);
            memo[node] = result;
            return result;
        }

        private Expression VisitCore(Expression node, bool negate)
        {
            return node switch
            {
                ConstantExpression c => negate ? new ConstantExpression(NnfSupport.Negate(c.Value)) : c,
                TermExpression => negate ? new NotExpression(node) : node,
                NotExpression n => this.Visit(n.Operand, !negate),

                // De Morgan: a negated AND is an OR of negated operands, and the reverse.
                AndExpression a => this.JoinOperands(a.Operands, negate, andJoin: !negate),
                OrExpression o => this.JoinOperands(o.Operands, negate, andJoin: negate),

                // Not information-monotone: no NOT is pushed in, but the operands below are normalized.
                CoalesceExpression or InspectionExpression or IfExpression => NnfSupport.Literal(
                    ExpressionTools.MapChildren(node, child => this.Visit(child, false)),
                    negate
                ),

                ThresholdExpression t => this.VisitThreshold((ThresholdExpression)PrimitiveExpander.ExpandTop(t), negate),

                // Every other operator is derived: expand its own level, then normalize the definition.
                _ => this.Visit(PrimitiveExpander.ExpandTop(node), negate),
            };
        }

        /// <summary>Normalizes each operand at the given polarity and joins them; De Morgan picks the connective via <paramref name="andJoin"/>.</summary>
        private Expression JoinOperands(EquatableArray<Expression> operands, bool negate, bool andJoin)
        {
            return ExpressionTools.Junction([.. operands.Select(operand => this.Visit(operand, negate))], andJoin);
        }

        private Expression VisitThreshold(ThresholdExpression t, bool negate)
        {
            int n = t.Operands.Count;
            if (!expandThresholds && !NnfSupport.IsCheap(t, n))
            {
                this.Keep(t);
                return NnfSupport.Literal(ExpressionTools.MapChildren(t, child => this.Visit(child, false)), negate);
            }

            return t.Comparison switch
            {
                // AtMost(k) is NOT AtLeast(k + 1); Exactly(k) is AtLeast(k) AND NOT AtLeast(k + 1). Both are exact in K3.
                ThresholdComparison.AtLeast => this.AtLeast(t.K, t.Operands, negate),
                ThresholdComparison.AtMost => this.AtLeast(t.K + 1, t.Operands, !negate),
                _ => negate
                    ? NnfSupport.SmartJoin(
                        [this.AtLeast(t.K, t.Operands, true), this.AtLeast(t.K + 1, t.Operands, false)],
                        andJoin: false
                    )
                    : NnfSupport.SmartJoin(
                        [this.AtLeast(t.K, t.Operands, false), this.AtLeast(t.K + 1, t.Operands, true)],
                        andJoin: true
                    ),
            };
        }

        /// <summary>
        /// <c>AtLeast(k)</c> is the <c>OR</c> over every k-subset of the <c>AND</c> of that subset; its negation is the
        /// <c>AND</c> over the subsets of the <c>OR</c> of the negated operands. A count at or below zero is
        /// <c>True</c> and above the operand count is <c>False</c>.
        /// </summary>
        private Expression AtLeast(int k, EquatableArray<Expression> operands, bool negate)
        {
            int n = operands.Count;
            if (k <= 0 || k > n)
            {
                bool value = (k <= 0) != negate;
                return new ConstantExpression(value ? TruthValue.True : TruthValue.False);
            }

            if (NnfSupport.Binomial(n, k) * k > cap)
            {
                this.Exceeded = true;
                return new ConstantExpression(TruthValue.Unknown);
            }

            Expression[] literals = [.. operands.Select(operand => this.Visit(operand, negate))];
            List<Expression> groups = [];
            int[] pick = [.. Enumerable.Range(0, k)];
            while (true)
            {
                groups.Add(NnfSupport.SmartJoin([.. pick.Select(i => literals[i])], andJoin: !negate));
                int position = k - 1;
                while (position >= 0 && pick[position] == n - k + position)
                {
                    position--;
                }

                if (position < 0)
                {
                    break;
                }

                pick[position]++;
                for (int i = position + 1; i < k; i++)
                {
                    pick[i] = pick[i - 1] + 1;
                }
            }

            return NnfSupport.SmartJoin(groups, andJoin: negate);
        }

        private void Keep(ThresholdExpression t)
        {
            if (this.kept.Any(existing => existing.Threshold.Equals(t)))
            {
                return;
            }

            this.kept.Add(new KeptThreshold(t, NnfSupport.EstimateExpansion(t)));
        }
    }

    /// <summary>A threshold the rewrite left as an atom, and what expanding it would cost.</summary>
    /// <param name="Threshold">The threshold node, with its operands as written.</param>
    /// <param name="EstimatedNodes">The approximate node count of its expansion.</param>
    internal sealed record KeptThreshold(ThresholdExpression Threshold, double EstimatedNodes);

    /// <summary>The outcome of a normal-form rewrite.</summary>
    /// <param name="Tree">The rewritten tree, or <see langword="null"/> when the rewrite would exceed the node cap.</param>
    /// <param name="Kept">The thresholds that stayed atoms, each once.</param>
    internal sealed record Result(Expression? Tree, IReadOnlyList<KeptThreshold> Kept);
}
