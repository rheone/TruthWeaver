namespace TruthWeaver.Rewriting;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// Reorders the operands of every <c>AND</c> and <c>OR</c> node, cheapest subtree first, so the evaluator's
/// left-to-right short-circuit (<see cref="Evaluation.EvaluationMode.ShortCircuit"/>) reaches a deciding cheap
/// predicate before an expensive one it would otherwise skip. Every other operator keeps its operand order:
/// <c>COALESCE</c>'s priority order, <c>IMPLIES</c>'s antecedent-first evaluation and <c>If</c>'s
/// condition-first evaluation all carry meaning beyond cost, so none is touched.
/// </summary>
internal static class OrderOptimizer
{
    /// <summary>Reorders <paramref name="root"/>'s <c>AND</c>/<c>OR</c> operands by <paramref name="costOf"/>.</summary>
    /// <param name="root">The tree to reorder.</param>
    /// <param name="costOf">
    /// The relative cost of one term, by its identity. Deterministic for a fixed rule and cost source, so the result is
    /// the same on every run.
    /// </param>
    /// <returns>An equivalent, same-size tree whose <c>AND</c>/<c>OR</c> operands are stable-sorted cheapest first.</returns>
    public static Expression Optimize(Expression root, Func<TermIdentity, double> costOf)
    {
        return new Rewriter(costOf).Visit(root);
    }

    /// <summary>A subtree's relative cost and the distinct terms it was computed from.</summary>
    /// <param name="Total">The sum of <c>costOf</c> over <see cref="Terms"/>.</param>
    /// <param name="Terms">
    /// The distinct <see cref="TermIdentity"/> values reachable from this subtree. A term that this subtree reaches
    /// through two different paths (shared structurally) is counted once, matching the evaluator's per-evaluation term
    /// memoization (ADR-0002): the cost estimate reflects what actually runs, not how many places reference it.
    /// </param>
    private readonly record struct SubtreeCost(double Total, IReadOnlySet<TermIdentity> Terms);

    private sealed class Rewriter(Func<TermIdentity, double> costOf)
    {
        private readonly Dictionary<Expression, Expression> rewritten = [with(ReferenceEqualityComparer.Instance)];
        private readonly Dictionary<Expression, SubtreeCost> costs = [with(ReferenceEqualityComparer.Instance)];

        public Expression Visit(Expression node)
        {
            if (this.rewritten.TryGetValue(node, out Expression? known))
            {
                return known;
            }

            // Children first, so a nested AND/OR is already reordered before this node ranks its own operands - and so
            // this node's own cost (computed from the already-visited children below) is stable either way.
            Expression rebuilt = ExpressionTools.MapChildren(node, this.Visit);
            Expression result = rebuilt switch
            {
                AndExpression and => this.Reorder(and, and.Operands, operands => new AndExpression(operands)),
                OrExpression or => this.Reorder(or, or.Operands, operands => new OrExpression(operands)),
                _ => rebuilt,
            };

            this.rewritten[node] = result;
            return result;
        }

        /// <summary>
        /// Stable-sorts <paramref name="operands"/> cheapest first. LINQ's <c>OrderBy</c> is a stable sort, so operands of
        /// equal cost (including the all-neutral case, where every predicate has no <c>Cost</c> hint) keep their original
        /// relative order - an unhinted rule is returned unchanged.
        /// </summary>
        private Expression Reorder(
            Expression original,
            EquatableArray<Expression> operands,
            Func<EquatableArray<Expression>, Expression> build
        )
        {
            Expression[] sorted = [.. operands.OrderBy(operand => this.CostOf(operand).Total)];
            bool changed = false;
            for (int i = 0; i < sorted.Length; i++)
            {
                if (!ReferenceEquals(sorted[i], operands[i]))
                {
                    changed = true;
                    break;
                }
            }

            return changed ? build(new EquatableArray<Expression>(sorted)) : original;
        }

        private SubtreeCost CostOf(Expression node)
        {
            if (this.costs.TryGetValue(node, out SubtreeCost known))
            {
                return known;
            }

            SubtreeCost result;
            if (node is TermExpression term)
            {
                result = new SubtreeCost(costOf(term.Identity), new HashSet<TermIdentity> { term.Identity });
            }
            else if (node is ConstantExpression)
            {
                result = new SubtreeCost(0.0, new HashSet<TermIdentity>());
            }
            else
            {
                HashSet<TermIdentity> terms = [];
                foreach (Expression child in ExpressionShape.Of(node).Operands)
                {
                    terms.UnionWith(this.CostOf(child).Terms);
                }

                double total = 0.0;
                foreach (TermIdentity termIdentity in terms)
                {
                    total += costOf(termIdentity);
                }

                result = new SubtreeCost(total, terms);
            }

            this.costs[node] = result;
            return result;
        }
    }
}
